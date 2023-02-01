using Alpaca.Markets;

namespace CryptoTradingBot;

internal sealed class ScalpingBot {
    private const string SYMBOL = "ETH/USD";
    private const string SYMBOL_NS = "ETHUSD";

    private const int FREQUENCY_MINS = 1;
    private const int HISTORICAL_DATA_MINS = 10;
    private const int MINS_TO_MILLI = 60000;
    private const decimal NOTIONAL_PERCENT = 0.5m;
    private const decimal TRADING_FEE = 0.003m;

    private static bool buyOrder = false;
    private static bool sellOrder = false;
    private static decimal buyPrice = 0.0m;
    private static decimal sellPrice = 0.0m;
    private static decimal buyOrderPrice = 0.0m;
    private static decimal sellOrderPrice = 0.0m;

    private static decimal currentPrice = 0.0m;
    private static decimal currentPosition = 0.0m;
    private static decimal cutLossThreshold = 0.005m;
    private static decimal spread = 0.0m;
    private static decimal totalFees = 0.0m;

    private static List<Guid> executedOrders = new List<Guid>();

    public static async Task Main() {
        IAlpacaCryptoDataClient dClient = Environments.Paper.GetAlpacaCryptoDataClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));
        IAlpacaTradingClient tClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));
        IAccount account = await tClient.GetAccountAsync();

        while (true) {
            Console.WriteLine("-----------------------------------");
            string currentTime = DateTime.UtcNow.ToString();
            Console.WriteLine("Current Time: " + currentTime.Substring(currentTime.Length - 10));
            await getHistoricalData(dClient, tClient, true);
            bool checkedMarket = await checkMarket(tClient, account);
            Console.WriteLine($"Check Market Complete! Returned {checkedMarket}");
            await Task.Delay(FREQUENCY_MINS * MINS_TO_MILLI);
        }
    }

    public static async Task<IReadOnlyList<IBar>> getHistoricalData(IAlpacaCryptoDataClient dClient, IAlpacaTradingClient tClient, bool logInfo) {
        DateTime end = DateTime.UtcNow;
        DateTime start = end.AddMinutes(-HISTORICAL_DATA_MINS);
        IReadOnlyList<IBar> bars = (await dClient.ListHistoricalBarsAsync(new HistoricalCryptoBarsRequest(SYMBOL, start, end, BarTimeFrame.Minute))).Items;

        if (logInfo) {
            writeHistoryToFile(bars);

            decimal startPrice = bars.First().Open;
            decimal endPrice = bars.Last().Close;
            Console.WriteLine($"{SYMBOL} Current (Close) Price: ${endPrice}");
            decimal percentChange = (endPrice - startPrice) / startPrice;
            Console.WriteLine($"{SYMBOL} moved {percentChange:P} over the last {HISTORICAL_DATA_MINS} mins.");
        }

        decimal currentPrice = bars.Last().Close;
        decimal minLowPrice = bars.First().Low;
        decimal maxHighPrice = bars.First().High;
        foreach (IBar bar in bars) {
            if (minLowPrice > bar.Low) {
                minLowPrice = bar.Low;
            }

            if (maxHighPrice < bar.High) {
                maxHighPrice = bar.High;
            }
        }

        buyPrice = Math.Round(minLowPrice * 1.002m, 1);
        sellPrice = Math.Round(maxHighPrice * 0.998m, 1);
        spread = Math.Round(sellPrice - buyPrice, 1);
        totalFees = Math.Round((buyPrice * TRADING_FEE) + (sellPrice * TRADING_FEE), 1);

        try {
            IPosition symbolPos = await tClient.GetPositionAsync(SYMBOL_NS);
            decimal currentPosition = symbolPos.Quantity;
            buyOrder = false;
        }
        catch (RestClientErrorException) {
            sellOrder = false;
        }

        return bars;
    }

    private static void writeHistoryToFile(IReadOnlyList<IBar> bars) {
        string timeOutput = $"Last {HISTORICAL_DATA_MINS} Minutes Report:\n";
        string priceOutput = "\n";
        string currentTime = "";
        foreach (IBar bar in bars) {
            currentTime = bar.TimeUtc.ToString();
            timeOutput += currentTime.Substring(currentTime.Length - 10) + "\n";
            priceOutput += bar.Vwap + "\n";
        }

        File.WriteAllText("./HistOutput.txt", timeOutput + priceOutput);
    }

    /// <summary>
    /// Returns true if it is a profitable time to trade
    /// </summary>
    public static async Task<bool> checkMarket(IAlpacaTradingClient tClient, IAccount account) {
        if (spread < totalFees) {
            Console.WriteLine("Spread is Less than Total Fees, Not a Profitable Opportunity to Trade");
            Console.WriteLine($"SPREAD: {spread}");
            Console.WriteLine($"TOTAL FEES: {totalFees}");
            Console.WriteLine($"BUY PRICE: {buyPrice}");
            Console.WriteLine($"SELL PRICE: {sellPrice}");

            return false;
        } else {
            if (currentPosition <= 0.01m && (!buyOrder) && currentPrice > buyPrice) {
                Console.WriteLine("No Position, No Open Orders, Spread is Greater than Total Fees, Placing Limit Buy Order at Buy Price");
                decimal availCash = account.TradableCash;
                IOrder buyLimitOrder = await tClient.PostOrderAsync(OrderSide.Buy.Limit(SYMBOL, OrderQuantity.Notional(NOTIONAL_PERCENT * availCash), buyPrice).WithDuration(TimeInForce.Gtc));
                buyOrderPrice = buyPrice;
                sellOrderPrice = sellPrice;
                executedOrders.Add(buyLimitOrder.OrderId);
                buyOrder = true;
                sellOrder = false;
            }

            if (currentPosition >= 0.01m && (!sellOrder) && currentPrice < sellOrderPrice) {
                Console.WriteLine("Have Position, No Open Orders, Spread is Greater than Total Fees, Place Limit Sell Order at Sell Price");
                IPosition symbolPos = await tClient.GetPositionAsync(SYMBOL_NS);
                IOrder sellLimitOrder = await tClient.PostOrderAsync(OrderSide.Sell.Limit(SYMBOL, OrderQuantity.Fractional(symbolPos.AvailableQuantity), sellPrice).WithDuration(TimeInForce.Gtc));
                buyOrderPrice = buyPrice;
                sellOrderPrice = sellPrice;
                executedOrders.Add(sellLimitOrder.OrderId);
                sellOrder = true;
                buyOrder = false;
            }

            if (currentPosition <= 0.01m && buyOrder && currentPrice > (sellOrderPrice * (1 + cutLossThreshold))) {
                Console.WriteLine("No Position, Open Buy Order and Current Price above Selling Price, Canceling Buy Limit Order");
                foreach (Guid orderID in executedOrders) {
                    try {
                        await tClient.CancelOrderAsync(orderID);
                    }
                    catch (RestClientErrorException) {
                    }
                }

                executedOrders.Clear();
                buyOrder = false;
            }

            if (currentPosition >= 0.01m && sellOrder && currentPrice < (buyOrderPrice * (1 - cutLossThreshold))) {
                Console.WriteLine("Have Position, Open Sell, Current Price Below Buy Price, Canceling Sell Limit Order");
                foreach (Guid orderID in executedOrders) {
                    try {
                        await tClient.CancelOrderAsync(orderID);
                    }
                    catch (RestClientErrorException) {
                    }
                }

                executedOrders.Clear();
                sellOrder = false;
            }

            return true;
        }
    }
}

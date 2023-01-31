using Alpaca.Markets;

namespace CryptoTradingBot;

internal sealed class Bot {
    private const decimal NOTIONAL_OFFSET = 0.05m;
    private const string SYMBOL = "ETH/USD";
    private const string SYMBOL_NS = "ETHUSD";

    public static async Task<IReadOnlyList<IBar>> getHistoricalData(IAlpacaCryptoDataClient dClient, int minsBack, bool writeToFile) {
        DateTime end = DateTime.UtcNow;
        DateTime start = end.AddMinutes(-minsBack);

        IReadOnlyList<IBar> bars = (await dClient.ListHistoricalBarsAsync(new HistoricalCryptoBarsRequest(SYMBOL, start, end, BarTimeFrame.Minute))).Items;
        decimal startPrice = bars.First().Open;
        decimal endPrice = bars.Last().Close;
        decimal lowPrice = bars.First().Low;
        
        if (writeToFile) {
            string fileOutput = $"Last {minsBack} Minutes Report:\n";
            foreach (var item in bars) {
                fileOutput += item.TimeUtc.ToString().Substring(10) + "\n";

                if (lowPrice > item.Low) {
                    lowPrice = item.Low;
                }
            }
            fileOutput += "\n";

            foreach (var item in bars) {
                fileOutput += item.Vwap + "\n";
            }
            File.WriteAllText("./histOutput.txt", fileOutput);
        } else {
            foreach (var item in bars) {
                if (lowPrice > item.Low) {
                    lowPrice = item.Low;
                }
            }
        }

        Console.WriteLine($"Low Price (Last {minsBack} mins.): $" + lowPrice);
        Console.WriteLine("Current (Close) Price: $" + endPrice);

        decimal percentChange = (endPrice - startPrice) / startPrice;
        Console.WriteLine($"{SYMBOL} moved {percentChange:P} over the last {minsBack} mins.");

        return bars;
    }

    public static async Task Main() {
        Console.WriteLine("Current Time: " + DateTime.UtcNow.ToString().Substring(10));
        
        IAlpacaCryptoDataClient dClient = Environments.Paper.GetAlpacaCryptoDataClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));
        IAlpacaTradingClient tClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));
        IAccount account = await tClient.GetAccountAsync();

        decimal cash = account.TradableCash;
        decimal? assetsValue = account.LongMarketValue;

        IReadOnlyList<IBar> bars = await getHistoricalData(dClient, 10, true);

        Console.WriteLine();
        IOrder buyOrder = await tClient.PostOrderAsync(OrderSide.Buy.Market(SYMBOL, OrderQuantity.Notional(1.0m + NOTIONAL_OFFSET)).WithDuration(TimeInForce.Gtc));
        // Console.WriteLine("Buy Order: " + buyOrder + "\n");

        IPosition ethPos = await tClient.GetPositionAsync(SYMBOL_NS);
        Console.WriteLine("Position: " + ethPos + "\n");
        decimal? ethMarketVal = ethPos.MarketValue; 
        
        IOrder sellOrder = await tClient.PostOrderAsync(OrderSide.Sell.Market(SYMBOL, OrderQuantity.Notional(1.0m)).WithDuration(TimeInForce.Gtc));
        // Console.WriteLine("Sell Order: " + sellOrder);



        //IOrder buyLimitOrder = await tClient.PostOrderAsync(OrderSide.Buy.Limit())
    }
}

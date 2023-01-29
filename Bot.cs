using Alpaca.Markets;

namespace CryptoTradingBot;

internal sealed class Bot {
    private const double NOTIONAL_OFFSET = 0.05;
    private const string SYMBOL = "ETH/USD";

    public static async Task Main() {
        var dClient = Environments.Paper.GetAlpacaCryptoDataClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));
        var tClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));
        var sClient = Environments.Paper.GetAlpacaCryptoStreamingClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));

        await sClient.ConnectAndAuthenticateAsync();
        var account = await tClient.GetAccountAsync();

        DateTime start = DateTime.Today.AddMinutes(-10);
        DateTime end = DateTime.Today;

        var bars = await dClient.ListHistoricalBarsAsync(new HistoricalCryptoBarsRequest(SYMBOL, start, end, BarTimeFrame.Minute));
        var startPrice = bars.Items.First().Open;
        var endPrice = bars.Items.Last().Close;

        decimal lowPrice = bars.Items.First().Low;

        foreach (var item in bars.Items) {
            if (lowPrice > item.Low) {
                lowPrice = item.Low;
            }
        }

        Console.WriteLine("Low Price (Last 10 min.): " + lowPrice);
        Console.WriteLine("Current Price $" + endPrice);

        var percentChange = (endPrice - startPrice) / startPrice;
        Console.WriteLine($"{SYMBOL} moved {percentChange:P} over the last 10 mins.");

        var barSubscription = sClient.GetMinuteBarSubscription(SYMBOL);
        barSubscription.Received += (bar) => {
            Console.WriteLine(bar);
        };

        await sClient.SubscribeAsync(barSubscription);
        //while(true);

        











        // Console.WriteLine(account.ToString());
        // var buyOrder = await client.PostOrderAsync(OrderSide.Buy.Market("ETHUSD", 20).WithDuration(TimeInForce.Ioc));
        //Console.WriteLine(buyOrder);
        // var buyOrder = await tClient.PostOrderAsync(OrderSide.Buy.Market("BTCUSD", OrderQuantity.Fractional((decimal) 0.1)).WithDuration(TimeInForce.Ioc));

        // var ETH = await tClient.GetAssetAsync("ETHUSD"); 
        // Console.WriteLine(ETH + "\n");

        // var ethPos = await tClient.GetPositionAsync("ETHUSD");
        // Console.WriteLine(ethPos);
        // 
        // var sellOrder = await tClient.PostOrderAsync(OrderSide.Sell.Market("BTCUSD", OrderQuantity.Notional((decimal) 1)).WithDuration(TimeInForce.Ioc));
        // Console.WriteLine();
        // Console.WriteLine(sellOrder);

    }
}

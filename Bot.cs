using Alpaca.Markets;

namespace CryptoTradingBot;

internal sealed class Bot {
    private const double NOTIONAL_OFFSET = 0.05;
    
    public static async Task Main() {
        var dClient = Environments.Paper.GetAlpacaCryptoDataClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));
        // var tClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(Constants.KEY_ID, Constants.SECRET_KEY));
        // var account = await tClient.GetAccountAsync();

        DateTime start = DateTime.Today.AddDays(-1);
        DateTime end = DateTime.Today;

        var bars = await dClient.ListHistoricalBarsAsync(new HistoricalCryptoBarsRequest("ETHUSD", start, end, BarTimeFrame.Day));
        Console.WriteLine(bars);













        // var endDate = DateTime.Today;
        // var startDate = endDate.AddMinutes(-10000);

        // var page = await dClient.ListHistoricalBarsAsync(new HistoricalCryptoBarsRequest("ETHUSD", startDate, endDate, BarTimeFrame.Minute));
        // var bars = page.Items;

        // See how much ETH moved in that timeframe.
        // var startPrice = bars.First().Open;
        // var endPrice = bars.Last().Close;

        // var percentChange = (endPrice - startPrice) / startPrice;
        // Console.WriteLine($"AAPL moved {percentChange:P} over the last 5 days.");



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

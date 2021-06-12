using Bittrex.Net;
using Bittrex.Net.Objects;
using CryptoExchange.Net.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StockAnalysis
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		// tbStockPrice
		const int INT_DotDiameter = 10;
		BittrexClient bittrexClient = new BittrexClient();
		BittrexSocketClient bittrexSocketClient = new BittrexSocketClient();
		WebCallResult<BittrexTick> bitcoinTicker;
		List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();
		double leftPos = 0;

		public MainWindow()
		{
			InitializeComponent();
			bitcoinTicker = bittrexClient.GetTicker("BTC-USDT");
			tbStockPrice.Text = $"BTC: ${bitcoinTicker.Data.LastTradeRate}";

			bittrexSocketClient.SubscribeToSymbolTickerUpdatesAsync("BTC-USDT", data =>
			{
				UpdateLastPrice(data);
			});
		}

		decimal high = 37000;
		decimal low = 35000;
		double chartHeightPixels = 500;

		void CalculateHighAndLow()
		{
			high = 0;
			low = decimal.MaxValue;
			foreach (StockDataPoint stockDataPoint in stockDataPoints)
			{
				if (stockDataPoint.Tick.LastTradeRate < low)
					low = stockDataPoint.Tick.LastTradeRate;
				if (stockDataPoint.Tick.LastTradeRate > high)
					high = stockDataPoint.Tick.LastTradeRate;
			}
		}
		void UpdateLastPrice(BittrexTick data)
		{
			// TODO: Connect the Ellipse with the StockDataPoint using a Dictionary, and update its position each time we get new data.
			StockDataPoint stockDataPoint = new StockDataPoint(data);
			stockDataPoints.Add(stockDataPoint);
			CalculateHighAndLow();
			if (high == low)
			{
				high += 1;
				low -= 1;
				if (low < 0)
					low = 0;
			}

			// Make sure the text is changed on the UI thread!
			Dispatcher.Invoke(() =>
			{
				leftPos += INT_DotDiameter;
				Ellipse dot = new Ellipse() { Fill = new SolidColorBrush(Colors.Red), Width = INT_DotDiameter, Height = INT_DotDiameter };
				Canvas.SetLeft(dot, leftPos);
				decimal amountAboveBottom = data.LastTradeRate - low;
				decimal chartHeightDollars = high - low;

				/* amountAboveTheLow = $1 
					 chartHeightDollars = $2
					 percentOfChartHeight = 0.5 == 50%
					 chartHeightPixels = 500px
				 */
				decimal percentOfChartHeightFromBottom = amountAboveBottom / chartHeightDollars;
				double distanceFromBottomPixels = (double)percentOfChartHeightFromBottom * chartHeightPixels;
				double distanceFromTopPixels = chartHeightPixels - distanceFromBottomPixels;
				Canvas.SetTop(dot, distanceFromTopPixels);
				cvsMain.Children.Add(dot);

				tbStockPrice.Text = $"{data.Symbol}: ${data.LastTradeRate}";
			});
		}
	}
}

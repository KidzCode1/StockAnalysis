using Bittrex.Net;
using Bittrex.Net.Objects;
using BotTraderCore;
using CryptoExchange.Net.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace StockAnalysis
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		static double chartHeightPixels = 500; // TODO: Let's not hard-code this.
		static double chartWidthPixels = 1900; // TODO: Let's not hard-code this.

		ChartTranslator chartTranslator = new ChartTranslator(chartWidthPixels, chartHeightPixels);


		BittrexClient bittrexClient = new BittrexClient();
		BittrexSocketClient bittrexSocketClient = new BittrexSocketClient();
		WebCallResult<BittrexTick> bitcoinTicker;

		// This is called a constructor. It initializes whatever class you are in.
		// No return type. 
		// Same name as the class.
		public MainWindow()
		{
			InitializeComponent();
			string symbol = "ETH";
			bitcoinTicker = bittrexClient.GetTicker($"{symbol}-USDT");
			tbStockPrice.Text = $"{symbol}: ${bitcoinTicker.Data.LastTradeRate}";
			
			bittrexSocketClient.SubscribeToSymbolTickerUpdatesAsync($"{symbol}-USDT", data =>
			{
				UpdateLastPrice(data);
			});

			tickGraph.SetChartTranslator(chartTranslator);
		}

		void UpdateLastPrice(BittrexTick data)
		{
			CustomTick ourData = new CustomTick(data);
			chartTranslator.AddStockPosition(ourData);

			Dispatcher.Invoke(() =>
			{
				tickGraph.DrawGraph();
				tbStockPrice.Text = $"{data.Symbol}: ${data.LastTradeRate}";
			});
		}
		
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			bittrexSocketClient.UnsubscribeAll();
		}

		private void btnTestSelection_Click(object sender, RoutedEventArgs e)
		{
			if (Selection.Exists)
			{
				List<StockDataPoint> selectedPoints = chartTranslator.GetPointsInRange(Selection.Start, Selection.End);

				string fullPathToFile = Folders.GetTestFilePath("Test3.json");

				selectedPoints.Save(fullPathToFile);

				List<StockDataPoint> loadedPoints = StockDataPoint.Load(fullPathToFile);

				if (selectedPoints.Matches(loadedPoints))
				{
					Title = "It worked!";
				}
				else
					Title = "Failure!";
			}
		}
	}
}

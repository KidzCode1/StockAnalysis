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
		ChartTranslator chartTranslator = new ChartTranslator();

		BittrexClient bittrexClient = new BittrexClient();
		BittrexSocketClient bittrexSocketClient = new BittrexSocketClient();
		WebCallResult<BittrexTick> bitcoinTicker;

		// This is called a constructor. It initializes whatever class you are in.
		// No return type. 
		// Same name as the class.
		public MainWindow()
		{
			InitializeComponent();
			SubscribeToTickerUpdates("BTC", "USDT");

			tickGraph.SetChartTranslator(chartTranslator);
		}

		private void SubscribeToTickerUpdates(string symbol, string quoteCurrency)
		{
			bitcoinTicker = bittrexClient.GetTicker($"{symbol}-{quoteCurrency}");
			if (bitcoinTicker.Data == null)
			{
				tbStockPrice.Text = $"{symbol}: Data is null!!!";
				tbStockPrice.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 194, 194));
			}
			else
			{
				tbStockPrice.Text = $"{symbol}: ${bitcoinTicker.Data.LastTradeRate}";
				tbStockPrice.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
			}

			bittrexSocketClient.SubscribeToSymbolTickerUpdatesAsync($"{symbol}-{quoteCurrency}", data =>
			{
				UpdateLastPrice(data);
			});
		}

		void UpdateLastPrice(BittrexTick data)
		{
			CustomTick ourData = new CustomTick(data);
			chartTranslator.AddStockPosition(ourData);

			// Dispatcher.Invoke ??? Threading ??? Forces the nested code to execute on the main UI thread
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
			if (tickGraph.Selection.Exists)
			{
				FrmTestGenerator frmTestGenerator = new FrmTestGenerator();
				frmTestGenerator.Show();
				ChartTranslator selectionChartTranslator = new ChartTranslator();
				List<StockDataPoint> selectedPoints = chartTranslator.GetPointsInRange(tickGraph.Selection.Start, tickGraph.Selection.End);

				selectionChartTranslator.SetStockDataPoints(selectedPoints);
				frmTestGenerator.tickGraph.SetChartTranslator(selectionChartTranslator);
				frmTestGenerator.tickGraph.DrawGraph();

				//string fullPathToFile = Folders.GetTestFilePath("Test3.json");

				//selectedPoints.Save(fullPathToFile);

				//List<StockDataPoint> loadedPoints = StockDataPoint.Load(fullPathToFile);

				//if (selectedPoints.Matches(loadedPoints))
				//{
				//	Title = "It worked!";
				//}
				//else
				//	Title = "Failure!";
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			double newWidth = e.NewSize.Width - 20;
			if (newWidth < 100)
				newWidth = 100;

			tickGraph.Width = newWidth;
			double newHeight = e.NewSize.Height - spContainer.ActualHeight - 50;
			if (newHeight < 100)
				newHeight = 100;

			tickGraph.Height = newHeight;
		}

		private void btnGo_Click(object sender, RoutedEventArgs e)
		{
			bittrexSocketClient.UnsubscribeAll();
			SubscribeToTickerUpdates($"{tbxCurrency.Text}", $"{tbxQuoteCurrency.Text}");
			chartTranslator.Clear();
		}
	}
}

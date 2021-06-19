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
		static double chartHeightPixels = 500; // TODO: Let's not hard-code this.
		static double chartWidthPixels = 1900; // TODO: Let's not hard-code this.

		ChartTranslator chartTranslator = new ChartTranslator(chartWidthPixels, chartHeightPixels);
		// tbStockPrice


		const double INT_DotDiameter = 10;
		const double INT_DotRadius = INT_DotDiameter / 2;
		BittrexClient bittrexClient = new BittrexClient();
		BittrexSocketClient bittrexSocketClient = new BittrexSocketClient();
		WebCallResult<BittrexTick> bitcoinTicker;
		//double leftPos = 0;

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

		void UpdateLastPrice(BittrexTick data)
		{
			chartTranslator.AddStockPosition(data);

			Dispatcher.Invoke(() =>
			{
				DrawGraph();
				tbStockPrice.Text = $"{data.Symbol}: ${data.LastTradeRate}";
			});
		}

		private void DrawGraph()
		{
			double lastX = double.MinValue;
			double lastY = double.MinValue;

			bool alreadyDrawnAtLeastOnePoint = false;

			cvsMain.Children.Clear();

			foreach (StockDataPoint stockDataPoint in chartTranslator.StockDataPoints)
			{
				double x = chartTranslator.GetStockPositionX(stockDataPoint.Time);
				double y = chartTranslator.GetStockPositionY(stockDataPoint.Tick.LastTradeRate);
				AddDot(x, y);

				if (alreadyDrawnAtLeastOnePoint)
				{
					AddLine(lastX, lastY, y, x);
				}

				alreadyDrawnAtLeastOnePoint = true;

				lastX = x;
				lastY = y;
			}
		}

		private void AddDot(double x, double y)
		{
			Ellipse dot = new Ellipse() { Fill = new SolidColorBrush(Colors.Red), Width = INT_DotDiameter, Height = INT_DotDiameter };
			Canvas.SetLeft(dot, x - INT_DotRadius);
			Canvas.SetTop(dot, y - INT_DotRadius);
			cvsMain.Children.Add(dot);
		}

		private void AddLine(double lastX, double lastY, double y, double x)
		{
			Line line = new Line();
			line.X1 = lastX;
			line.Y1 = lastY;
			line.X2 = x;
			line.Y2 = y;
			line.Stroke = new SolidColorBrush(Colors.Blue);
			line.StrokeThickness = 3;
			cvsMain.Children.Insert(0, line);  // All lines go to the back.
		}

		private void cvsMain_MouseMove(object sender, MouseEventArgs e)
		{
			// TODO: Make sure this works. Draw a line behind mouse.
			Point position = e.GetPosition(cvsMain);
			DateTime mouseTime = chartTranslator.GetTimeFromX(position.X);
			StockDataPoint nearestPoint = chartTranslator.GetNearestPoint(mouseTime);
			if (nearestPoint != null)
				Title = $"{nearestPoint.Tick.LastTradeRate}";
			else
				Title = "Move mouse near point to see value!";
		}
	}
}

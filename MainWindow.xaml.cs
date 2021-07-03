using Bittrex.Net;
using Bittrex.Net.Objects;
using CryptoExchange.Net.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
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


		const double INT_DotDiameter = 6;
		const double INT_DotRadius = INT_DotDiameter / 2;
		BittrexClient bittrexClient = new BittrexClient();
		BittrexSocketClient bittrexSocketClient = new BittrexSocketClient();
		WebCallResult<BittrexTick> bitcoinTicker;
		Point lastMousePosition;
		//
		//double leftPos = 0;

		public MainWindow()
		{
			Selection.OnChange += Selection_OnChange;
			Selection.OnChanging += Selection_OnChanging;

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

		void ChartPoints(Canvas canvas, List<Point> smallMovingAverage, SolidColorBrush brush, int lineThickness)
		{
			double lastX = double.MinValue;
			double lastY = double.MinValue;

			foreach (Point point in smallMovingAverage)
			{
				if (lastX != double.MinValue)
				{
					Line line = CreateLine(lastX, lastY, point.X, point.Y, lineThickness);
					line.Stroke = brush;
					line.StrokeEndLineCap = PenLineCap.Round;
					canvas.Children.Add(line);
				}

				lastX = point.X;
				lastY = point.Y;
			}
		}

		private void DrawGraph()
		{
			double lastX = double.MinValue;
			double lastY = double.MinValue;

			bool alreadyDrawnAtLeastOnePoint = false;

			cvsMain.Children.Clear();

			lock (chartTranslator.stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in chartTranslator.StockDataPoints)
				{
					double x = chartTranslator.GetStockPositionX(stockDataPoint.Time);
					double y = chartTranslator.GetStockPositionY(stockDataPoint.Tick.LastTradeRate);
					AddDot(lastY, x, y, stockDataPoint);

					if (alreadyDrawnAtLeastOnePoint)
					{
						AddLine(lastX, lastY, x, y);
					}

					alreadyDrawnAtLeastOnePoint = true;

					lastX = x;
					lastY = y;
				}

			AddAdornments(lastMousePosition);
			UpdateSelection();
			DrawAnalysisCharts();
		}

		private void DrawAnalysisCharts()
		{
			cvsAnalysis.Children.Clear();

			//chartTranslator.AddMovingAverage(20, cvsAnalysis, new SolidColorBrush(Color.FromArgb(127, 27, 0, 163)));
			//chartTranslator.AddMovingAverage(200, cvsAnalysis, new SolidColorBrush(Color.FromArgb(127, 0, 178, 33)));

			List<Point> smallMovingAverage = chartTranslator.GetMovingAverages(20);
			ChartPoints(cvsAnalysis, smallMovingAverage, new SolidColorBrush(Color.FromArgb(127, 27, 0, 163)), 4);

			List<Point> largerMovingAverage = chartTranslator.GetMovingAverages(100);
			ChartPoints(cvsAnalysis, largerMovingAverage, new SolidColorBrush(Color.FromArgb(127, 34, 171, 0)), 8);

			//chartTranslator.AddMovingAverage(5, cvsMain, new SolidColorBrush(Color.FromArgb(127, 0, 255, 47)));
		}

		private void AddDot(double lastY, double x, double y, StockDataPoint stockDataPoint)
		{
			Ellipse dot = new Ellipse() { Fill = new SolidColorBrush(GetFillColor(lastY, y, 128)), Width = INT_DotDiameter, Height = INT_DotDiameter };
			Canvas.SetLeft(dot, x - INT_DotRadius);
			Canvas.SetTop(dot, y - INT_DotRadius);
			cvsMain.Children.Add(dot);
			dot.Tag = stockDataPoint;
		}

		private static Color GetFillColor(double lastY, double y, byte opacity)
		{
			if (lastY == double.MinValue || lastY == y /* IsClose(lastY, y) */)
				return Color.FromArgb(opacity, 164, 74, 255);
			if (lastY > y)
				return Color.FromArgb(opacity, 0, 75, 125);
			return Color.FromArgb(opacity, 255, 46, 46);
		}

		private static bool IsClose(double lastY, double y)
		{
			double difference = Math.Abs(lastY - y);
			if (difference == 0)
				return true;

			double largestValue = Math.Max(lastY, y);
			if (largestValue == 0)
				return true;

			double percentClose = difference / largestValue;  // should be between 0-1
			if (percentClose < 0.03)
				return true;
			return false;
		}

		private void AddLine(double lastX, double lastY, double x, double y)
		{
			Line line = CreateLine(lastX, lastY, x, y);
			line.Stroke = new SolidColorBrush(GetFillColor(lastY, y, 255));
			cvsMain.Children.Insert(0, line);  // All lines go to the back.
		}

		private static Line CreateLine(double lastX, double lastY, double x, double y, double lineThickness = 1)
		{
			Line line = new Line();
			line.X1 = lastX;
			line.Y1 = lastY;
			line.X2 = x;
			line.Y2 = y;
			line.StrokeThickness = lineThickness;
			return line;
		}

		double GetX(UIElement uIElement)
		{
			return Canvas.GetLeft(uIElement) + INT_DotRadius;
		}

		double GetY(UIElement uIElement)
		{
			return Canvas.GetTop(uIElement) + INT_DotRadius;
		}

		StockDataPoint GetNearestPoint(double mouseX, double mouseY)
		{
			Ellipse closestEllipse = GetClosestEllipse(mouseX, mouseY);

			// We're finally done checking all the points!!.
			if (closestEllipse == null)
				return null;

			if (closestEllipse.Tag is StockDataPoint stockDataPoint)
				return stockDataPoint;
			return null;
		}

		private Ellipse GetClosestEllipse(double mouseX, double mouseY)
		{
			double closestDistanceSoFar = double.MaxValue;
			Ellipse closestEllipse = null;

			foreach (UIElement uIElement in cvsMain.Children)
			{
				if (uIElement is Ellipse ellipse)
				{
					double x = GetX(ellipse);
					double y = GetY(ellipse);

					double deltaX = mouseX - x;
					double deltaY = mouseY - y;

					double distance = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));   // Pythagorean's theorem.

					if (closestDistanceSoFar > distance)
					{
						closestDistanceSoFar = distance;
						closestEllipse = ellipse;
					}
				}
			}

			return closestEllipse;
		}

		void AddHighlightCircle(double x, double y)
		{
			Ellipse ellipse = new Ellipse();
			const double diameter = 20;
			const double radius = diameter / 2.0;
			ellipse.Height = diameter;
			ellipse.Width = diameter;
			ellipse.Stroke = new SolidColorBrush(Colors.Black);
			ellipse.StrokeThickness = 2;
			Canvas.SetLeft(ellipse, x - radius);
			Canvas.SetTop(ellipse, y - radius);
			cvsAdornments.Children.Add(ellipse);
		}

		private void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			AddAdorments(e);
			UpdateSelectionIfNeeded(e);
		}

		private void AddAdorments(MouseEventArgs e)
		{
			lastMousePosition = e.GetPosition(cvsAdornments);
			AddAdornments(lastMousePosition);
		}

		private static string GetNum(decimal value, int numDigits = 29)
		{
			string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			string strDec = value.ToString("#,0." + new string('#', numDigits), CultureInfo.CurrentCulture);
			return strDec.Contains(sep) ? strDec.TrimEnd('0').TrimEnd(sep.ToCharArray()) : strDec;
		}

		void ShowHintData(double x, double y, StockDataPoint nearestPoint)
		{
			string symbol = nearestPoint.Tick.Symbol;
			string currency = string.Empty;
			int dashIndex = symbol.IndexOf("-");
			if (dashIndex >= 0)
				currency = symbol.Substring(dashIndex + 1);
			tbTradePrice.Text = $"{GetNum(nearestPoint.Tick.LastTradeRate)} {currency}";
			tbHighestBid.Text = $"{GetNum(nearestPoint.Tick.BidRate)} {currency}";
			tbLowestAsk.Text = $"{GetNum(nearestPoint.Tick.AskRate)} {currency}";
			tbTime.Text = $"{nearestPoint.Time:yyy MMM dd hh:mm:ss.fff}";
			grdStockTickDetails.Visibility = Visibility.Visible;
			double yPos = y - 34;
			
			if (OnLeftSide(x))
			{
				stockHintPointingRight.Visibility = Visibility.Hidden;
				stockHintPointingLeft.Visibility = Visibility.Visible;
				Canvas.SetLeft(stockHintPointingLeft, x);
				Canvas.SetTop(stockHintPointingLeft, yPos);
				Canvas.SetLeft(grdStockTickDetails, x + 50);
				Canvas.SetTop(grdStockTickDetails, yPos + 8);
			}
			else
			{
				stockHintPointingRight.Visibility = Visibility.Visible;
				stockHintPointingLeft.Visibility = Visibility.Hidden;
				double xPos = x - stockHintPointingRight.ActualWidth;
				Canvas.SetLeft(stockHintPointingRight, xPos);
				Canvas.SetTop(stockHintPointingRight, yPos);
				Canvas.SetLeft(grdStockTickDetails, xPos + 13);
				Canvas.SetTop(grdStockTickDetails, yPos + 8);
			}
		}

		private bool OnLeftSide(double x)
		{
			return x < chartTranslator.chartWidthPixels / 2;
		}

		private void AddAdornments(Point position)
		{
			cvsAdornments.Children.Clear();

			AddDashedLine(position);

			DateTime mouseTime = chartTranslator.GetTimeFromX(position.X);
			Ellipse closestEllipse = GetClosestEllipse(position.X, position.Y);
			if (closestEllipse != null)
			{
				double x = GetX(closestEllipse);
				double y = GetY(closestEllipse);
				AddHighlightCircle(x, y);

				StockDataPoint nearestPoint = closestEllipse.Tag as StockDataPoint;
				if (nearestPoint != null)
				{
					Title = $"{nearestPoint.Tick.LastTradeRate}";
					ShowHintData(x, y, nearestPoint);
				}
				else
					Title = "Move mouse near point to see value!";
			}
		}

		private Size MeasureString(TextBlock textBlock)
		{
			var formattedText = new FormattedText(
					textBlock.Text,
					CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight,
					new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
					textBlock.FontSize,
					Brushes.Black,
					new NumberSubstitution(),
					1);

			return new Size(formattedText.Width, formattedText.Height);
		}

		private void AddDashedLine(Point position)
		{
			DateTime time = chartTranslator.GetTime(position.X);
			TextBlock timeTextBlock = new TextBlock();
			timeTextBlock.Text = time.ToString("dd MMM yyyy - hh:mm:ss.ff");
			
			cvsAdornments.Children.Add(timeTextBlock);
			if (position.X > chartTranslator.chartWidthPixels / 2)
			{
				Size size = MeasureString(timeTextBlock);
				// Right-align this.
				Canvas.SetLeft(timeTextBlock, position.X - size.Width - 5);
			}
			else  // Left-align:
				Canvas.SetLeft(timeTextBlock, position.X + 5);
			

			Line line = CreateLine(position.X, 0, position.X, cvsAdornments.Height);
			line.IsHitTestVisible = false;
			line.Stroke = new SolidColorBrush(Color.FromArgb(200, 115, 115, 115));
			line.StrokeDashArray.Add(5);
			line.StrokeDashArray.Add(3);
			cvsAdornments.Children.Add(line);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			bittrexSocketClient.UnsubscribeAll();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Point position = e.GetPosition(cvsSelection);
			if (Selection.IsInBounds(position.X, position.Y))
			{
				Selection.Mode = SelectionModes.DraggingToSelect;
				Title = "Mouse down detected! DraggingToSelect...";
				Selection.Anchor = chartTranslator.GetTimeFromX(position.X);
				cvsSelection.CaptureMouse();
			}
		}

		void UpdateSelection()
		{
			cvsSelection.Children.Clear();
			if (!Selection.Exists)
				return;
			Rectangle selectionRect = new Rectangle();
			selectionRect.Fill = new SolidColorBrush(Color.FromArgb(73, 54, 127, 255));
			Canvas.SetTop(selectionRect, 0);
			selectionRect.Height = cvsSelection.ActualHeight;

			double leftSide = chartTranslator.GetStockPositionX(Selection.Start);  // 750
			double rightSide = chartTranslator.GetStockPositionX(Selection.End);   // 1100

			Canvas.SetLeft(selectionRect, leftSide);

			selectionRect.Width = rightSide - leftSide;

			cvsSelection.Children.Add(selectionRect);
		}

		private void Selection_OnChanging(object sender, EventArgs e)
		{
			UpdateSelection();
		}

		private void Selection_OnChange(object sender, EventArgs e)
		{
			UpdateSelection();
		}

		private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (Selection.Mode == SelectionModes.DraggingToSelect)
			{
				cvsSelection.ReleaseMouseCapture();
				Title = "Mouse up detected. Selection complete.";
				Point position = e.GetPosition(cvsSelection);
				Selection.Cursor = chartTranslator.GetTimeFromX(position.X);
				Selection.Mode = SelectionModes.Normal;
				Selection.Changed();
			}
		}
		void UpdateSelectionIfNeeded(MouseEventArgs e)
		{
			if (Selection.Mode == SelectionModes.DraggingToSelect)
			{
				Point position = e.GetPosition(cvsSelection);
				Title = $"Moving mouse ({position.X}, {position.Y})";
				Selection.Cursor = chartTranslator.GetTimeFromX(position.X);
				Selection.Changing();
			}
		}
	}
}

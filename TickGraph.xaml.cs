using BotTraderCore;
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
	/// Interaction logic for TickGraph.xaml
	/// </summary>
	public partial class TickGraph : UserControl
	{
		Point lastMousePosition;

		const double INT_DotDiameter = 6;
		const double INT_DotRadius = INT_DotDiameter / 2;

		ChartTranslator chartTranslator;
		public TickGraph()
		{
			InitializeComponent();
			HookEvents();
		}
		public void Clear()
		{
			cvsMain.Children.Clear();
		}

		public void AddElement(FrameworkElement element)
		{
			cvsMain.Children.Add(element);
		}

		public void InsertElement(FrameworkElement element)
		{
			cvsMain.Children.Insert(0, element);
		}

		public void AddAdornment(FrameworkElement element)
		{
			cvsAdornments.Children.Add(element);
		}

		private bool OnLeftSide(double x)
		{
			return x < chartTranslator.chartWidthPixels / 2;
		}

		double GetX(UIElement uIElement)
		{
			return Canvas.GetLeft(uIElement) + INT_DotRadius;
		}

		double GetY(UIElement uIElement)
		{
			return Canvas.GetTop(uIElement) + INT_DotRadius;
		}

		public void ShowHintData(double x, double y, StockDataPoint nearestPoint)
		{
			string symbol = nearestPoint.Tick.Symbol;
			string currency = string.Empty;
			int dashIndex = symbol.IndexOf("-");
			if (dashIndex >= 0)
				currency = symbol.Substring(dashIndex + 1);
			tbTradePrice.Text = $"{nearestPoint.Tick.LastTradePrice.GetNum()} {currency}";
			tbHighestBid.Text = $"{nearestPoint.Tick.HighestBidPrice.GetNum()} {currency}";
			tbLowestAsk.Text = $"{nearestPoint.Tick.LowestAskPrice.GetNum()} {currency}";
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

		void ChartPoints(Canvas canvas, List<PointXY> smallMovingAverage, SolidColorBrush brush, int lineThickness)
		{
			double lastX = double.MinValue;
			double lastY = double.MinValue;

			foreach (PointXY point in smallMovingAverage)
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

		private void AddDot(double lastY, double x, double y, StockDataPoint stockDataPoint)
		{
			Ellipse dot = new Ellipse() { Fill = new SolidColorBrush(GetFillColor(lastY, y, 128)), Width = INT_DotDiameter, Height = INT_DotDiameter };
			Canvas.SetLeft(dot, x - INT_DotRadius);
			Canvas.SetTop(dot, y - INT_DotRadius);
			AddElement(dot);
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

		private void AddLine(double lastX, double lastY, double x, double y)
		{
			Line line = CreateLine(lastX, lastY, x, y);
			line.Stroke = new SolidColorBrush(GetFillColor(lastY, y, 255));
			InsertElement(line);  // All lines go to the back.
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

		private void AddDashedLine(Point position)
		{
			DateTime time = chartTranslator.GetTime(position.X);
			TextBlock timeTextBlock = new TextBlock();
			timeTextBlock.Text = time.ToString("dd MMM yyyy - hh:mm:ss.ff");

			AddAdornment(timeTextBlock);

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

		public void SetChartTranslator(ChartTranslator chartTranslator)
		{
			this.chartTranslator = chartTranslator;
		}
		public void ClearAdornments()
		{
			cvsAdornments.Children.Clear();
		}

		public void DrawGraph()
		{
			double lastX = double.MinValue;
			double lastY = double.MinValue;

			bool alreadyDrawnAtLeastOnePoint = false;

			Clear();

			List<StockDataPoint> stockDataPoints = chartTranslator.GetStockDataPoints();
			foreach (StockDataPoint stockDataPoint in stockDataPoints)
			{
				double x = chartTranslator.GetStockPositionX(stockDataPoint.Time);
				double y = chartTranslator.GetStockPositionY(stockDataPoint.Tick.LastTradePrice);
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

			// TODO: if the mouse is down...
			//if (mouseIsDown (Selection is active))
			//Selection.Cursor = chartTranslator.GetTimeFromX(lastMousePosition.X);
			//Selection.Changing();

			UpdateSelection();
			DrawAnalysisCharts();
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
		private void DrawAnalysisCharts()
		{
			cvsAnalysis.Children.Clear();

			//chartTranslator.AddMovingAverage(20, cvsAnalysis, new SolidColorBrush(Color.FromArgb(127, 27, 0, 163)));
			//chartTranslator.AddMovingAverage(200, cvsAnalysis, new SolidColorBrush(Color.FromArgb(127, 0, 178, 33)));

			List<PointXY> smallMovingAverage = chartTranslator.GetMovingAverages(20);
			ChartPoints(cvsAnalysis, smallMovingAverage, new SolidColorBrush(Color.FromArgb(127, 27, 0, 163)), 4);

			List<PointXY> largerMovingAverage = chartTranslator.GetMovingAverages(100);
			ChartPoints(cvsAnalysis, largerMovingAverage, new SolidColorBrush(Color.FromArgb(127, 34, 171, 0)), 8);

			//chartTranslator.AddMovingAverage(5, cvsMain, new SolidColorBrush(Color.FromArgb(127, 0, 255, 47)));
		}

		public void HandleMouseMove(MouseEventArgs e)
		{
			lastMousePosition = e.GetPosition(cvsAdornments);
			AddAdornments(lastMousePosition);
			UpdateSelectionIfNeeded(e);
		}

		void UpdateSelectionIfNeeded(MouseEventArgs e)
		{
			if (Selection.Mode == SelectionModes.DraggingToSelect)
			{
				Point position = e.GetPosition(cvsSelection);
				Selection.Cursor = chartTranslator.GetTimeFromX(position.X);
				Selection.Changing();
			}
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
			AddAdornment(ellipse);
		}

		private void AddAdornments(Point position)
		{
			ClearAdornments();

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
					//Title = $"{nearestPoint.Tick.LastTradePrice}";
					ShowHintData(x, y, nearestPoint);
				}
				else
				{
					//Title = "Move mouse near point to see value!";
				}
			}
		}
		public void HandleMouseDown(MouseButtonEventArgs e)
		{
			Point position = e.GetPosition(cvsSelection);
			if (Selection.IsInBounds(position.X, position.Y))
			{
				Selection.Mode = SelectionModes.DraggingToSelect;
				Selection.Anchor = chartTranslator.GetTimeFromX(position.X);
				cvsSelection.CaptureMouse();
			}
		}

		private void Selection_OnChanging(object sender, EventArgs e)
		{
			UpdateSelection();
		}

		private void Selection_OnChange(object sender, EventArgs e)
		{
			UpdateSelection();
		}

		private void HookEvents()
		{
			Selection.OnChange += Selection_OnChange;
			Selection.OnChanging += Selection_OnChanging;
		}

		public void HandleMouseUp(MouseButtonEventArgs e)
		{
			if (Selection.Mode == SelectionModes.DraggingToSelect)
			{
				cvsSelection.ReleaseMouseCapture();
				Point position = e.GetPosition(cvsSelection);
				Selection.Cursor = chartTranslator.GetTimeFromX(position.X);
				Selection.Mode = SelectionModes.Normal;
				Selection.Changed();
			}
		}

		private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			HandleMouseDown(e);
		}

		private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			HandleMouseMove(e);
		}

		private void UserControl_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			HandleMouseUp(e);
		}
	}
}

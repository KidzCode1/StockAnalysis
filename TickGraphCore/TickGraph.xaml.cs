﻿using BotTraderCore;
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

namespace TickGraphCore
{
	/// <summary>
	/// Interaction logic for TickGraph.xaml
	/// </summary>
	public partial class TickGraph : UserControl
	{
		Color verticalTimeLineColor = Color.FromArgb(200, 115, 115, 115);
		Color horizontalPriceLineColor = Color.FromArgb(200, 115, 115, 115);

		public Selection Selection { get; set; } = new Selection();

		Point lastMousePosition;

		public void SelectAll()
		{
			Selection.Set(chartTranslator.Start, chartTranslator.End);
		}

		public static readonly DependencyProperty ShowAnalysisProperty = DependencyProperty.Register("ShowAnalysis", typeof(bool), typeof(TickGraph), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnShowAnalysisChanged)));
		
		
		public bool ShowAnalysis
		{
			// IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
			get => (bool)GetValue(ShowAnalysisProperty);
			set => SetValue(ShowAnalysisProperty, value);
		}


		const double INT_DotDiameter = 6;
		const double INT_DotRadius = INT_DotDiameter / 2;
		double chartHeightPixels;
		double chartWidthPixels;

		ChartTranslator chartTranslator;
		public TickGraph()
		{
			InitializeComponent();
			HookEvents();
		}

		private static void OnShowAnalysisChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			TickGraph tickGraph = o as TickGraph;
			if (tickGraph != null)
				tickGraph.OnShowAnalysisChanged((bool)e.OldValue, (bool)e.NewValue);
		}

		protected virtual void OnShowAnalysisChanged(bool oldValue, bool newValue)
		{
			if (ShowAnalysis)
				cvsAnalysis.Visibility = Visibility.Visible;
			else
				cvsAnalysis.Visibility = Visibility.Hidden;
			DrawGraph();
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

		public void AddCoreAdornment(FrameworkElement element)
		{
			cvsCoreAdornments.Children.Add(element);
		}

		public void AddCustomAdornment(FrameworkElement element)
		{
			cvsCustomAdornments.Children.Add(element);
		}

		private bool OnLeftSide(double x)
		{
			return x < chartWidthPixels / 2;
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

		private void AddVerticalTimeLine(Point position)
		{
			if (chartTranslator == null)
				return;
			DateTime time = chartTranslator.GetTime(position.X, chartWidthPixels);
			TextBlock timeTextBlock = new TextBlock();
			timeTextBlock.Text = time.ToString("dd MMM yyyy - hh:mm:ss.ff");

			AddCoreAdornment(timeTextBlock);

			if (position.X > chartWidthPixels / 2)
			{
				Size size = MeasureString(timeTextBlock);
				// Right-align this.
				Canvas.SetLeft(timeTextBlock, position.X - size.Width - 5);
			}
			else  // Left-align:
				Canvas.SetLeft(timeTextBlock, position.X + 5);
			
			Line line = CreateVerticalDashedLineAtX(position.X, verticalTimeLineColor);
			cvsCoreAdornments.Children.Add(line);
		}

		private void AddHorizontalPriceLine(Point position)
		{
			if (chartTranslator == null)
				return;
			decimal price = chartTranslator.GetPrice(position.Y, chartHeightPixels);
			TextBlock priceTextBlock = new TextBlock();
			priceTextBlock.Text = price.ToString();  // No currency yet here.
			AddCoreAdornment(priceTextBlock);

			if (position.Y > chartHeightPixels / 2)
			{
				Canvas.SetTop(priceTextBlock, position.Y - 20);
			}
			else  // Left-align:
				Canvas.SetTop(priceTextBlock, position.Y + 5);
			
			Line line = CreateHorizontalDashedLineAtY(position.Y, horizontalPriceLineColor);
			cvsCoreAdornments.Children.Add(line);
		}

		public Line CreateVerticalDashedLineAtX(double x, Color color)
		{
			Line line = CreateLine(x, 0, x, chartHeightPixels);
			line.IsHitTestVisible = false;
			line.Stroke = new SolidColorBrush(color);
			line.StrokeDashArray.Add(5);
			line.StrokeDashArray.Add(3);
			return line;
		}

		public Line CreateHorizontalDashedLineAtY(double y, Color color)
		{
			Line line = CreateLine(0, y, chartWidthPixels, y);
			line.IsHitTestVisible = false;
			line.Stroke = new SolidColorBrush(color);
			line.StrokeDashArray.Add(5);
			line.StrokeDashArray.Add(3);
			return line;
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

		public void ClearCoreAdornments()
		{
			cvsCoreAdornments.Children.Clear();
		}

		public void ClearCustomAdornments()
		{
			cvsCustomAdornments.Children.Clear();
		}


		void DrawCustomAdornments(List<CustomAdornment> customAdornments)
		{
			ClearCustomAdornments();
			if (customAdornments == null)
				return;

			foreach (CustomAdornment customAdornment in customAdornments)
				DrawAdornment(customAdornment);
		}

		private void DrawAdornment(CustomAdornment customAdornment)
		{
			Point point = GetAdornmentPoint(customAdornment);

			AddDashedLineIfNecessary(customAdornment, point);

			PositionIcon(customAdornment, point);
			DrawLabel(customAdornment, point);
		}

		void DrawLabel(CustomAdornment customAdornment, Point point)
		{
			TextBlock textBlock = new TextBlock();
			textBlock.Text = customAdornment.Name;
			switch (customAdornment.LabelAlignment)
			{
				case LabelAlignment.Left:
					textBlock.HorizontalAlignment = HorizontalAlignment.Left;
					break;
				case LabelAlignment.Center:
					textBlock.HorizontalAlignment = HorizontalAlignment.Center;
					break;
				case LabelAlignment.Right:
					textBlock.HorizontalAlignment = HorizontalAlignment.Right;
					break;
			}
			Canvas.SetLeft(textBlock, point.X + customAdornment.LabelLeftOffset);
			Canvas.SetTop(textBlock, point.Y + customAdornment.LabelTopOffset);
			AddCustomAdornment(textBlock);
		}

		private void PositionIcon(CustomAdornment customAdornment, Point point)
		{
			Viewbox icon = GetIcon(customAdornment);

			double left = point.X + customAdornment.IconLeftOffset;
			double top = point.Y + customAdornment.IconTopOffset;
			Canvas.SetLeft(icon, left);
			Canvas.SetTop(icon, top);
			AddCustomAdornment(icon);
		}

		private Point GetAdornmentPoint(CustomAdornment customAdornment)
		{
			double x = chartTranslator.GetStockPositionX(customAdornment.Time, chartWidthPixels);
			double y = chartTranslator.GetStockPositionY(customAdornment.Price, chartHeightPixels);
			Point adornmentPoint = new Point(x, y);
			return adornmentPoint;
		}

		private void AddDashedLineIfNecessary(CustomAdornment customAdornment, Point point)
		{
			if (customAdornment.DashedLineOption == DashedLineOption.Vertical)
			{
				Line dashedLine = CreateVerticalDashedLineAtX(point.X, customAdornment.Color);
				AddCustomAdornment(dashedLine);
			}
			if (customAdornment.DashedLineOption == DashedLineOption.Horizontal)
			{
				Line dashedLine = CreateHorizontalDashedLineAtY(point.Y, customAdornment.Color);
				AddCustomAdornment(dashedLine);
			}
		}

		void ChangeColor(Canvas canvas, Color color)
		{
			if (canvas == null)
				return;
			foreach (UIElement uIElement in canvas.Children)
			{
				if (uIElement is Polygon polygon)
				{
					if (polygon.Fill != null)
						polygon.Fill = new SolidColorBrush(color);
					if (polygon.Stroke != null)
						polygon.Stroke = new SolidColorBrush(color);
				}
				if (uIElement is Canvas childCanvas)
					ChangeColor(childCanvas, color);
			}
		}

		private Viewbox GetIcon(CustomAdornment customAdornment)
		{
			Viewbox iconTimePoint = FindResource(customAdornment.Key) as Viewbox;
			iconTimePoint.Width = customAdornment.Size;
			ChangeColor(iconTimePoint.Child as Canvas, customAdornment.Color);
			return iconTimePoint;
		}

		public void DrawGraph(List<CustomAdornment> customAdornments = null)
		{
			double lastX = double.MinValue;
			double lastY = double.MinValue;

			bool alreadyDrawnAtLeastOnePoint = false;

			Clear();

			if (chartTranslator == null)
				return;

			List<StockDataPoint> stockDataPoints = chartTranslator.GetAllStockDataPoints();
			foreach (StockDataPoint stockDataPoint in stockDataPoints)
			{
				double x = chartTranslator.GetStockPositionX(stockDataPoint.Time, chartWidthPixels);
				double y = chartTranslator.GetStockPositionY(stockDataPoint.Tick.LastTradePrice, chartHeightPixels);
				AddDot(lastY, x, y, stockDataPoint);

				if (alreadyDrawnAtLeastOnePoint)
				{
					AddLine(lastX, lastY, x, y);
				}

				alreadyDrawnAtLeastOnePoint = true;

				lastX = x;
				lastY = y;
			}

			AddCoreAdornments(lastMousePosition);

			// TODO: if the mouse is down...
			//if (mouseIsDown (Selection is active))
			//Selection.Cursor = chartTranslator.GetTimeFromX(lastMousePosition.X);
			//Selection.Changing();

			UpdateSelection();
			DrawAnalysisCharts();
			DrawCustomAdornments(customAdornments);
		}

		void UpdateSelection()
		{
			cvsSelection.Children.Clear();
			if (!Selection.Exists)
				return;

			if (chartTranslator == null)
				return;

			Rectangle selectionRect = new Rectangle();
			selectionRect.Fill = new SolidColorBrush(Color.FromArgb(73, 54, 127, 255));
			Canvas.SetTop(selectionRect, 0);
			selectionRect.Height = cvsSelection.ActualHeight;

			double leftSide = chartTranslator.GetStockPositionX(Selection.Start, chartWidthPixels);  // 750
			double rightSide = chartTranslator.GetStockPositionX(Selection.End, chartWidthPixels);   // 1100

			Canvas.SetLeft(selectionRect, leftSide);

			selectionRect.Width = rightSide - leftSide;

			cvsSelection.Children.Add(selectionRect);
		}
		private void DrawAnalysisCharts()
		{
			if (!ShowAnalysis)
				return;

			cvsAnalysis.Children.Clear();

			if (chartTranslator == null)
				return;

			//chartTranslator.AddMovingAverage(20, cvsAnalysis, new SolidColorBrush(Color.FromArgb(127, 27, 0, 163)));
			//chartTranslator.AddMovingAverage(200, cvsAnalysis, new SolidColorBrush(Color.FromArgb(127, 0, 178, 33)));

			List<PointXY> smallMovingAverage = chartTranslator.GetMovingAverages(20, chartWidthPixels, chartHeightPixels);
			ChartPoints(cvsAnalysis, smallMovingAverage, new SolidColorBrush(Color.FromArgb(127, 27, 0, 163)), 4);

			List<PointXY> largerMovingAverage = chartTranslator.GetMovingAverages(100, chartWidthPixels, chartHeightPixels);
			ChartPoints(cvsAnalysis, largerMovingAverage, new SolidColorBrush(Color.FromArgb(127, 34, 171, 0)), 8);

			//chartTranslator.AddMovingAverage(5, cvsMain, new SolidColorBrush(Color.FromArgb(127, 0, 255, 47)));
		}

		public void HandleMouseMove(MouseEventArgs e)
		{
			lastMousePosition = e.GetPosition(cvsCoreAdornments);
			AddCoreAdornments(lastMousePosition);
			UpdateSelectionIfNeeded(e);
		}

		void UpdateSelectionIfNeeded(MouseEventArgs e)
		{
			if (Selection.Mode == SelectionModes.DraggingToSelect)
			{
				if (chartTranslator == null)
					return;
				Point position = e.GetPosition(cvsSelection);
				Selection.SetChangingCursor(chartTranslator.GetTime(position.X, chartWidthPixels));
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
			AddCoreAdornment(ellipse);
		}

		private void AddCoreAdornments(Point position)
		{
			ClearCoreAdornments();

			AddVerticalTimeLine(position);
			AddHorizontalPriceLine(position);

			if (chartTranslator == null)
				return;

			//DateTime mouseTime = chartTranslator.GetTimeFromX(position.X, chartWidthPixels);
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
			if (e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Pressed)
				return;
			if (chartTranslator == null)
				return;

			Point position = e.GetPosition(cvsSelection);
			if (Selection.IsInBounds(position.X, position.Y))
			{
				Selection.Mode = SelectionModes.DraggingToSelect;
				Selection.Anchor = chartTranslator.GetTime(position.X, chartWidthPixels);
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
			if (chartTranslator == null)
				return;

			if (Selection.Mode == SelectionModes.DraggingToSelect)
			{
				cvsSelection.ReleaseMouseCapture();
				Point position = e.GetPosition(cvsSelection);
				Selection.SetFinalCursor(chartTranslator.GetTime(position.X, chartWidthPixels));
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

		void SetSize(FrameworkElement element)
		{
			element.Width = chartWidthPixels;
			element.Height = chartHeightPixels;
		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			double horizontalMargin = grdContainer.Margin.Left + grdContainer.Margin.Right;
			double verticalMargin = grdContainer.Margin.Top + grdContainer.Margin.Bottom;

			chartWidthPixels = e.NewSize.Width - horizontalMargin;
			chartHeightPixels = e.NewSize.Height - verticalMargin - 100;

			// Tip: Ctrl+K, Ctrl+D to format the document!
			SetSize(cvsBackground);
			SetSize(rctBackground);
			SetSize(cvsMain);
			SetSize(cvsCoreAdornments);
			SetSize(cvsCustomAdornments);
			SetSize(cvsSelection);
			SetSize(cvsAnalysis);
			SetSize(cvsHints);
			SetSize(grdContainer);
			DrawGraph();
		}

		public DateTime GetTimeAtMouse()
		{
			return chartTranslator.GetTime(lastMousePosition.X, chartWidthPixels);
		}

		public decimal GetPriceAtMouse()
		{
			return chartTranslator.GetPrice(lastMousePosition.Y, chartHeightPixels);
		}

		public void SaveData(string fullPathToFile)
		{
			chartTranslator.SaveAll(fullPathToFile);

			//List<StockDataPoint> loadedPoints = StockDataPoint.Load(fullPathToFile);

			//if (selectedPoints.Matches(loadedPoints))
			//{
			//	Title = "It worked!";
			//}
			//else
			//	Title = "Failure!";
		}
		public void HideCoreAdornments()
		{
			cvsCoreAdornments.Visibility = Visibility.Hidden;
			cvsHints.Visibility = Visibility.Hidden;
		}

		public void ShowCoreAdornments()
		{
			cvsCoreAdornments.Visibility = Visibility.Visible;
			cvsHints.Visibility = Visibility.Visible;
		}
	}
}
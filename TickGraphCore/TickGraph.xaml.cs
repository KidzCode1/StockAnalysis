using System;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using BotTraderCore;
using System.Windows.Documents;

namespace TickGraphCore
{
	/// <summary>
	/// Interaction logic for TickGraph.xaml
	/// </summary>
	public partial class TickGraph : UserControl
	{
		
		Color verticalTimeLineColor = Color.FromArgb(200, 115, 115, 115);
		Color horizontalPriceLineColor = Color.FromArgb(200, 115, 115, 115);
		public int MaxDataDensity { get; set; } = INT_MaxDataDensity;
		public Selection Selection { get; set; } = new Selection();

		Point lastMousePosition;

		public void SelectAll()
		{
			Selection.Set(chartTranslator.TradeHistory.Start, chartTranslator.TradeHistory.End);
		}

		public static readonly DependencyProperty ShowAnalysisProperty = DependencyProperty.Register("ShowAnalysis", typeof(bool), typeof(TickGraph), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnShowAnalysisChanged)));
		
		public bool ShowAnalysis
		{
			// IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
			get => (bool)GetValue(ShowAnalysisProperty);
			set => SetValue(ShowAnalysisProperty, value);
		}

		public static readonly DependencyProperty UseChangeSummariesProperty = DependencyProperty.Register("UseChangeSummaries", typeof(bool), typeof(TickGraph), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnUseChangeSummariesChanged)));

		public bool UseChangeSummaries
		{
			// IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
			get => (bool)GetValue(UseChangeSummariesProperty);
			set => SetValue(UseChangeSummariesProperty, value);
		}

		public ITradeHistory TradeHistory => chartTranslator?.TradeHistory;

		const double INT_DotDiameter = 6;
		const double INT_DotRadius = INT_DotDiameter / 2;
		const double INT_BuySignalDiameter = INT_DotDiameter * 3;
		const double INT_BuySignalRadius = INT_BuySignalDiameter / 2;
		const double INT_BuySignalThickness = 2;
		const int INT_MinDataDensity = 8;
		const int INT_MaxDataDensity = 150;

		double chartHeightPixels;
		double chartWidthPixels;

		ChartTranslator chartTranslator;
		bool mouseIsInsideGraph;
		List<DataPoint> denseDataPoints;
		List<CustomAdornment> lastCustomAdornments;
		readonly static SolidColorBrush noChangeBrushSolid = new SolidColorBrush(Color.FromArgb(255, 164, 74, 255));
		readonly static SolidColorBrush noChangeBrushHalfOpacity = new SolidColorBrush(Color.FromArgb(128, 164, 74, 255));
		readonly static SolidColorBrush riseBrushSolid = new SolidColorBrush(Color.FromArgb(255, 0, 75, 125));
		readonly static SolidColorBrush riseBrushHalfOpacity = new SolidColorBrush(Color.FromArgb(128, 0, 75, 125));
		readonly static SolidColorBrush fallBrushSolid = new SolidColorBrush(Color.FromRgb(196, 47, 47));
		readonly static SolidColorBrush fallBrushHalfOpacity = new SolidColorBrush(Color.FromArgb(128, 196, 47, 47));

		public TickGraph()
		{
			MaxDataDensity = INT_MaxDataDensity;
			InitializeComponent();
			HookEvents();
		}

		private static void OnShowAnalysisChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			if (o is TickGraph tickGraph)
				tickGraph.OnShowAnalysisChanged((bool)e.OldValue, (bool)e.NewValue);
		}

		private static void OnUseChangeSummariesChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			if (o is TickGraph tickGraph)
				tickGraph.OnUseChangeSummariesChanged((bool)e.OldValue, (bool)e.NewValue);
		}

		protected virtual void OnUseChangeSummariesChanged(bool oldValue, bool newValue)
		{
			if (chartTranslator.TradeHistory is IUpdatableTradeHistory updatableTradeHistory)
				updatableTradeHistory.ChangedSinceLastDataDensityQuery = true;
			DrawGraph();
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

		public bool OnLeftSide(double x)
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

		void ShowCallout(Viewbox viewbox, double dotX, double dotY)
		{
			vbHintLL.Visibility = Visibility.Hidden;
			vbHintLR.Visibility = Visibility.Hidden;
			vbHintUR.Visibility = Visibility.Hidden;
			vbHintUL.Visibility = Visibility.Hidden;
			double calloutOffsetX = 0;
			double calloutOffsetY = 0;

			if (viewbox == vbHintLL)
			{
				vbHintLL.Visibility = Visibility.Visible;
				calloutOffsetY = -118;
			}
			else if (viewbox == vbHintLR)
			{
				vbHintLR.Visibility = Visibility.Visible;
				calloutOffsetY = -118;
				calloutOffsetX = -220;
			}
			else if (viewbox == vbHintUR)
			{
				vbHintUR.Visibility = Visibility.Visible;
				calloutOffsetY = 22;
				calloutOffsetX = -220;
			}
			else if (viewbox == vbHintUL)
			{
				vbHintUL.Visibility = Visibility.Visible;
				calloutOffsetY = 22;
			}

			Canvas.SetLeft(cvsHints, dotX + calloutOffsetX);
			Canvas.SetTop(cvsHints, dotY + calloutOffsetY);

			cvsHints.Visibility = Visibility.Visible;
		}

		string GetUSD(decimal lastTradePrice, int decimals = 9)
		{
			return (TradeHistory.QuoteCurrencyToUsdConversion * lastTradePrice).GetNum(decimals);
		}

		public void ShowHintData(double x, double y, DataPoint nearestPoint)
		{
			CustomTick tick = nearestPoint.Tick;

			tbTradePrice.Text = $"${GetUSD(tick.LastTradePrice)}";
			if (tick.QuoteVolume == 0)
				tbTradeVolume.Text = "(not available)";
			else
				tbTradeVolume.Text = $"${GetUSD(tick.QuoteVolume, 0)}";
			tbHighestBid.Text = $"${GetUSD(tick.HighestBidPrice)}";
			tbLowestAsk.Text = $"${GetUSD(tick.LowestAskPrice)}";

			tbDate.Text = $"{nearestPoint.Time:yyy MMM dd}";

			tbTimeHoursMinutesSeconds.Text = $"{nearestPoint.Time:hh:mm:ss}";
			tbTimeFraction.Text = $"{nearestPoint.Time:fff}";

			grdStockTickDetails.Visibility = Visibility.Visible;

			double dotX = x;
			double dotY = y;
			if (dotX < cvsMain.ActualWidth / 2.0)
				if (dotY < cvsMain.ActualHeight / 2.0)
					ShowCallout(vbHintUL, dotX, dotY);  // Upper Left
				else
					ShowCallout(vbHintLL, dotX, dotY);
			else if (dotY < cvsMain.ActualHeight / 2.0)
				ShowCallout(vbHintUR, dotX, dotY);  // Upper Right
			else
				ShowCallout(vbHintLR, dotX, dotY);  // Lower Right
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

		void AddBuySignal(double x, double y, DataPoint dataPoint)
		{
			Ellipse buySignal = new Ellipse() { Stroke = Brushes.Blue, Opacity = 0.4, Width = INT_BuySignalDiameter, Height = INT_BuySignalDiameter, StrokeThickness = INT_BuySignalThickness };
			Canvas.SetLeft(buySignal, x - INT_BuySignalRadius);
			Canvas.SetTop(buySignal, y - INT_BuySignalRadius);
			AddElement(buySignal);
			buySignal.Tag = dataPoint;
		}

		private void AddDot(double lastY, double x, double y, DataPoint stockDataPoint)
		{
			Ellipse dot = new Ellipse() { Fill = GetFillBrush(lastY, y, 128), Width = INT_DotDiameter, Height = INT_DotDiameter };
			Canvas.SetLeft(dot, x - INT_DotRadius);
			Canvas.SetTop(dot, y - INT_DotRadius);
			AddElement(dot);
			dot.Tag = stockDataPoint;
		}

		private static SolidColorBrush GetFillBrush(double lastY, double y, byte opacity)
		{
			if (lastY == double.MinValue || lastY == y /* IsClose(lastY, y) */)
				if (opacity == 255)
					return noChangeBrushSolid;
				else
					return noChangeBrushHalfOpacity;
			if (lastY > y)
				if (opacity == 255)
					return riseBrushSolid;
				else
					return riseBrushHalfOpacity;

			if (opacity == 255)
				return fallBrushSolid;
			return fallBrushHalfOpacity;
		}

		private void AddLine(double lastX, double lastY, double x, double y)
		{
			Line line = CreateLine(lastX, lastY, x, y);
			line.Stroke = GetFillBrush(lastY, y, 255);
			InsertElement(line);  // All lines go to the back.
		}

		private static Line CreateLine(double lastX, double lastY, double x, double y, double lineThickness = 1)
		{
			Line line = new Line() { 
										X1 = lastX, 
										Y1 = lastY, 
										X2 = x, 
										Y2 = y, 
										StrokeThickness = lineThickness };
			return line;
		}

		private void AddVerticalTimeLine(Point position)
		{
			if (chartTranslator == null || !mouseIsInsideGraph)
				return;
			DateTime time = chartTranslator.GetTime(position.X, chartWidthPixels);
			if (time >= chartTranslator.TradeHistory.End || time <= chartTranslator.TradeHistory.Start)
				return;

			TextBlock timeTextBlock = new TextBlock() { Text = time.ToString("dd MMM yyyy - hh:mm:ss.ff") };

			AddCoreAdornment(timeTextBlock);

			if (position.X > chartWidthPixels / 2)
			{
				Size size = MeasureString(timeTextBlock);
				// Right-align this.
				Canvas.SetLeft(timeTextBlock, position.X - size.Width - 5);
			}
			else  // Left-align:
				Canvas.SetLeft(timeTextBlock, position.X + 5);

			Canvas.SetTop(timeTextBlock, -15);

			Line line = CreateVerticalDashedLineAtX(position.X, verticalTimeLineColor);
			cvsCoreAdornments.Children.Add(line);
		}

		private void AddHorizontalPriceLine(Point position)
		{
			if (chartTranslator == null || !mouseIsInsideGraph)
				return;
			decimal price = chartTranslator.GetPrice(position.Y, chartHeightPixels);
			if (price <= chartTranslator.TradeHistory.Low || price >= chartTranslator.TradeHistory.High)
				return;
			TextBlock priceTextBlock = new TextBlock() { Text = $" {Math.Round(price, 2):C}"  /* No currency yet here.*/};
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
			line.Opacity = 0.5;
			line.StrokeDashArray.Add(5);
			line.StrokeDashArray.Add(3);
			return line;
		}

		public Line CreateHorizontalDashedLineAtY(double y, Color color)
		{
			Line line = CreateLine(0, y, chartWidthPixels, y);
			line.IsHitTestVisible = false;
			line.Stroke = new SolidColorBrush(color);
			line.Opacity = 0.5;
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
			TextBlock textBlock = new TextBlock() { Text = customAdornment.Name };

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
			lastCustomAdornments = customAdornments;
			Clear();

			if (chartTranslator == null)
				return;

			int dataDensity = (int)Math.Round(sldDataDensity.Value);

			if (dataDensity == 0 && chartTranslator.TradeHistory.DataPointCount > MaxDataDensity)
				dataDensity = MaxDataDensity;

			List<DataPoint> buySignals;

			if (dataDensity > INT_MinDataDensity - 3)
			{
				if (denseDataPoints == null || (chartTranslator.TradeHistory as IUpdatableTradeHistory)?.ChangedSinceLastDataDensityQuery == true)
					denseDataPoints = chartTranslator.TradeHistory.GetDataPointsAcrossSegments(dataDensity, UseChangeSummaries);

				DrawDataPoints(denseDataPoints);
				buySignals = chartTranslator.TradeHistory.BuySignals.ToList();
			}
			else
			{
				DataPointsSnapshot stockDataPointSnapshot = chartTranslator.TradeHistory.GetSnapshot();
				DrawDataPoints(stockDataPointSnapshot.DataPoints);
				
				buySignals = stockDataPointSnapshot.BuySignals;
			}

			
			if (buySignals != null && buySignals.Count > 0)
			{
				DrawBuySignals(buySignals);
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

		void DrawBuySignals(List<DataPoint> buySignals)
		{
			if (buySignals == null)
				return;

			DataPoint dataPoint = buySignals.FirstOrDefault();
			if (dataPoint != null)
			{
				double x = chartTranslator.GetStockPositionX(dataPoint.Time, chartWidthPixels);
				double y = chartTranslator.GetStockPositionY(dataPoint.Tick.LastTradePrice, chartHeightPixels);
				AddBuySignal(x, y, dataPoint);
			}
		}

		private void DrawDataPoints(IEnumerable<DataPoint> stockDataPoints)
		{
			if (stockDataPoints == null)
				return;

			bool alreadyDrawnAtLeastOnePoint = false;
			double lastX = double.MinValue;
			double lastY = double.MinValue;
			foreach (DataPoint stockDataPoint in stockDataPoints)
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
		}

		void UpdateSelection()
		{
			cvsSelection.Children.Clear();
			if (!Selection.Exists)
				return;

			if (chartTranslator == null)
				return;

			Rectangle selectionRect = new Rectangle() { Fill = new SolidColorBrush(Color.FromArgb(73, 54, 127, 255)) };

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

		void AddPriceMarkers()
		{
			const double fontSize = 14;
			const int leftMargin = -80;
			const int leftItemWidth = -leftMargin - 10;
			TextBlock txLow = new TextBlock() { Text = $"${GetUSD(chartTranslator.TradeHistory.Low)}", Width = leftItemWidth, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, FontSize = fontSize };
			TextBlock txHigh = new TextBlock() { Text = $"${GetUSD(chartTranslator.TradeHistory.High)}", Width = leftItemWidth, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, FontSize = fontSize };

			AddCoreAdornment(txHigh, leftMargin, -fontSize);
			AddCoreAdornment(txLow, leftMargin, chartHeightPixels - fontSize);


			string amountInView = GetUSD(chartTranslator.TradeHistory.ValueSpan, 2);
			decimal percentInView = chartTranslator.TradeHistory.PercentInView;
			TextBlock txPercentInView = new TextBlock() { Text = $"{Math.Round(percentInView, 2)}%", Width = leftItemWidth, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, FontSize = fontSize * 1.2 };
			double amountFontSize = fontSize * 0.9;
			TextBlock txAmountInView = new TextBlock() { Text = $"(${amountInView})", Width = leftItemWidth, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = amountFontSize };
			AddCoreAdornment(txPercentInView, leftMargin + 5, chartHeightPixels / 2.0 - fontSize * 1.5 - amountFontSize / 2);
			AddCoreAdornment(txAmountInView, leftMargin + 20, chartHeightPixels / 2.0 - amountFontSize / 4);
		}

		private void AddCoreAdornment(FrameworkElement element, int left, double top)
		{
			Canvas.SetLeft(element, left);
			Canvas.SetTop(element, top);
			AddCoreAdornment(element);
		}

		private void AddCoreAdornments(Point position)
		{
			ClearCoreAdornments();

			if (chartTranslator == null)
				return;

			AddPriceMarkers();
			AddVerticalTimeLine(position);
			AddHorizontalPriceLine(position);

			if (mouseIsInsideGraph)
				AddPriceHint(position);
		}

		private void AddPriceHint(Point position)
		{
			Ellipse closestEllipse = GetClosestEllipse(position.X, position.Y);
			if (closestEllipse != null)
			{
				double x = GetX(closestEllipse);
				double y = GetY(closestEllipse);
				AddHighlightCircle(x, y);

				DataPoint nearestPoint = closestEllipse.Tag as DataPoint;
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

			Canvas.SetTop(vbUpDownArrows, e.NewSize.Height / 2 - 125);

			chartWidthPixels = e.NewSize.Width - horizontalMargin - 60;
			chartHeightPixels = e.NewSize.Height - verticalMargin - 100;

			SetSize(cvsBackground);
			SetSize(rctBackground);
			SetSize(cvsMain);
			SetSize(cvsAllHints);
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

		public void SaveTickRange(string fullPathToFile)
		{
			chartTranslator.TradeHistory.SaveTickRange(fullPathToFile);
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

		private void CvsBackground_MouseEnter(object sender, MouseEventArgs e)
		{
			mouseIsInsideGraph = true;
			RefreshGraph();
		}

		private void CvsBackground_MouseLeave(object sender, MouseEventArgs e)
		{
			mouseIsInsideGraph = false;
			vbHintLL.Visibility = Visibility.Hidden;
			vbHintLR.Visibility = Visibility.Hidden;
			vbHintUR.Visibility = Visibility.Hidden;
			vbHintUL.Visibility = Visibility.Hidden;
			grdStockTickDetails.Visibility = Visibility.Hidden;
			RefreshGraph();
		}

		void RefreshGraph()
		{
			DrawGraph(lastCustomAdornments);
		}

		void ClearDataDensityPoints()
		{
			denseDataPoints = null;
		}

		private void SldDataDensity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (sldDataDensity.Value < INT_MinDataDensity - 3)
			{
				ClearDataDensityPoints();
				DrawGraph();
				return;
			}
			denseDataPoints = chartTranslator.TradeHistory.GetDataPointsAcrossSegments((int)Math.Round(sldDataDensity.Value), UseChangeSummaries);
			DrawGraph();
		}

		public void SetTradeHistory(ITradeHistory tradeHistory)
		{
			if (chartTranslator == null)
				return;
			chartTranslator.TradeHistory = tradeHistory;
			ClearDataDensityPoints();
			DrawGraph();
		}
	}
}

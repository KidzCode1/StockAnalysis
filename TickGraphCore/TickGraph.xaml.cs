using System;
using System.Linq;
using System.Windows;
using System.Diagnostics;
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
		const double INT_AveragePriceThickness = 2;
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
			tbSymbolTitle.Text = "";
			tbVolumeAtBuySignal.Text = "";
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
			cvsBackLayer.Children.Clear();
		}

		public void AddElement(FrameworkElement element)
		{
			cvsMain.Children.Add(element);
		}

		public void AddBackLayerElement(FrameworkElement element)
		{
			cvsBackLayer.Children.Add(element);
		}

		public void InsertBackLayerElement(FrameworkElement element)
		{
			cvsBackLayer.Children.Add(element);
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

		string GetSmartUSD(decimal amount, int decimals = 9)
		{
			decimal adjustedAmount = TradeHistory.QuoteCurrencyToUsdConversion * amount;
			return "$" + GetAmountStr(adjustedAmount, decimals);
		}

		private static string GetAmountStr(decimal adjustedAmount, int decimals = 9)
		{
			if (adjustedAmount >= 1000)
				decimals = 0;
			else if (adjustedAmount >= 10)
				decimals = 2;
			else if (adjustedAmount >= 1)
				decimals = 3;
			else if (adjustedAmount >= 0.1m)
				decimals = 4;
			else if (adjustedAmount >= 0.01m)
				decimals = 5;
			else if (adjustedAmount >= 0.001m)
				decimals = 6;
			else if (adjustedAmount >= 0.0001m)
				decimals = 7;

			return adjustedAmount.GetNum(decimals);
		}

		public void ShowHintData(double x, double y, DataPoint nearestPoint)
		{
			CustomTick tick = nearestPoint.Tick;

			tbTradePrice.Text = $"{GetSmartUSD(tick.LastTradePrice)}";
			if (tick.QuoteVolume == 0)
				tbTradeVolume.Text = "(not available)";
			else
				tbTradeVolume.Text = $"{GetSmartUSD(tick.QuoteVolume, 0)}";
			tbHighestBid.Text = $"{GetSmartUSD(tick.HighestBidPrice)}";
			tbLowestAsk.Text = $"{GetSmartUSD(tick.LowestAskPrice)}";

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
			Ellipse dot = new Ellipse() { Fill = GetFillBrush(lastY, y, 128), Opacity = 0.5, Width = INT_DotDiameter, Height = INT_DotDiameter };
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

		void DrawHorizontalLine(decimal amount, SolidColorBrush brush, double opacity)
		{
			double y = GetStockPositionY(amount);
			Line line = CreateLine(0, y, chartWidthPixels, y);
			line.Stroke = brush;
			line.Opacity = opacity;
			AddBackLayerElement(line);
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
			double x = GetStockPositionX(customAdornment.Time);
			double y = GetStockPositionY(customAdornment.Price);
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

		/// <summary>
		/// Set in calls to DrawGraph
		/// </summary>
		decimal averagePrice;

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

			decimal standardDeviation;
			ITradeHistory tradeHistory;
			if (dataDensity > INT_MinDataDensity - 3)
			{
				if (denseDataPoints == null || (chartTranslator.TradeHistory as IUpdatableTradeHistory)?.ChangedSinceLastDataDensityQuery == true)
					denseDataPoints = chartTranslator.TradeHistory.GetDataPointsAcrossSegments(dataDensity, UseChangeSummaries);

				DrawDataPoints(denseDataPoints);
				tradeHistory = chartTranslator.TradeHistory;
			}
			else
			{
				DataPointsSnapshot stockDataPointSnapshot = chartTranslator.TradeHistory.GetSnapshot();
				DrawDataPoints(stockDataPointSnapshot.DataPoints);

				tradeHistory = stockDataPointSnapshot;
			}

			buySignals = tradeHistory.BuySignals.ToList();
			averagePrice = tradeHistory.AveragePriceAtBuySignal;
			if (averagePrice == 0)
				averagePrice = tradeHistory.AveragePrice;

			standardDeviation = tradeHistory.StandardDeviationAtBuySignal;

			if (buySignals != null && buySignals.Count > 0)
				DrawBuySignals(buySignals);

			

			if (standardDeviation == 0)
				standardDeviation = tradeHistory.StandardDeviation;

			DrawAveragePrice(tradeHistory, averagePrice, standardDeviation);

			DrawHorizontalLine(averagePrice * 1.03m, Brushes.DarkRed, 0.6);
			DrawHorizontalLine(averagePrice * 1.04m, Brushes.Green, 0.6);
			DrawHorizontalLine(averagePrice * 1.05m, Brushes.Blue, 0.15);
			DrawHorizontalLine(averagePrice * 1.06m, Brushes.Blue, 0.15);
			DrawHorizontalLine(averagePrice * 1.07m, Brushes.Blue, 0.15);

			AddCoreAdornments(lastMousePosition);

			UpdateSelection();
			DrawAnalysisCharts();
			DrawCustomAdornments(customAdornments);
		}

		void DrawAveragePrice(ITradeHistory tradeHistory, decimal averagePrice, decimal standardDeviation)
		{
			double left = GetStockPositionX(tradeHistory.Start);
			double right = GetStockPositionX(tradeHistory.End /* buySignal.Time */);
			double width = right - left;
			double averageY = GetStockPositionY(averagePrice);
			double plusOneStandardDeviation = GetStockPositionY(averagePrice + standardDeviation);
			double standardDeviationHeight = Math.Abs(plusOneStandardDeviation - averageY);
			AddStandardDeviationRectangle(left, width, averageY, standardDeviationHeight * 3, 0.05);
			AddStandardDeviationRectangle(left, width, averageY, standardDeviationHeight * 2);
			AddStandardDeviationRectangle(left, width, averageY, standardDeviationHeight);
			AddStandardDeviationRectangle(left, width, averageY, 2);
			DrawHorizontalLine(averagePrice + 3 * standardDeviation, Brushes.Orange, 0.5);
		}

		void AddStandardDeviationRectangle(double left, double width, double top, double halfHeight, double opacityOverride = 0.2)
		{
			double y = top - halfHeight;
			double height = halfHeight * 2;
			if (y < 0)
			{
				height += y;
				y = 0;
			}

			if (y + height > chartHeightPixels)
				height = chartHeightPixels - y;

			if (height < 0)
				return;

			Rectangle averagePrice = new Rectangle() { Width = width, Height = height, Fill = Brushes.Yellow, Stroke = Brushes.Orange, Opacity = opacityOverride, StrokeThickness = INT_AveragePriceThickness };
			averagePrice.IsHitTestVisible = false;
			cvsBackLayer.IsHitTestVisible = false;

			Canvas.SetLeft(averagePrice, left);
			Canvas.SetTop(averagePrice, y);
			AddBackLayerElement(averagePrice);
		}

		private double GetStockPositionX(DateTime time)
		{
			return chartTranslator.GetStockPositionX(time, chartWidthPixels);
		}

		private double GetStockPositionY(decimal price)
		{
			return chartTranslator.GetStockPositionY(price, chartHeightPixels);
		}

		void DrawBuySignals(List<DataPoint> buySignals)
		{
			if (buySignals == null)
				return;

			DataPoint dataPoint = buySignals.FirstOrDefault();
			if (dataPoint != null)	{
				double x = GetStockPositionX(dataPoint.Time);
				double y = GetStockPositionY(dataPoint.Tick.LastTradePrice);
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
				double x = GetStockPositionX(stockDataPoint.Time);
				double y = GetStockPositionY(stockDataPoint.Tick.LastTradePrice);
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

			double leftSide = GetStockPositionX(Selection.Start);  // 750
			double rightSide = GetStockPositionX(Selection.End);   // 1100

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

			if (Selection.Exists && Selection.Mode == SelectionModes.DraggingToSelect)
				tbStatus.Text = $"{GetSpanStr(Selection.Span)} selected";
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
			const int leftMargin = 80;
			const int highLowPriceMargin = 60;
			const int leftItemWidth = leftMargin - 10;
			TextBlock txLow = new TextBlock() { Text = $"{GetSmartUSD(chartTranslator.TradeHistory.Low)}", Width = leftItemWidth, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, FontSize = fontSize };
			TextBlock txHigh = new TextBlock() { Text = $"{GetSmartUSD(chartTranslator.TradeHistory.High)}", Width = leftItemWidth, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, FontSize = fontSize };

			while (MeasureString(txLow).Width > highLowPriceMargin)
				txLow.FontSize *= 0.9;

			while (MeasureString(txHigh).Width > highLowPriceMargin)
				txHigh.FontSize *= 0.9;

			AddCoreAdornment(txHigh, -leftMargin, -fontSize);
			AddCoreAdornment(txLow, -leftMargin, chartHeightPixels - fontSize);


			string amountInView = GetSmartUSD(chartTranslator.TradeHistory.ValueSpan, 2);
			decimal percentInView = chartTranslator.TradeHistory.PercentInView;
			TextBlock txPercentInView = new TextBlock() { Text = $"{Math.Round(percentInView, 2)}%", Width = leftItemWidth, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, FontSize = fontSize * 1.2 };
			double amountFontSize = fontSize * 0.9;
			TextBlock txAmountInView = new TextBlock() { Text = $"({amountInView})", Width = leftItemWidth, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = amountFontSize };
			AddCoreAdornment(txPercentInView, -leftMargin + 5, chartHeightPixels / 2.0 - fontSize * 1.5 - amountFontSize / 2);
			AddCoreAdornment(txAmountInView, -leftMargin + 20, chartHeightPixels / 2.0 - amountFontSize / 4);
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

			if (mouseIsInsideGraph && Selection.Mode != SelectionModes.DraggingToSelect)
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
				HideDataPointHint();
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

		string GetSpanStr(TimeSpan timeSpan)
		{
			if (timeSpan.TotalHours >= 1)
				if (timeSpan.Hours == 1)
					return $"1 hr {timeSpan.Minutes} min";
				else if (timeSpan.Minutes == 0)
					return $"{timeSpan.Hours} hrs";
				else
					return $"{timeSpan.Hours} hrs {timeSpan.Minutes} min";

			if (timeSpan.TotalMinutes >= 1)
				if (timeSpan.Minutes == 1)
					return $"1 min {timeSpan.Seconds} sec";
				else if (timeSpan.Seconds == 0)
					return $"{timeSpan.Minutes} min";
				else
					return $"{timeSpan.Minutes} min {timeSpan.Seconds} sec";

			if (timeSpan.Milliseconds == 0)
				return $"{timeSpan.Seconds} sec";

			return $"{timeSpan.Seconds} sec {timeSpan.Milliseconds} ms";
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

				UpdateStatusText();
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

			Canvas.SetTop(vbUpDownArrows, e.NewSize.Height / 2 - 95);

			chartWidthPixels = e.NewSize.Width - horizontalMargin - 60;
			chartHeightPixels = e.NewSize.Height - verticalMargin - 10;
			
			SetSize(cvsBackground);
			SetSize(rctBackground);
			SetSize(cvsMain);
			SetSize(cvsBackLayer);
			SetSize(cvsTitleLayer);
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
			HideDataPointHint();
			RefreshGraph();
		}

		private void HideDataPointHint()
		{
			vbHintLL.Visibility = Visibility.Hidden;
			vbHintLR.Visibility = Visibility.Hidden;
			vbHintUR.Visibility = Visibility.Hidden;
			vbHintUL.Visibility = Visibility.Hidden;
			grdStockTickDetails.Visibility = Visibility.Hidden;
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
			if (tradeHistory != null)
			{
				tbSymbolTitle.Text = tradeHistory.SymbolPair;
				if (tradeHistory.BuySignals != null && tradeHistory.BuySignals.Count > 0)
				{
					
					decimal quoteVolumeUsd = PriceConverter.GetPriceUsd(tradeHistory.SymbolPair) * tradeHistory.BuySignals[0].Tick.QuoteVolume;
					if (quoteVolumeUsd <= 0)
						tbVolumeAtBuySignal.Text = "(volume not available!)";
					else
						tbVolumeAtBuySignal.Text = $"(volume at buy signal: ${GetAmountStr(quoteVolumeUsd)})";

					if (quoteVolumeUsd < 1000)
					{
						tbVolumeAtBuySignal.Foreground = Brushes.DarkRed;
						tbVolumeAtBuySignal.FontWeight = FontWeights.Bold;
					}
					else
					{
						tbVolumeAtBuySignal.Foreground = Brushes.SteelBlue;
						tbVolumeAtBuySignal.FontWeight = FontWeights.Regular;
					}
				}
				else
				{
					tbVolumeAtBuySignal.Text = "";
					//tbVolumeAtBuySignal.Text = $"(volume: {tradeHistory.AverageVolume})";
				}
			}
			else
			{
				tbSymbolTitle.Text = "";
			}

			ClearDataDensityPoints();
			DrawGraph();
		}

		void UpdateStatusText()
		{
			if (!Selection.Exists)
			{
				tbStatus.Text = "";
				return;
			}

			chartTranslator.TradeHistory.GetFirstLastLowHigh(out DataPoint first, out DataPoint last, out DataPoint low, out DataPoint high, Selection.Start, Selection.End);

			string selectionStr = $"{GetSpanStr(Selection.Span)} selected";
			if (low == null || high == null)
				tbStatus.Text = selectionStr;
			else
			{
				decimal priceSpan = high.Tick.LastTradePrice - low.Tick.LastTradePrice;
				string priceSpanUsd = GetSmartUSD(priceSpan);
				string climbFallStr;
				decimal delta = last.Tick.LastTradePrice - first.Tick.LastTradePrice;
				if (delta > 0)
					climbFallStr = $"Climbs {GetSmartUSD(delta)} ({GetAmountStr(100 * delta / averagePrice)}%)";
				else if (delta < 0)
					climbFallStr = $"Falls {GetSmartUSD(-delta)} ({GetAmountStr(100 * -delta / averagePrice)}%)";
				else
					climbFallStr = "No change";

				tbStatus.Text = $"{selectionStr}, Low to High: {priceSpanUsd}, {climbFallStr}";
			}
		}

		private void ZoomMenuItem_Click(object sender, RoutedEventArgs e)
		{
			FrmZoom frmZoom = new FrmZoom();
			
			DateTime end;
			DateTime start;
			
			if (Selection.Exists)
			{
				end = Selection.End;
				start = Selection.Start;
			}
			else
			{
				TimeSpan spanAcross = chartTranslator.TradeHistory.SpanAcross;
				TimeSpan zoomHalf = TimeSpan.FromSeconds(spanAcross.TotalSeconds / 8.0);
				start = Selection.Cursor - zoomHalf;
				end = Selection.Cursor + zoomHalf;
			}

			DataPointsSnapshot snapshot = chartTranslator.TradeHistory.GetTruncatedSnapshot(start, end);
			frmZoom.SetTradeHistory(snapshot);
			frmZoom.ShowDialog();
		}
	}
}

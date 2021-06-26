using Bittrex.Net.Objects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StockAnalysis
{
	public class ChartTranslator
	{
		internal object stockDataPointsLock = new object();
		List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();

		public ChartTranslator(double chartWidthPixels, double chartHeightPixels)
		{
			this.chartWidthPixels = chartWidthPixels;
			this.chartHeightPixels = chartHeightPixels;
		}

		decimal high = 37000;
		decimal low = 35000;
		public readonly double chartHeightPixels;
		public readonly double chartWidthPixels;
		DateTime start;
		DateTime end;

		public List<StockDataPoint> StockDataPoints { get => stockDataPoints; }

		void CalculateBounds()
		{
			high = 0;
			low = decimal.MaxValue;
			lock (stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in stockDataPoints)
				{
					if (stockDataPoint.Tick.LastTradeRate < low)
						low = stockDataPoint.Tick.LastTradeRate;
					if (stockDataPoint.Tick.LastTradeRate > high)
						high = stockDataPoint.Tick.LastTradeRate;
				}

			if (high == low)  // Only one data point?
			{
				// Offset both for the charting:
				high += 1;
				low -= 1;
				if (low < 0)
					low = 0;
			}

			lock (stockDataPointsLock)
			{
				if (stockDataPoints.Count == 0)
				{
					start = DateTime.Now;
					end = DateTime.Now;
					return;
				}

				start = stockDataPoints[0].Time;
				end = stockDataPoints[stockDataPoints.Count - 1].Time;
			}
		}

		public double GetStockPositionX(DateTime time)
		{
			TimeSpan distanceToPoint = time - start;
			TimeSpan totalSpanAcross = end - start;
			double totalSecondsAcross = totalSpanAcross.TotalSeconds;
			double secondsToPoint = distanceToPoint.TotalSeconds;
			double percentAcross = secondsToPoint / totalSecondsAcross;

			return percentAcross * chartWidthPixels;
		}

		public DateTime GetTime(double x)
		{
			TimeSpan totalSpanAcross = end - start;
			double totalSecondsAcross = totalSpanAcross.TotalSeconds;
			double percentAcross = x / chartWidthPixels;
			double secondsToPoint = percentAcross * totalSecondsAcross;

			return start + TimeSpan.FromSeconds(secondsToPoint);
		}

		public DateTime GetTimeFromX(double xPos)
		{
			double percentAcross = xPos / chartWidthPixels;
			TimeSpan totalSpanAcross = end - start;
			double timeAcrossSeconds = percentAcross * totalSpanAcross.TotalSeconds;
			return start + TimeSpan.FromSeconds(timeAcrossSeconds);
		}

		public double GetStockPositionY(decimal lastTradeRate)
		{
			// Make sure the text is changed on the UI thread!
			decimal amountAboveBottom = lastTradeRate - low;
			decimal chartHeightDollars = high - low;

			// ![](99A33C7E4008781D520BA988900A3B50.png;;;0.01945,0.01945)

			decimal percentOfChartHeightFromBottom = amountAboveBottom / chartHeightDollars;
			double distanceFromBottomPixels = (double)percentOfChartHeightFromBottom * chartHeightPixels;
			double distanceFromTopPixels = chartHeightPixels - distanceFromBottomPixels;
			return distanceFromTopPixels;
		}

		public StockDataPoint AddStockPosition(BittrexTick data)
		{
			StockDataPoint stockDataPoint = new StockDataPoint(data);
			if (stockDataPoints.Count >= 2)
			{
				int lastIndex = stockDataPoints.Count - 1;
				StockDataPoint lastPoint = stockDataPoints[lastIndex];
				StockDataPoint secondToLastPoint = stockDataPoints[lastIndex - 1];
				if (lastPoint.Tick.LastTradeRate == secondToLastPoint.Tick.LastTradeRate)
				{
					if (lastPoint.Tick.LastTradeRate == data.LastTradeRate)
					{
						// Last two points, plus this one are the same. We can remove the middle point.
						lock (stockDataPointsLock)
						{
							// Since we are removing data points for efficiency, we have to weigh down the new point we are adding so our SMA still works. 
							stockDataPoint.Weight += lastPoint.Weight;
							stockDataPoints.RemoveAt(lastIndex);
						}
					}
				}
			}
			stockDataPoints.Add(stockDataPoint);
			CalculateBounds();
			return stockDataPoint;
		}

		public void AddMovingAverage(double spanDurationSeconds, Canvas canvas, Brush lineColor)
		{
			canvas.Children.Clear();
			TimeSpan timeSpan = TimeSpan.FromSeconds(spanDurationSeconds);
			TimeSpan halfTimeSpan = TimeSpan.FromSeconds(spanDurationSeconds / 2.0);
			DateTime spanStartTime = start;
			DateTime endTime = spanStartTime + timeSpan;

			List<StockDataPoint> pointsInSpan = new List<StockDataPoint>();
			double lastAverageX = 0;
			double lastAverageY = double.MinValue;

			lock (stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in StockDataPoints)
				{
					if (stockDataPoint.Time > endTime && pointsInSpan.Count > 0)
					{
						// We are outside of the span we are interested in.
						// That means we need to calculate the moving average for the points we have collected.
						decimal totalPrice = 0;
						int totalWeight = 0;
						foreach (StockDataPoint dataPoint in pointsInSpan)
						{
							totalWeight += dataPoint.Weight;
							totalPrice += dataPoint.Tick.LastTradeRate;
						}

						decimal averagePrice = totalPrice / totalWeight;
						DateTime middleTime = spanStartTime + halfTimeSpan;
						double averageX = GetStockPositionX(middleTime);
						double averageY = GetStockPositionY(averagePrice);

						if (lastAverageY != double.MinValue)
						{
							Line line = new Line();
							line.X1 = lastAverageX;
							line.Y1 = lastAverageY;
							line.X2 = averageX;
							line.Y2 = averageY;
							line.Stroke = lineColor;
							line.StrokeThickness = 2;
							canvas.Children.Add(line);
						}

						lastAverageX = averageX;
						lastAverageY = averageY;

						pointsInSpan.Clear();
						spanStartTime = endTime;
						endTime = spanStartTime + timeSpan;
					}
					pointsInSpan.Add(stockDataPoint);
				}
		}

		public StockDataPoint GetNearestPointInTime(DateTime time)
		{
			double closestSpanSoFar = double.MaxValue;
			StockDataPoint closestDataPoint = null;
			lock (stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in StockDataPoints)
				{
					double distanceInTime = Math.Abs((stockDataPoint.Time - time).TotalSeconds);
					if (distanceInTime < closestSpanSoFar)
					{
						closestSpanSoFar = distanceInTime;
						closestDataPoint = stockDataPoint;
					}
				}
			return closestDataPoint;
		}
	}
}

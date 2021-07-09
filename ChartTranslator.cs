using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
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
			if (totalSecondsAcross == 0)
				return 0;
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
			if (xPos < 0)
				return start;

			double percentAcross = xPos / chartWidthPixels;

			if (percentAcross > 1)
				return end;

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

		public StockDataPoint AddStockPosition(CustomTick data)
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
			TimeSpan timeSpan = TimeSpan.FromSeconds(spanDurationSeconds);
			TimeSpan halfTimeSpan = TimeSpan.FromSeconds(spanDurationSeconds / 2.0);
			DateTime spanStartTime = start;
			DateTime spanEndTime = spanStartTime + timeSpan;

			List<StockDataPoint> pointsInSpan = new List<StockDataPoint>();
			double lastAverageX = 0;
			double lastAverageY = double.MinValue;

			lock (stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in StockDataPoints)
				{
					if (stockDataPoint.Time > spanEndTime)
					{
						DateTime middleTime = spanStartTime + halfTimeSpan;
						double averageX = GetStockPositionX(middleTime);

						if (pointsInSpan.Count > 0)
						{
							// We are outside of the span we are interested in.
							// That means we need to calculate the moving average for the points we have collected.
							decimal averagePrice = GetAveragePrice(pointsInSpan);
							double averageY = GetStockPositionY(averagePrice);

							DrawLine(canvas, lineColor, lastAverageX, lastAverageY, averageX, averageY);

							lastAverageY = averageY;

							StockDataPoint lastPoint = pointsInSpan[pointsInSpan.Count - 1];
							pointsInSpan.Clear();
							if (stockDataPoint.Time > spanEndTime)
								pointsInSpan.Add(lastPoint);
						}
						else
						{
							DrawLine(canvas, lineColor, lastAverageX, lastAverageY, averageX, lastAverageY);
						}

						lastAverageX = averageX;

						if (pointsInSpan.Count > 0)
						{
							while (spanStartTime < pointsInSpan[0].Time - timeSpan)
							{
								spanStartTime += timeSpan;
							}
						}
						else
							spanStartTime = spanEndTime;

						spanEndTime = spanStartTime + timeSpan;
					}
					pointsInSpan.Add(stockDataPoint);
				}
		}

		private static decimal GetAveragePrice(List<StockDataPoint> pointsInSpan)
		{
			decimal totalPrice = 0;
			int totalWeight = 0;
			foreach (StockDataPoint dataPoint in pointsInSpan)
			{
				totalWeight += dataPoint.Weight;
				totalPrice += dataPoint.Tick.LastTradeRate * dataPoint.Weight;
			}

			if (totalWeight == 0)
				return decimal.MinValue;

			decimal averagePrice = totalPrice / totalWeight;
			return averagePrice;
		}

		private static void DrawLine(Canvas canvas, Brush lineColor, double lastAverageX, double lastAverageY, double averageX, double averageY)
		{
			if (lastAverageY != double.MinValue)
			{
				Line line = new Line();
				line.X1 = lastAverageX;
				line.Y1 = lastAverageY;
				line.X2 = averageX;
				line.Y2 = averageY;
				line.Stroke = lineColor;
				line.StrokeThickness = 4;
				canvas.Children.Add(line);
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

		List<StockDataPoint> GetDataPointsInRange(DateTime time, int timeSpanSeconds)
		{
			DateTime startRange = time - TimeSpan.FromSeconds(timeSpanSeconds / 2.0);
			DateTime endRange = time + TimeSpan.FromSeconds(timeSpanSeconds / 2.0);

			List<StockDataPoint> pointsInRange = new List<StockDataPoint>();
			lock (stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in StockDataPoints)
				{
					if (stockDataPoint.Time > endRange)
						return pointsInRange;

					if (stockDataPoint.Time > startRange)
					{
						// We can work with this data!
						pointsInRange.Add(stockDataPoint);
					}
				}
			return pointsInRange;
		}

		public List<Point> GetMovingAverages(int timeSpanSeconds)
		{
			DateTime currentTime = DateTime.MinValue;
			currentTime = start;
			TimeSpan timeAcross = end - start;
			double totalSecondsAcross = timeAcross.TotalSeconds;
			const int numberOfDataPoints = 200;
			List<Point> points = new List<Point>();
			double secondsPerDataPoint = totalSecondsAcross / numberOfDataPoints;
			for (int i = 0; i < numberOfDataPoints; i++)
			{
				DateTime timeAtDataPoint = start + TimeSpan.FromSeconds(i * secondsPerDataPoint);
				List<StockDataPoint> dataPointsInRange = GetDataPointsInRange(timeAtDataPoint, timeSpanSeconds);
				decimal averagePrice = GetAveragePrice(dataPointsInRange);
				if (averagePrice == decimal.MinValue)
					continue;
				double stockPositionX = GetStockPositionX(timeAtDataPoint);
				double stockPositionY = GetStockPositionY(averagePrice);
				Point point = new Point(stockPositionX, stockPositionY);
				points.Add(point);
			}
			return points;
		}
	}
}

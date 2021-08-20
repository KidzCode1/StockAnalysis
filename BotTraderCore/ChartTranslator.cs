using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BotTraderCore
{
	public class ChartTranslator
	{
		internal object stockDataPointsLock = new object();
		List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();

		public ChartTranslator()
		{
		}

		decimal high = 37000;
		decimal low = 35000;
		DateTime start;
		DateTime end;

		public List<StockDataPoint> StockDataPoints { get => stockDataPoints; }
		public DateTime Start => start;
		public DateTime End => end;

		void CalculateBounds()
		{
			high = 0;
			low = decimal.MaxValue;
			lock (stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in stockDataPoints)
				{
					if (stockDataPoint.Tick.LastTradePrice < low)
						low = stockDataPoint.Tick.LastTradePrice;
					if (stockDataPoint.Tick.LastTradePrice > high)
						high = stockDataPoint.Tick.LastTradePrice;
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

		public double GetStockPositionX(DateTime time, double chartWidthPixels)
		{
			if (time == DateTime.MinValue)
				return 0d;

			if (time == DateTime.MaxValue)
				return chartWidthPixels;

			TimeSpan distanceToPoint = time - start;
			TimeSpan totalSpanAcross = end - start;
			double totalSecondsAcross = totalSpanAcross.TotalSeconds;
			if (totalSecondsAcross == 0)
				return 0;
			double secondsToPoint = distanceToPoint.TotalSeconds;
			double percentAcross = secondsToPoint / totalSecondsAcross;

			return percentAcross * chartWidthPixels;
		}

		public DateTime GetTime(double x, double chartWidthPixels)
		{
			TimeSpan totalSpanAcross = end - start;
			double totalSecondsAcross = totalSpanAcross.TotalSeconds;
			double percentAcross = x / chartWidthPixels;
			double secondsToPoint = percentAcross * totalSecondsAcross;

			return start + TimeSpan.FromSeconds(secondsToPoint);
		}

		public DateTime GetTimeFromX(double xPos, double chartWidthPixels)
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

		public double GetStockPositionY(decimal price, double chartHeightPixels)
		{
			if (price == decimal.MinValue)
				return chartHeightPixels;

			if (price == decimal.MaxValue)
				return 0;
			// Make sure the text is changed on the UI thread!
			decimal amountAboveBottom = price - low;
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
				//RemoveMatchingDataPoints(data, stockDataPoint);
			}
			stockDataPoints.Add(stockDataPoint);
			CalculateBounds();
			return stockDataPoint;
		}

		private void RemoveMatchingDataPoints(CustomTick data, StockDataPoint stockDataPoint)
		{
			int lastIndex = stockDataPoints.Count - 1;
			StockDataPoint lastPoint = stockDataPoints[lastIndex];
			StockDataPoint secondToLastPoint = stockDataPoints[lastIndex - 1];
			if (lastPoint.Tick.LastTradePrice == secondToLastPoint.Tick.LastTradePrice)
			{
				if (lastPoint.Tick.LastTradePrice == data.LastTradePrice)
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

		//public void AddMovingAverage(double spanDurationSeconds, Canvas canvas, Brush lineColor)
		//{
		//	TimeSpan timeSpan = TimeSpan.FromSeconds(spanDurationSeconds);
		//	TimeSpan halfTimeSpan = TimeSpan.FromSeconds(spanDurationSeconds / 2.0);
		//	DateTime spanStartTime = start;
		//	DateTime spanEndTime = spanStartTime + timeSpan;

		//	List<StockDataPoint> pointsInSpan = new List<StockDataPoint>();
		//	double lastAverageX = 0;
		//	double lastAverageY = double.MinValue;

		//	lock (stockDataPointsLock)
		//		foreach (StockDataPoint stockDataPoint in StockDataPoints)
		//		{
		//			if (stockDataPoint.Time > spanEndTime)
		//			{
		//				DateTime middleTime = spanStartTime + halfTimeSpan;
		//				double averageX = GetStockPositionX(middleTime);

		//				if (pointsInSpan.Count > 0)
		//				{
		//					// We are outside of the span we are interested in.
		//					// That means we need to calculate the moving average for the points we have collected.
		//					decimal averagePrice = GetAveragePrice(pointsInSpan);
		//					double averageY = GetStockPositionY(averagePrice);

		//					DrawLine(canvas, lineColor, lastAverageX, lastAverageY, averageX, averageY);

		//					lastAverageY = averageY;

		//					StockDataPoint lastPoint = pointsInSpan[pointsInSpan.Count - 1];
		//					pointsInSpan.Clear();
		//					if (stockDataPoint.Time > spanEndTime)
		//						pointsInSpan.Add(lastPoint);
		//				}
		//				else
		//				{
		//					DrawLine(canvas, lineColor, lastAverageX, lastAverageY, averageX, lastAverageY);
		//				}

		//				lastAverageX = averageX;

		//				if (pointsInSpan.Count > 0)
		//				{
		//					while (spanStartTime < pointsInSpan[0].Time - timeSpan)
		//					{
		//						spanStartTime += timeSpan;
		//					}
		//				}
		//				else
		//					spanStartTime = spanEndTime;

		//				spanEndTime = spanStartTime + timeSpan;
		//			}
		//			pointsInSpan.Add(stockDataPoint);
		//		}
		//}

		private static decimal GetAveragePrice(List<StockDataPoint> pointsInSpan)
		{
			decimal totalPrice = 0;
			int totalWeight = 0;
			foreach (StockDataPoint dataPoint in pointsInSpan)
			{
				totalWeight += dataPoint.Weight;
				totalPrice += dataPoint.Tick.LastTradePrice * dataPoint.Weight;
			}

			if (totalWeight == 0)
				return decimal.MinValue;

			decimal averagePrice = totalPrice / totalWeight;
			return averagePrice;
		}

		//private static void DrawLine(Canvas canvas, Brush lineColor, double lastAverageX, double lastAverageY, double averageX, double averageY)
		//{
		//	if (lastAverageY != double.MinValue)
		//	{
		//		Line line = new Line();
		//		line.X1 = lastAverageX;
		//		line.Y1 = lastAverageY;
		//		line.X2 = averageX;
		//		line.Y2 = averageY;
		//		line.Stroke = lineColor;
		//		line.StrokeThickness = 4;
		//		canvas.Children.Add(line);
		//	}
		//}

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

		public List<PointXY> GetMovingAverages(int timeSpanSeconds, double chartWidthPixels, double chartHeightPixels)
		{
			DateTime currentTime = DateTime.MinValue;
			currentTime = start;
			TimeSpan timeAcross = end - start;
			double totalSecondsAcross = timeAcross.TotalSeconds;
			const int numberOfDataPoints = 200;

			List<PointXY> points = new List<PointXY>();

			if (start == DateTime.MinValue)
				return points;

			double secondsPerDataPoint = totalSecondsAcross / numberOfDataPoints;
			for (int i = 0; i < numberOfDataPoints; i++)
			{
				DateTime timeAtDataPoint = start + TimeSpan.FromSeconds(i * secondsPerDataPoint);
				TickRange tickRange = GetPointsAroundTime(timeAtDataPoint, timeSpanSeconds);
				decimal averagePrice = GetAveragePrice(tickRange.DataPoints);
				if (averagePrice == decimal.MinValue)
					continue;
				double stockPositionX = GetStockPositionX(timeAtDataPoint, chartWidthPixels);
				double stockPositionY = GetStockPositionY(averagePrice, chartHeightPixels);
				PointXY point = new PointXY(stockPositionX, stockPositionY);
				points.Add(point);
			}
			return points;
		}

		TickRange GetPointsAroundTime(DateTime timeCenterPoint, int timeSpanSeconds)
		{
			double halfTimeSpan = timeSpanSeconds / 2.0;
			TimeSpan halfTimeSpanSeconds = TimeSpan.FromSeconds(halfTimeSpan);

			DateTime startRange = timeCenterPoint - halfTimeSpanSeconds;
			DateTime endRange = timeCenterPoint + halfTimeSpanSeconds;
			return GetPointsInRange(startRange, endRange);
		}

		public TickRange GetPointsInRange(DateTime start, DateTime end)
		{
			TickRange tickRange = new TickRange()
			{
				Start = start,
				End = end,
				DataPoints = new List<StockDataPoint>(),
			};

			StockDataPoint lastDataPoint = null;
			StockDataPoint lowestSoFar = null;
			StockDataPoint highestSoFar = null;

			lock (stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in StockDataPoints)
					if (stockDataPoint.Time > end)
						return tickRange;
					else if (stockDataPoint.Time >= start)   // We can work with this data!
					{
						if (lastDataPoint != null)  // We have just entered the start of the desired range.
						{
							tickRange.ValueBeforeRangeStarts = lastDataPoint;
						}
						tickRange.DataPoints.Add(stockDataPoint);
						if (lowestSoFar == null)
							lowestSoFar = stockDataPoint;
						else if (stockDataPoint.Tick.LastTradePrice < lowestSoFar.Tick.LastTradePrice)
							lowestSoFar = stockDataPoint;
						if (highestSoFar == null)
							highestSoFar = stockDataPoint;
						else if (stockDataPoint.Tick.LastTradePrice > highestSoFar.Tick.LastTradePrice)
							highestSoFar = stockDataPoint;
					}
					else
					{
						// stockDataPoint.Time comes before start
						lastDataPoint = stockDataPoint;
					}

			tickRange.Low = lowestSoFar;
			tickRange.High = highestSoFar;
			return tickRange;
		}

		public List<StockDataPoint> GetAllStockDataPoints()
		{
			List<StockDataPoint> result = new List<StockDataPoint>();
			lock (stockDataPointsLock)
				result.AddRange(StockDataPoints);
			return result;
		}

		public void SetStockDataPoints(List<StockDataPoint> points)
		{
			if (points.Count == 0)
				return;

			lock (stockDataPointsLock)
				StockDataPoints.AddRange(points);
			start = points[0].Time;
			end = points[points.Count - 1].Time;
			CalculateBounds();
		}
		public void Clear()
		{
			lock (stockDataPointsLock)
				StockDataPoints.Clear();
		}

		public void SetTickRange(TickRange tickRange)
		{
			if (tickRange.DataPoints.Count == 0)
				return;

			lock (stockDataPointsLock)
				StockDataPoints.AddRange(tickRange.DataPoints);

			CalculateBounds();
			start = tickRange.Start;
			end = tickRange.End;
		}
	}
}

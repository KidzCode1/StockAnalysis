﻿using System;
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
		bool changedSinceLastDataDensityQuery;

		public List<StockDataPoint> StockDataPoints { get => stockDataPoints; }
		public DateTime Start => start;
		public DateTime End => end;
		public decimal High => high;
		public decimal Low => low;

		public bool ChangedSinceLastDataDensityQuery { get => changedSinceLastDataDensityQuery; }

		/// <summary>
		/// Adjusts the high, low, and end, based on the new data point.
		/// </summary>
		/// <param name="latestDataPoint"></param>
		void AdjustBounds(StockDataPoint latestDataPoint)
		{
			SetHighLow(latestDataPoint.Tick);
			CheckHighLow();
			end = latestDataPoint.Time;
		}

		/// <summary>
		/// Goes through every StockDataPoint and finds the highest/lowest values, 
		/// and also sets the start and end of the graph based on the times of the first 
		/// and last data points.
		/// </summary>
		public void CalculateBounds()
		{
			high = 0;
			low = decimal.MaxValue;
			lock (stockDataPointsLock)
				foreach (StockDataPoint stockDataPoint in StockDataPoints)
					SetHighLow(stockDataPoint.Tick);
			CheckHighLow();

			lock (stockDataPointsLock)
			{
				if (StockDataPoints.Count == 0)
				{
					start = DateTime.Now;
					end = DateTime.Now;
					return;
				}

				start = StockDataPoints[0].Time;
				end = StockDataPoints[StockDataPoints.Count - 1].Time;
			}
		}

		/// <summary>
		/// Makes sure the chart has enough vertical room to show the data if high == low.
		/// </summary>
		private void CheckHighLow()
		{
			if (high == low)  // No change in price?
			{
				// Offset both for the charting:
				high += 1;
				low -= 1;
				if (low < 0)
					low = 0;
			}
		}

		/// <summary>
		/// Sets the high and/or low value based on the CustomTick's LastTradePrice.
		/// </summary>
		private void SetHighLow(CustomTick tick)
		{
			if (tick.LastTradePrice < low)
				low = tick.LastTradePrice;
			if (tick.LastTradePrice > high)
				high = tick.LastTradePrice;
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
			if (x < 0)
				return start;
			TimeSpan totalSpanAcross = end - start;
			double totalSecondsAcross = totalSpanAcross.TotalSeconds;
			double percentAcross = x / chartWidthPixels;
			if (percentAcross > 1)
				return end;
			double secondsToPoint = percentAcross * totalSecondsAcross;

			return start + TimeSpan.FromSeconds(secondsToPoint);
		}

		public decimal GetPrice(double yPos, double chartHeightPixels)
		{
			if (yPos < 0)
				return high;

			double percentUp = yPos / chartHeightPixels;

			if (percentUp > 1)
				return low;

			decimal totalPriceSpan = high - low;
			decimal totalPriceDelta = (decimal)percentUp * totalPriceSpan;
			return high - totalPriceDelta;
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
			lock (stockDataPointsLock)
				StockDataPoints.Add(stockDataPoint);

			if (StockDataPoints.Count > 3000)
			{
				CustomTick removed = StockDataPoints[0].Tick;
				lock (stockDataPointsLock)
					StockDataPoints.RemoveAt(0); // Remove oldest data point

				if (IsHighOrLow(removed))  // This data point may have been defining our high or low
					CalculateBounds();  // We need to recalculate everything.
				else  // Only the start and end have changed...
				{
					start = StockDataPoints[0].Time;
					AdjustBounds(stockDataPoint);
				}
			}
			else if (StockDataPoints.Count < 10)
				CalculateBounds();
			else
				AdjustBounds(stockDataPoint);
			changedSinceLastDataDensityQuery = true;
			return stockDataPoint;
		}

		public StockDataPoint TestAddStockDataPoint(decimal price, double offsetSeconds)
		{
			StockDataPoint stockDataPoint = new StockDataPoint(new CustomTick() { LastTradePrice = price, HighestBidPrice = price, LowestAskPrice = price, Symbol = "BTC" }) { Time = DateTime.MinValue + TimeSpan.FromSeconds(offsetSeconds) };
			lock (stockDataPointsLock)
				StockDataPoints.Add(stockDataPoint);

			return stockDataPoint;
		}

		private bool IsHighOrLow(CustomTick first)
		{
			return first.LastTradePrice == High || first.LastTradePrice == Low;
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

		public StockDataPointsSnapshot GetStockDataPointsSnapshot()
		{
			StockDataPointsSnapshot result;
			
			lock (stockDataPointsLock)
				result = new StockDataPointsSnapshot(StockDataPoints, start, end, low, high);
			
			return result;
		}

		public static int GetIndexOfPointOnOrBefore(StockDataPointsSnapshot stockDataPointsSnapshot, DateTime time, int leftStartIndex = 0)
		{
			StockDataPoint lastPreviousDataPoint = null;
			int lastPreviousIndex = -1;

			int left = leftStartIndex;
			int right = stockDataPointsSnapshot.StockDataPoints.Count - 1;
			while (left <= right)
			{
				int middle = left + (right - left) / 2;

				StockDataPoint middleDataPoint = stockDataPointsSnapshot.StockDataPoints[middle];
				// Check if x is present at mid 
				if (middleDataPoint.Time == time)
					return middle;

				// If x greater, ignore left half 
				if (middleDataPoint.Time < time)
				{
					if (lastPreviousDataPoint == null || lastPreviousDataPoint.Time < middleDataPoint.Time)
					{
						lastPreviousDataPoint = middleDataPoint;
						lastPreviousIndex = middle;
					}

					left = middle + 1;
				}
				else  // If x is smaller, ignore right half.
					right = middle - 1;
			}

			// if we reach here, then element was not present. Return the index of the closest data point just before this one.
			return lastPreviousIndex;
		}

		/// <summary>
		/// Returns a single point that represents the specified range (an average of all data points in that space, or 
		/// the previous point's value if there are no points in the range).
		/// </summary>
		/// <param name="stockDataPointsSnapshot"></param>
		/// <param name="startSegment"></param>
		/// <param name="endSegment"></param>
		public static StockDataPoint GetPointInRange(StockDataPointsSnapshot stockDataPointsSnapshot, DateTime startSegment, DateTime endSegment, ref int leftStartIndex)
		{
			int startIndex = GetIndexOfPointOnOrBefore(stockDataPointsSnapshot, startSegment, leftStartIndex);

			if (startIndex == -1)
				return null;

			// TODO: We need to multiply these values by the actual length of this data point (until the next data point is found)
			// The length of the first value is the time of the second tick minus the startSegment.
			// The length of the last value is the endSegment minus the time of the last tick.
			// All other tick lengths are the time of that tick minus the time of the last tick.

			decimal totalLastTradePrice = 0;
			decimal totalHighestBidPrice = 0;
			decimal totalLowestAskPrice = 0;
			decimal totalDurationSeconds = 0;
			int index = startIndex;
			StockDataPoint stockDataPoint;
			while (index < stockDataPointsSnapshot.StockDataPoints.Count)
			{
				stockDataPoint = stockDataPointsSnapshot.StockDataPoints[index];
				if (stockDataPoint.Time > endSegment)
					break;

				index++;

				DateTime startTime;
				if (index == startIndex) // This is the first point.
					startTime = startSegment;
				else
					startTime = stockDataPoint.Time;

				DateTime endTime;
				if (index < stockDataPointsSnapshot.StockDataPoints.Count)
				{
					StockDataPoint nextStockDataPoint = stockDataPointsSnapshot.StockDataPoints[index];
					endTime = nextStockDataPoint.Time;
					if (endTime > endSegment)
						endTime = endSegment;
					else
						leftStartIndex++;
				}
				else  // This is the last point
					endTime = endSegment;

				decimal durationSeconds = (decimal)(endTime - startTime).TotalSeconds;

				totalDurationSeconds += durationSeconds;

				CustomTick tick = stockDataPoint.Tick;
				totalLastTradePrice += durationSeconds * tick.LastTradePrice;
				totalHighestBidPrice += durationSeconds * tick.HighestBidPrice;
				totalLowestAskPrice += durationSeconds * tick.LowestAskPrice;
			}

			decimal lastTradePrice = totalLastTradePrice / totalDurationSeconds;
			decimal highestBidPrice = totalHighestBidPrice / totalDurationSeconds;
			decimal lowestAskPrice = totalLowestAskPrice / totalDurationSeconds;
			return new StockDataPoint(new CustomTick() { LastTradePrice = lastTradePrice, HighestBidPrice = highestBidPrice, LowestAskPrice = lowestAskPrice }) { Time = startSegment };
		}


		/// <summary>
		/// Gets a list of StockDataPoints based on the specified segmentCount. So if I have 11 data points and I 
		/// specify five segments, this function will return six points. The first five points will contain averages
		/// of data at indexes 0 & 1, 2 & 3, 4 & 5, 6 & 7, and 8 & 9. The last point will always contain the last point
		/// in this ChartTranslator (no averaging or repeating).
		/// </summary>
		/// <param name="segmentCount">The number of segments to break the existing data into. Data points 
		/// will be repeated if more segments span a range than actual data points (in the ChartTranslator).
		/// Similarly, data points will be averaged if more than one appear in a single segment.</param>
		/// <returns>Returns the calculated StockDataPoints. The count will always be segmentCount plus one.</returns>
		public List<StockDataPoint> GetStockDataPointsAcrossSegments(int segmentCount)
		{
			if (segmentCount == 0)
				return null;
			List<StockDataPoint> results = new List<StockDataPoint>();
			changedSinceLastDataDensityQuery = false;
			StockDataPointsSnapshot stockDataPointSnapshot = GetStockDataPointsSnapshot();

			TimeSpan dataTimeSpan = TimeSpan.FromTicks((stockDataPointSnapshot.End - stockDataPointSnapshot.Start).Ticks / segmentCount);
			DateTime startSegment = stockDataPointSnapshot.Start;
			DateTime endSegment = stockDataPointSnapshot.Start + dataTimeSpan;
			int leftStartIndex = 0;
			do
			{
				StockDataPoint dp = GetPointInRange(stockDataPointSnapshot, startSegment, endSegment, ref leftStartIndex);
				if (dp == null)
					break;

				results.Add(dp);
				startSegment += dataTimeSpan;
				endSegment += dataTimeSpan;

				if (leftStartIndex < stockDataPointSnapshot.StockDataPoints.Count - 1)
				{
					StockDataPoint nextPoint = stockDataPointSnapshot.StockDataPoints[leftStartIndex];
					while (startSegment < nextPoint.Time)  // We are just going to keep adding the same point until the startSegment equals or is greater than the nextPoint.
					{
						results.Add(dp);
						startSegment += dataTimeSpan;
						endSegment += dataTimeSpan;
					}
				}
			} while (results.Count < segmentCount);

			results.Add(stockDataPointSnapshot.StockDataPoints[stockDataPointSnapshot.StockDataPoints.Count - 1]);

			return results;
		}

		public void SetStockDataPoints(List<StockDataPoint> points)
		{
			if (points.Count == 0)
				return;

			lock (stockDataPointsLock)
				StockDataPoints.AddRange(points);
			//start = points[0].Time;
			//end = points[points.Count - 1].Time;
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
			{
				StockDataPoints.Clear();
				StockDataPoints.AddRange(tickRange.DataPoints);
			}

			CalculateBounds();
			start = tickRange.Start;
			end = tickRange.End;
		}

		TickRange GetTickRange(/* DateTime startRange, DateTime endRange */)
		{
			TickRange tickRange = new TickRange();
			tickRange.Start = start;
			tickRange.End = end;
			tickRange.DataPoints = new List<StockDataPoint>();
			tickRange.DataPoints.AddRange(StockDataPoints);
			if (StockDataPoints.Count > 0)
				tickRange.ValueBeforeRangeStarts = StockDataPoints[0];
			tickRange.CalculateLowAndHigh();
			return tickRange;
		}

		public void SaveAll(string fullPathToFile)
		{
			string serializeObject = Newtonsoft.Json.JsonConvert.SerializeObject(GetTickRange(), Newtonsoft.Json.Formatting.Indented);
			System.IO.File.WriteAllText(fullPathToFile, serializeObject);
		}
	}

	//public class StockDataPointTimeComparer : IComparer<StockDataPoint>
	//{
	//	public int Compare(StockDataPoint x, StockDataPoint y)
	//	{
	//		if (x == null)
	//		{
	//			if (y == null)
	//			{
	//				// If x is null and y is null, they're
	//				// equal.
	//				return 0;
	//			}
	//			else
	//			{
	//				// If x is null and y is not null, y
	//				// is greater.
	//				return -1;
	//			}
	//		}
	//		else
	//		{
	//			// If x is not null...
	//			//
	//			if (y == null)
	//			// ...and y is null, x is greater.
	//			{
	//				return 1;
	//			}
	//			else
	//			{
	//				if (x.Time == y.Time)
	//					return 0;

	//				if (x.Time > y.Time)
	//					return 1;

	//				return -1;
	//			}
	//		}
	//	}
	//}
}

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BotTraderCore
{
	public class TradeHistory
	{
		internal readonly object dataLock = new object();
		ChangeSummary activeChangeSummary;  // access protected by dataLock
		readonly List<ChangeSummary> changeSummaries = new List<ChangeSummary>();  // access protected by dataLock
		readonly List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();  // access protected by dataLock

		bool changedSinceLastDataDensityQuery;
		bool changedSinceLastSnapshot = true;

		StockDataPointsSnapshot lastSnapShot;

		bool onFlatLine;

		DateTime start;
		DateTime end;
		decimal low = 35000;
		decimal high = 37000;


		public TradeHistory()
		{
		}

		public decimal AmountInView => High - Low;

		public bool ChangedSinceLastDataDensityQuery
		{
			get { return changedSinceLastDataDensityQuery; }
			set { changedSinceLastDataDensityQuery = value; }
		}

		public int Count
		{
			get
			{
				int count;
				lock (dataLock)
					count = StockDataPoints.Count;
				return count;
			}
		}

		public DateTime Start => start;
		public DateTime End => end;
		public decimal High => high;
		public decimal Low => low;

		public int MaxDataPointsToKeep { get; set; } = 500;

		public decimal PercentInView => AmountInView / High * 100;
		public TimeSpan SpanAcross => End - Start;

		public List<StockDataPoint> StockDataPoints => stockDataPoints;

		static List<ChangeSummary> GetChangeSummariesInRange(
			StockDataPointsSnapshot stockDataPointSnapshot,
			DateTime startSegment,
			DateTime endSegment,
			ref int changeSummaryIndex)
		{
			if (stockDataPointSnapshot.ChangeSummaries == null)
				return null;
			List<ChangeSummary> result = new List<ChangeSummary>();

			while (changeSummaryIndex < stockDataPointSnapshot.ChangeSummaries.Count)
			{
				ChangeSummary changeSummary = stockDataPointSnapshot.ChangeSummaries.ElementAt(changeSummaryIndex);
				if (changeSummary.Start.Time < endSegment && changeSummary.End.Time > startSegment)
				{
					result.Add(changeSummary);
					changeSummaryIndex++;
				}
				else
					break;
			}

			if (result.Count > 0)
			{
				changeSummaryIndex--;  // Go back one in case this last change summary straddles two segments.
				return result;
			}

			return null;
		}

		public static decimal GetAveragePrice(List<StockDataPoint> pointsInSpan)
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

		public static int GetIndexOfPointOnOrBefore(
			StockDataPointsSnapshot stockDataPointsSnapshot,
			DateTime time,
			int leftStartIndex = 0)
		{
			StockDataPoint lastPreviousDataPoint = null;
			int lastPreviousIndex = -1;

			int left = leftStartIndex;
			int right = stockDataPointsSnapshot.DataPoints.Count - 1;
			while (left <= right)
			{
				int middle = left + (right - left) / 2;

				StockDataPoint middleDataPoint = stockDataPointsSnapshot.DataPoints.ElementAt(middle);
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
		/// Returns a single point that represents the specified range (an average of all data points in that space, or  the
		/// previous point's value if there are no points in the range).
		/// </summary>
		/// <param name="stockDataPointsSnapshot"></param>
		/// <param name="startSegment"></param>
		/// <param name="endSegment"></param>
		public static StockDataPoint GetPointInRange(
			StockDataPointsSnapshot stockDataPointsSnapshot,
			DateTime startSegment,
			DateTime endSegment,
			ref int leftStartIndex)
		{
			int startIndex = GetIndexOfPointOnOrBefore(stockDataPointsSnapshot, startSegment, leftStartIndex);

			if (startIndex == -1)
				return null;

			// The length of the first value is the time of the second tick minus the startSegment.
			// The length of the last value is the endSegment minus the time of the last tick.
			// All other tick lengths are the time of that tick minus the time of the last tick.

			decimal totalLastTradePrice = 0;
			decimal totalHighestBidPrice = 0;
			decimal totalLowestAskPrice = 0;
			decimal totalDurationSeconds = 0;
			int index = startIndex;
			StockDataPoint stockDataPoint;
			while (index < stockDataPointsSnapshot.DataPoints.Count)
			{
				stockDataPoint = stockDataPointsSnapshot.DataPoints.ElementAt(index);
				if (stockDataPoint.Time > endSegment)
					break;

				index++;

				DateTime startTime;
				if (index == startIndex) // This is the first point.
					startTime = startSegment;
				else
					startTime = stockDataPoint.Time;

				DateTime endTime;
				if (index < stockDataPointsSnapshot.DataPoints.Count)
				{
					StockDataPoint nextStockDataPoint = stockDataPointsSnapshot.DataPoints.ElementAt(index);
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
				// For calculating average value...
				totalLastTradePrice += durationSeconds * tick.LastTradePrice;
				totalHighestBidPrice += durationSeconds * tick.HighestBidPrice;
				totalLowestAskPrice += durationSeconds * tick.LowestAskPrice;
			}

			if (totalDurationSeconds == 0)
				return null;

			decimal lastTradePrice = totalLastTradePrice / totalDurationSeconds;
			decimal highestBidPrice = totalHighestBidPrice / totalDurationSeconds;
			decimal lowestAskPrice = totalLowestAskPrice / totalDurationSeconds;
			return new StockDataPoint(
				new CustomTick()
				{
					LastTradePrice = lastTradePrice,
					HighestBidPrice = highestBidPrice,
					LowestAskPrice = lowestAskPrice
				})
			{
				Time = startSegment
			};
		}

		void AddChangeSummaries(
			List<StockDataPoint> results,
			StockDataPointsSnapshot stockDataPointSnapshot,
			double segmentQuarterSpanSeconds)
		{
			if (stockDataPointSnapshot.ChangeSummaries.Count == 0)
				return;

			StockDataPoint lastStockPoint = null;

			foreach (ChangeSummary changeSummary in stockDataPointSnapshot.ChangeSummaries)
			{
				results.Add(changeSummary.Start);
				results.Add(changeSummary.Low);
				results.Add(changeSummary.High);
				results.Add(changeSummary.End);
			}

			results.Sort((x, y) => Math.Sign((x.Time - y.Time).TotalSeconds));

			decimal average = results.Select(m => m.Tick.LastTradePrice).Average();

			DateTime lastTime = DateTime.MinValue;
			List<StockDataPoint> stockDataPointsToRemove = new List<StockDataPoint>();
			StockDataPoint priorToLastDataPoint = null;
			foreach (StockDataPoint stockPoint in results)
			{
				if (lastStockPoint == null)
				{
					lastTime = stockPoint.Time;
					priorToLastDataPoint = lastStockPoint;
					lastStockPoint = stockPoint;
					continue;
				}

				//lastTime == stockPoint.Time
				double distanceInSeconds;
				if (lastTime < stockPoint.Time)
					distanceInSeconds = (stockPoint.Time - lastTime).TotalSeconds;
				else if (lastTime == stockPoint.Time)
					distanceInSeconds = 0;
				else
				{
					// Should never get here. List is sorted by time.
					distanceInSeconds = 0;
					Debugger.Break();
				}

				if (distanceInSeconds < segmentQuarterSpanSeconds)
				{
					decimal lastDistanceFromAverage = Math.Abs(average - lastStockPoint.Tick.LastTradePrice);
					decimal thisDistanceFromAverage = Math.Abs(average - stockPoint.Tick.LastTradePrice);
					if (lastDistanceFromAverage > thisDistanceFromAverage)
						stockDataPointsToRemove.Add(stockPoint);
					else
						stockDataPointsToRemove.Add(lastStockPoint);
				}
				else if (priorToLastDataPoint != null) // Check for multiple data points in a row at the same price that we can remove...
				{
					if (AreClose(priorToLastDataPoint.Tick.LastTradePrice, lastStockPoint.Tick.LastTradePrice) &&
						AreClose(lastStockPoint.Tick.LastTradePrice, stockPoint.Tick.LastTradePrice))
					{
						// Three matching stock prices in a row. Remove the middle point.
						stockDataPointsToRemove.Add(lastStockPoint);
					}
				}
				lastTime = stockPoint.Time;
				priorToLastDataPoint = lastStockPoint;
				lastStockPoint = stockPoint;
			}

			foreach (StockDataPoint stockDataPointToRemove in stockDataPointsToRemove)
				results.Remove(stockDataPointToRemove);
		}

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

		bool AreClose(decimal price1, decimal price2)
		{
			decimal averagePrice = (price1 + price2) / 2;
			return Math.Abs(price1 - price2) < averagePrice * 0.0001m;
		}

		/// <summary>
		/// Makes sure the chart has enough vertical room to show the data if high == low.
		/// </summary>
		void CheckHighLow()
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

		bool IsHighOrLow(CustomTick first)
		{
			return first.LastTradePrice == High || first.LastTradePrice == Low;
		}

		/// <summary>
		/// Call before adding a new point to see if last two trailing ticks match this one (comparing LastTradePrice only).
		/// This method works directly on stockDataPoints so make sure this call is protected by a lock on stockDataPointsLock
		/// before calling.
		/// </summary>
		/// <param name="newStockDataPoint">The new StockDataPoint we're about to add.</param>
		void RemoveMatchingDataPoints(StockDataPoint newStockDataPoint)
		{
			if (StockDataPoints.Count < 2)
				return;
			int lastIndex = StockDataPoints.Count - 1;
			StockDataPoint lastPointToRemove = StockDataPoints[lastIndex];

			if (lastPointToRemove.Tick.LastTradePrice == newStockDataPoint.Tick.LastTradePrice)
			{
				if (activeChangeSummary != null)
				{
					changeSummaries.Add(activeChangeSummary);
					activeChangeSummary = null;
				}

				if (StockDataPoints[lastIndex - 1].Tick.LastTradePrice == newStockDataPoint.Tick.LastTradePrice)  // Last two points, plus this one are the same. We can remove the middle point.
				{
					// Track number of points removed... 
					newStockDataPoint.Weight += lastPointToRemove.Weight;

					// Keep lowest ask and highest bid across all duplicate points we remove...
					if (lastPointToRemove.Tick.LowestAskPrice < newStockDataPoint.Tick.LowestAskPrice)
						newStockDataPoint.Tick.LowestAskPrice = lastPointToRemove.Tick.LowestAskPrice;
					if (lastPointToRemove.Tick.HighestBidPrice > newStockDataPoint.Tick.HighestBidPrice)
						newStockDataPoint.Tick.HighestBidPrice = lastPointToRemove.Tick.HighestBidPrice;

					// Remove the duplicate point...
					StockDataPoints.RemoveAt(lastIndex);
					onFlatLine = true;
				}
			}
			else if (onFlatLine)
			{
				onFlatLine = false;
				activeChangeSummary = new ChangeSummary()
				{
					Start = newStockDataPoint,
					End = newStockDataPoint,
					High = newStockDataPoint,
					Low = newStockDataPoint,
					PointCount = 1
				};
			}
		}

		/// <summary>
		/// Removes any range summaries older than Start
		/// </summary>
		void RemoveOldRangeSummaries()
		{
			int numSummariesToRemove = 0;
			foreach (ChangeSummary rangeSummary in changeSummaries)
				if (rangeSummary.End.Time < start)
					numSummariesToRemove++;
				else  // range summaries are added in chronological order, so once we find the end of a range summary that is in bounds, we can skip the rest as they are automatically in bounds.
					break;

			for (int i = 0; i < numSummariesToRemove; i++)
				changeSummaries.RemoveAt(0);
		}

		/// <summary>
		/// Sets the high and/or low value based on the CustomTick's LastTradePrice.
		/// </summary>
		void SetHighLow(CustomTick tick)
		{
			if (tick.LastTradePrice < low)
				low = tick.LastTradePrice;
			if (tick.LastTradePrice > high)
				high = tick.LastTradePrice;
		}

		public StockDataPoint AddStockPosition(CustomTick data, DateTime? timeOverride = null)
		{
			StockDataPoint stockDataPoint = new StockDataPoint(data);

			if (timeOverride != null)
				stockDataPoint.Time = timeOverride.Value;

			lock (dataLock)
			{
				RemoveMatchingDataPoints(stockDataPoint);
				StockDataPoints.Add(stockDataPoint);
				if (activeChangeSummary != null && stockDataPoint != activeChangeSummary.End)
				{
					activeChangeSummary.End = stockDataPoint;
					activeChangeSummary.PointCount++;

					if (stockDataPoint.Tick.LastTradePrice > activeChangeSummary.High.Tick.LastTradePrice)
						activeChangeSummary.High = stockDataPoint;

					if (stockDataPoint.Tick.LastTradePrice < activeChangeSummary.Low.Tick.LastTradePrice)
						activeChangeSummary.Low = stockDataPoint;
				}
			}

			if (StockDataPoints.Count > MaxDataPointsToKeep)  // Only keep this many data points in history.
			{
				CustomTick removed = StockDataPoints[0].Tick;
				lock (dataLock)
					StockDataPoints.RemoveAt(0); // Remove oldest data point

				start = StockDataPoints[0].Time;
				RemoveOldRangeSummaries();

				// TODO: Invalidate instead of recalculate. Later, calculate on demand when the property is accessed.
				if (IsHighOrLow(removed))  // This data point may have been defining our high or low
					CalculateBounds();  // We need to recalculate everything.
				else  // Only the start and end have changed...
					AdjustBounds(stockDataPoint);
			}
			else if (StockDataPoints.Count < 10)
				CalculateBounds();
			else
				AdjustBounds(stockDataPoint);
			changedSinceLastDataDensityQuery = true;
			changedSinceLastSnapshot = true;

			return stockDataPoint;
		}

		/// <summary>
		/// Goes through every StockDataPoint and finds the highest/lowest values,  and also sets the start and end of the
		/// graph based on the times of the first  and last data points.
		/// </summary>
		public void CalculateBounds()
		{
			high = 0;
			low = decimal.MaxValue;
			lock (dataLock)
				foreach (StockDataPoint stockDataPoint in StockDataPoints)
					SetHighLow(stockDataPoint.Tick);
			CheckHighLow();

			lock (dataLock)
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

		public void Clear()
		{
			lock (dataLock)
			{
				StockDataPoints.Clear();
				changeSummaries.Clear();
			}
		}

		///// <param name="redistributePoints">If true, points will be redistributed away from flats (no change) toward 
		///// areas with more change (peaks and valleys).</param>
		/// <summary>
		/// Gets a list of StockDataPoints based on the specified segmentCount. So if I have 11 data points and I  specify
		/// five segments, this function will return six points. The first five points will contain averages of data at
		/// indexes 0 & 1, 2 & 3, 4 & 5, 6 & 7, and 8 & 9. The last point will always contain the last point in this
		/// ChartTranslator (no averaging or repeating).
		/// </summary>
		/// <param name="segmentCount">
		/// The number of segments to break the existing data into. Data points  will be repeated if more segments span a
		/// range than actual data points (in the ChartTranslator). Similarly, data points will be averaged if more than one
		/// appear in a single segment.
		/// </param>
		/// <returns>Returns the calculated StockDataPoints. The count will always be segmentCount plus one.</returns>
		public List<StockDataPoint> GetDataPointsAcrossSegments(
			int segmentCount,
			bool addChangeSummaries = false,
			bool distributeDensityAcrossFlats = false)
		{
			if (segmentCount == 0)
				return null;
			List<StockDataPoint> results = new List<StockDataPoint>();
			changedSinceLastDataDensityQuery = false;
			StockDataPointsSnapshot stockDataPointSnapshot = GetStockDataPointsSnapshot();

			TimeSpan dataTimeSpan = TimeSpan.FromTicks(
				(stockDataPointSnapshot.End - stockDataPointSnapshot.Start).Ticks / segmentCount);
			DateTime startSegment = stockDataPointSnapshot.Start;
			DateTime endSegment = stockDataPointSnapshot.Start + dataTimeSpan;

			double secondsPerSegment = dataTimeSpan.TotalSeconds;
			double segmentQuarterSpanSeconds = secondsPerSegment / 4.0;

			int leftStartIndex = 0;
			do
			{
				StockDataPoint dp = GetPointInRange(stockDataPointSnapshot, startSegment, endSegment, ref leftStartIndex);

				if (dp == null)
					break;

				results.Add(dp);
				startSegment += dataTimeSpan;
				endSegment += dataTimeSpan;

				if (leftStartIndex < stockDataPointSnapshot.DataPoints.Count - 1)
				{
					StockDataPoint nextPoint = stockDataPointSnapshot.DataPoints.ElementAt(leftStartIndex);
					if (!distributeDensityAcrossFlats)
					{
						if (nextPoint.Tick.LastTradePrice != dp.Tick.LastTradePrice)
						{
						}
					}
					else
						while (startSegment < nextPoint.Time)  // We are just going to keep adding the same point until the startSegment equals or is greater than the nextPoint.
						{
							results.Add(dp.Clone(startSegment));
							startSegment += dataTimeSpan;
							endSegment += dataTimeSpan;
						}
				}
			} while (results.Count < segmentCount);


			StockDataPoint last = stockDataPointSnapshot.DataPoints.ElementAt(stockDataPointSnapshot.DataPoints.Count - 1);

			results.Add(last);

			if (addChangeSummaries)
				AddChangeSummaries(results, stockDataPointSnapshot, segmentQuarterSpanSeconds);

			return results;
		}

		public StockDataPoint GetNearestPointInTime(DateTime time)
		{
			double closestSpanSoFar = double.MaxValue;
			StockDataPoint closestDataPoint = null;
			lock (dataLock)
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

		public TickRange GetPointsAroundTime(DateTime timeCenterPoint, int timeSpanSeconds)
		{
			double halfTimeSpan = timeSpanSeconds / 2.0;
			TimeSpan halfTimeSpanSeconds = TimeSpan.FromSeconds(halfTimeSpan);

			DateTime startRange = timeCenterPoint - halfTimeSpanSeconds;
			DateTime endRange = timeCenterPoint + halfTimeSpanSeconds;
			return GetPointsInRange(startRange, endRange);
		}

		public TickRange GetPointsInRange(DateTime start, DateTime end)
		{
			TickRange tickRange = new TickRange() { DataPoints = new List<StockDataPoint>(), };

			List<StockDataPoint> dataPoints = tickRange.DataPoints;

			StockDataPoint lastDataPoint = null;
			StockDataPoint lowestSoFar = null;
			StockDataPoint highestSoFar = null;

			lock (dataLock)
				foreach (StockDataPoint stockDataPoint in StockDataPoints)
					if (stockDataPoint.Time > end)
						break;
					else if (stockDataPoint.Time >= start)   // We can work with this data!
					{
						if (lastDataPoint != null)  // We have just entered the start of the desired range.
						{
							tickRange.ValueBeforeRangeStarts = lastDataPoint;
						}
						dataPoints.Add(stockDataPoint);
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
			if (dataPoints.Count > 0)
			{
				tickRange.Start = dataPoints[0].Time;
				tickRange.End = dataPoints[dataPoints.Count - 1].Time;
			}

			return tickRange;
		}

		public StockDataPointsSnapshot GetStockDataPointsSnapshot()
		{
			if (!changedSinceLastSnapshot)
				return lastSnapShot;

			lock (dataLock)
				lastSnapShot = new StockDataPointsSnapshot(
					StockDataPoints,
					start,
					end,
					low,
					high,
					changeSummaries,
					activeChangeSummary);

			changedSinceLastSnapshot = false;

			return lastSnapShot;
		}

		public TickRange GetTickRange(/* DateTime startRange, DateTime endRange */)
		{
			TickRange tickRange = new TickRange() { Start = start, End = end, DataPoints = new List<StockDataPoint>() };
			tickRange.DataPoints.AddRange(StockDataPoints);
			if (StockDataPoints.Count > 0)
				tickRange.ValueBeforeRangeStarts = StockDataPoints[0];
			tickRange.CalculateLowAndHigh();
			return tickRange;
		}

		public void SaveAll(string fullPathToFile)
		{
			string serializeObject = JsonConvert.SerializeObject(GetTickRange(), Formatting.Indented);
			File.WriteAllText(fullPathToFile, serializeObject);
		}

		public void SetStockDataPoints(List<StockDataPoint> points)
		{
			if (points.Count == 0)
				return;

			lock (dataLock)
				StockDataPoints.AddRange(points);
			//start = points[0].Time;
			//end = points[points.Count - 1].Time;
			CalculateBounds();
		}

		public void SetTickRange(TickRange tickRange)
		{
			if (tickRange.DataPoints.Count == 0)
				return;

			lock (dataLock)
			{
				StockDataPoints.Clear();
				StockDataPoints.AddRange(tickRange.DataPoints);
			}

			CalculateBounds();
			start = tickRange.Start;
			end = tickRange.End;
		}

		/// <summary>
		/// Adds a range of data points specified by (price, time offset in seconds) pairs.
		/// </summary>
		/// <param name="args">Pairs of numeric data - price followed by the time offset in seconds.</param>
		/// <exception cref="ArgumentException"></exception>
		public void TestAddDataPoints(params decimal[] args)
		{
			if (args.Length % 2 != 0)
				throw new ArgumentException($"Must pass an even number of args to {nameof(TestAddDataPoints)}.");

			for (int i = 0; i < args.Length; i += 2)
				TestAddStockDataPoint(args[i], (double)args[i + 1]);
		}

		/// <summary>
		/// Adds a sequence of prices as stock data points, each offset by one second.
		/// </summary>
		/// <param name="args"></param>
		public void TestAddPriceSequence(params decimal[] args)
		{
			for (int i = 0; i < args.Length; i++)
				TestAddStockDataPoint(args[i], i);
		}

		/// <summary>
		/// Adds a test stock data point.
		/// </summary>
		/// <param name="price"></param>
		/// <param name="offsetSeconds"></param>
		public void TestAddStockDataPoint(decimal price, double offsetSeconds)
		{
			AddStockPosition(
				new CustomTick() { LastTradePrice = price, HighestBidPrice = price, LowestAskPrice = price, Symbol = "BTC" },
				DateTime.MinValue + TimeSpan.FromSeconds(offsetSeconds));
		}
	}
}

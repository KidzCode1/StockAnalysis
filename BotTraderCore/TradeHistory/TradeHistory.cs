using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;

namespace BotTraderCore
{
	public class TradeHistory : INotifyPropertyChanged, IUpdatableTradeHistory
	{
		const int MaxDataPointAgeMinutes = 35;
		public event EventHandler<TradeHistory> DataPointAdded;
		internal readonly object dataLock = new object();
		ChangeSummary activeRangeSummary;  // access protected by dataLock
		readonly List<ChangeSummary> changeSummaries = new List<ChangeSummary>();  // access protected by dataLock
		readonly List<DataPoint> stockDataPoints = new List<DataPoint>();  // access protected by dataLock

		bool changedSinceLastDataDensityQuery;
		bool changedSinceLastSnapshot = true;

		DataPointsSnapshot lastSnapShot;

		bool onFlatLine;

		DateTime start;
		DateTime end;
		decimal low = 35000;
		decimal high = 37000;
		int saveDataPointCount;
		bool needToSaveData;
		string saveFileName;
		DateTime saveTime;

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// The weighted total price of all trades in this history. Weighted because 
		/// three or more sequential same-price data points are consolidated into 
		/// one with a Weight property that represents how many matching trades 
		/// are consolidated into a single data point.
		/// </summary>
		public decimal WeightedTotalPrice { get; set; }

		/// <summary>
		/// The number of data points this history represents. Weighted because 
		/// three or more sequential same-price data points are consolidated into 
		/// one with a Weight property that represents how many matching trades 
		/// are consolidated into a single data point.
		/// </summary>
		public int WeightedCount { get; set; }

		public decimal AveragePrice
		{
			get
			{
				decimal averagePrice = 0;

				lock (dataLock)
					if (WeightedCount != 0)
						averagePrice = WeightedTotalPrice / WeightedCount;

				return averagePrice;
			}
		}

		public decimal StandardDeviation
		{
			get
			{
				if (cacheStandardDeviation != decimal.MinValue)
					return cacheStandardDeviation;
				decimal sum = 0;
				decimal count = 0;
				decimal averagePrice = AveragePrice;

				lock (dataLock)
					foreach (DataPoint stockDataPoint in StockDataPoints)
					{
						decimal deviation = stockDataPoint.Tick.LastTradePrice - averagePrice;
						decimal deviationSquared = deviation * deviation;
						sum += deviationSquared * stockDataPoint.Weight;
						count += stockDataPoint.Weight;
					}

				if (count == 0)
					cacheStandardDeviation = 0;
				else
					cacheStandardDeviation = sum / count;
				return (decimal)Math.Sqrt((double)cacheStandardDeviation);
			}
		}

		public void InvalidateDataPointCaches()
		{
			cacheStandardDeviation = decimal.MinValue;
		}


		decimal quoteCurrencyToUsdConversion;
		decimal cacheStandardDeviation = decimal.MinValue;
		public decimal QuoteCurrencyToUsdConversion => quoteCurrencyToUsdConversion;

		public void SetQuoteCurrencyToUsdConversion(decimal amount)
		{
			quoteCurrencyToUsdConversion = amount;
		}

		void SetQuoteCurrencyToUsdConversion()
		{
			if (string.IsNullOrWhiteSpace(SymbolPair))
			{
				Logging.Alert.Error("SymbolPair is null or empty.");
				SetQuoteCurrencyToUsdConversion(1);
				return;
			}

			string quoteCurrencySymbol = BinanceSymbolLookup.GetQuoteCurrency(SymbolPair);

			if (quoteCurrencySymbol == null)
			{
				Logging.Alert.Error($"Unable to determine quote currency from symbol pair: \"{SymbolPair}\"");
				return;
			}

			SetQuoteCurrencyToUsdConversion(PriceConverter.GetPriceUsd(quoteCurrencySymbol));
		}



		public TradeHistory(string symbolPair = null)
		{
			SymbolPair = symbolPair;
			SetQuoteCurrencyToUsdConversion();
		}

		/// <summary>
		/// The value amount (in the quote currency) represented by all the points in this
		/// trade history. The highest value minus the lowest.
		/// </summary>
		public decimal ValueSpan => High - Low;

		public bool ChangedSinceLastDataDensityQuery
		{
			get { return changedSinceLastDataDensityQuery; }
			set { changedSinceLastDataDensityQuery = value; }
		}

		public int DataPointCount
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

		public int MaxDataPointsToKeep { get; set; } = 450;
		public int MaxDataPointsToKeepWithBuySignal { get; set; } = 10000;

		public decimal PercentInView => ValueSpan / High * 100;
		public TimeSpan SpanAcross => End - Start;

		public List<DataPoint> StockDataPoints => stockDataPoints;
		public string SymbolPair { get; set; }

		/// <summary>
		/// DataPoints representing buy signals for this trade history.
		/// </summary>
		public List<DataPoint> BuySignals { get; set; } = new List<DataPoint>();


		/// <summary>
		/// DataPoints representing sell signals for this trade history.
		/// </summary>
		public List<DataPoint> SellSignals { get; set; } = new List<DataPoint>();

		/// <summary>
		/// The average price at the time the first buy signal arrived.
		/// </summary>
		public decimal AveragePriceAtBuySignal { get; set; }

		/// <summary>
		/// The StandardDeviation at the time the first buy signal arrived.
		/// </summary>
		public decimal StandardDeviationAtBuySignal { get; set; }

		public bool NeedToSaveData { get => needToSaveData; }

		public BuySignalAction BuySignalAction { get; set; }

		int updateCount;
		bool changedDataPoints;
		bool holdsTestData;

		public void BeginUpdate()
		{
			if (updateCount == 0)
				changedDataPoints = false;
			updateCount++;
			saveDataPointCount = DataPointCount;
		}

		public void EndUpdate()
		{
			updateCount--;
			if (updateCount == 0)
			{
				if (saveDataPointCount == DataPointCount && !changedDataPoints)
					return;

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataPointCount)));
			}
		}

		public void AddStockPositionWithUpdate(CustomTick data, DateTime? timeOverride)
		{
			BeginUpdate();
			try
			{
				AddStockPosition(data, timeOverride);
			}
			finally
			{
				EndUpdate();
			}
		}

		bool RemoveAgingDataPoints()
		{
			if (holdsTestData)
				return false;

			lock (dataLock)
			{
				if (HasBuySignal())
					return false;  // If we have a buy signal, don't remove older points.

				if (StockDataPoints.Count == 0)
					return false;

				DateTime now = DateTime.Now;
				int numLeadingPointsToRemove = 0;
				for (int i = 0; i < StockDataPoints.Count; i++)
				{
					if ((now - StockDataPoints[i].Time).TotalMinutes < MaxDataPointAgeMinutes)
						break;

					numLeadingPointsToRemove++;
				}

				if (numLeadingPointsToRemove == 0)
					return false;

				StockDataPoints.RemoveRange(0, numLeadingPointsToRemove);
				changedDataPoints = true;
			}
			CalculateBounds();
			return true;
		}

		private bool HasBuySignal()
		{
			return BuySignals != null && BuySignals.Count > 0;
		}

		private bool HasSellSignal()
		{
			return SellSignals != null && SellSignals.Count > 0;
		}

		public DataPoint AddStockPosition(CustomTick data, DateTime? timeOverride = null)
		{
			BeginUpdate();
			DataPoint stockDataPoint = new DataPoint(data);

			if (timeOverride != null)
				stockDataPoint.Time = timeOverride.Value;

			lock (dataLock)
			{
				WeightedCount++;
				WeightedTotalPrice += stockDataPoint.Tick.LastTradePrice;

				RemoveMatchingDataPoints(stockDataPoint);
				StockDataPoints.Add(stockDataPoint);
				changedDataPoints = true;
				UpdateRangeSummaries(stockDataPoint);
			}

			bool removedOldDataPoints = RemoveAgingDataPoints();

			if (HasExcessDataPoints())
				RemoveExcessDataPoints(stockDataPoint);
			else if (!removedOldDataPoints)
				if (StockDataPoints.Count < 10)
					CalculateBounds();
				else
					AdjustBounds(stockDataPoint);
			changedSinceLastDataDensityQuery = true;
			changedSinceLastSnapshot = true;

			EndUpdate();

			if (needToSaveData && saveTime < DateTime.Now)
				SaveNow();

			DataPointAdded?.Invoke(this, this);

			return stockDataPoint;
		}

		private bool HasExcessDataPoints()
		{
			return (StockDataPoints.Count > MaxDataPointsToKeep && !HasBuySignal()) || StockDataPoints.Count > MaxDataPointsToKeepWithBuySignal;
		}

		private void RemoveExcessDataPoints(DataPoint newestDataPoint)
		{
			DataPoint removed = StockDataPoints[0];
			lock (dataLock)
			{
				WeightedCount -= removed.Weight;
				WeightedTotalPrice -= removed.Tick.LastTradePrice * removed.Weight;
				StockDataPoints.RemoveAt(0); // Remove oldest data point
				changedDataPoints = true;
			}

			start = StockDataPoints[0].Time;
			RemoveOldRangeSummaries();

			// TODO: Invalidate instead of recalculate. Later, calculate on demand when the property is accessed.
			if (IsHighOrLow(removed.Tick))  // This data point may have been defining our high or low
				CalculateBounds();  // We need to recalculate everything.
			else  // Only the start and end have changed...
				AdjustBounds(newestDataPoint);
		}

		private void UpdateRangeSummaries(DataPoint stockDataPoint)
		{
			if (activeRangeSummary != null && stockDataPoint != activeRangeSummary.End)
			{
				activeRangeSummary.End = stockDataPoint;
				activeRangeSummary.PointCount++;

				if (stockDataPoint.Tick.LastTradePrice > activeRangeSummary.High.Tick.LastTradePrice)
					activeRangeSummary.High = stockDataPoint;

				if (stockDataPoint.Tick.LastTradePrice < activeRangeSummary.Low.Tick.LastTradePrice)
					activeRangeSummary.Low = stockDataPoint;
			}
		}

		static List<ChangeSummary> GetChangeSummariesInRange(
			DataPointsSnapshot stockDataPointSnapshot,
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


		/// <summary>
		/// Calculates an average price across all the given points in the span.
		/// </summary>
		/// <param name="pointsInSpan">The points to calculate.</param>
		/// <returns>The average price.</returns>
		public static decimal CalculateAveragePrice(List<DataPoint> pointsInSpan)
		{
			decimal totalPrice = 0;
			int totalWeight = 0;
			foreach (DataPoint dataPoint in pointsInSpan)
			{
				totalWeight += dataPoint.Weight;
				totalPrice += dataPoint.Tick.LastTradePrice * dataPoint.Weight;
			}

			if (totalWeight == 0)
				return decimal.MinValue;

			decimal averagePrice = totalPrice / totalWeight;
			return averagePrice;
		}

		/// <summary>
		/// Adjusts the high, low, and end, based on the new data point.
		/// </summary>
		/// <param name="latestDataPoint"></param>
		void AdjustBounds(DataPoint latestDataPoint)
		{
			SetHighLow(latestDataPoint.Tick);
			CheckHighLow();
			end = latestDataPoint.Time;
			InvalidateDataPointCaches();
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

		/// <summary>
		/// Returns true if the specified tick price matches the existing high or low price for 
		/// the trade history.
		/// </summary>
		bool IsHighOrLow(CustomTick tick)
		{
			return tick.LastTradePrice == High || tick.LastTradePrice == Low;
		}

		/// <summary>
		/// Call before adding a new point to see if last two trailing ticks match this one (comparing LastTradePrice only).
		/// This method works directly on stockDataPoints so make sure this call is protected by a lock on stockDataPointsLock
		/// before calling.
		/// </summary>
		/// <param name="newStockDataPoint">The new StockDataPoint we're about to add.</param>
		void RemoveMatchingDataPoints(DataPoint newStockDataPoint)
		{
			if (StockDataPoints.Count < 2)
				return;
			int lastIndex = StockDataPoints.Count - 1;
			DataPoint lastPointToRemove = StockDataPoints[lastIndex];

			if (lastPointToRemove.Tick.LastTradePrice == newStockDataPoint.Tick.LastTradePrice)
			{
				if (activeRangeSummary != null)
				{
					changeSummaries.Add(activeRangeSummary);
					activeRangeSummary = null;
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
					changedDataPoints = true;
					onFlatLine = true;
				}
			}
			else if (onFlatLine)
			{
				onFlatLine = false;
				activeRangeSummary = new ChangeSummary()
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

		/// <summary>
		/// Goes through every StockDataPoint and finds the highest/lowest values,  and also sets the start and end of the
		/// graph based on the times of the first and last data points.
		/// </summary>
		public void CalculateBounds()
		{
			SetPriceBounds();
			SetTimeBounds();
			InvalidateDataPointCaches();
		}

		/// <summary>
		/// Sets the high and low price bounds for the history.
		/// </summary>
		private void SetPriceBounds()
		{
			high = 0;
			low = decimal.MaxValue;
			lock (dataLock)
				foreach (DataPoint stockDataPoint in StockDataPoints)
					SetHighLow(stockDataPoint.Tick);
			CheckHighLow();
		}

		/// <summary>
		/// Sets the Start and End time bounds for the history.
		/// </summary>
		private void SetTimeBounds()
		{
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

		/// <summary>
		/// Clears all data points.
		/// </summary>
		public void Clear()
		{
			BeginUpdate();

			lock (dataLock)
			{
				StockDataPoints.Clear();
				changeSummaries.Clear();
				changedDataPoints = true;
				CalculateBounds();
			}

			EndUpdate();
		}

		public List<DataPoint> GetDataPointsAcrossSegments(int segmentCount,
			bool addChangeSummaries = false,
			bool distributeDensityAcrossFlats = false)
		{
			if (segmentCount == 0)
				return null;
			changedSinceLastDataDensityQuery = false;
			DataPointsSnapshot stockDataPointSnapshot = GetSnapshot();
			return stockDataPointSnapshot.GetDataPointsAcrossSegments(segmentCount, addChangeSummaries, distributeDensityAcrossFlats);
		}

		/// <summary>
		/// Gets the DataPoint from the history that is nearest to the specified time.
		/// </summary>
		public DataPoint GetNearestPointInTime(DateTime time)
		{
			double closestSpanSoFar = double.MaxValue;
			DataPoint closestDataPoint = null;
			lock (dataLock)
				foreach (DataPoint stockDataPoint in StockDataPoints)
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

		/// <summary>
		/// Collects all the points close to the specified center point, contained within the 
		/// specified TimeSpan.
		/// </summary>
		/// <param name="timeCenterPoint">The time point on which to center the search.</param>
		/// <param name="timeSpanSeconds">The total time span (centered on the center point) to search.</param>
		/// <returns>All the points in the specified range.</returns>
		public TickRange GetPointsAroundTime(DateTime timeCenterPoint, int timeSpanSeconds)
		{
			double halfTimeSpan = timeSpanSeconds / 2.0;
			TimeSpan halfTimeSpanSeconds = TimeSpan.FromSeconds(halfTimeSpan);

			DateTime startRange = timeCenterPoint - halfTimeSpanSeconds;
			DateTime endRange = timeCenterPoint + halfTimeSpanSeconds;
			return GetPointsInRange(startRange, endRange);
		}

		/// <summary>
		/// Gets all the points in the specified range.
		/// </summary>
		public TickRange GetPointsInRange(DateTime start, DateTime end)
		{
			TickRange tickRange = new TickRange() { DataPoints = new List<DataPoint>(), };

			List<DataPoint> dataPoints = tickRange.DataPoints;

			DataPoint lastDataPoint = null;
			DataPoint lowestSoFar = null;
			DataPoint highestSoFar = null;

			lock (dataLock)
				foreach (DataPoint stockDataPoint in StockDataPoints)
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

		/// <summary>
		/// Gets a read-only snapshot of the trade history.
		/// </summary>
		public DataPointsSnapshot GetSnapshot()
		{
			if (!changedSinceLastSnapshot)
				return lastSnapShot;

			lock (dataLock)
				lastSnapShot = new DataPointsSnapshot(
					StockDataPoints,
					start,
					end,
					low,
					high,
					changeSummaries,
					activeRangeSummary,
					BuySignals,
					SymbolPair,
					QuoteCurrencyToUsdConversion, AveragePriceAtBuySignal, StandardDeviationAtBuySignal);
			
			if (SellSignals != null && SellSignals.Count > 0)
				lastSnapShot.SellSignals = new List<DataPoint>(SellSignals);

			lastSnapShot.BuySignalAction = BuySignalAction;

			changedSinceLastSnapshot = false;

			return lastSnapShot;
		}

		public TickRange GetTickRange(/* DateTime startRange, DateTime endRange */)
		{
			return TradeHistoryHelper.GetTickRange(this);
		}

		public void SaveTickRange(string fullPathToFile)
		{
			string serializeObject = JsonConvert.SerializeObject(GetTickRange(), Formatting.Indented);
			File.WriteAllText(fullPathToFile, serializeObject);
		}

		public void SetStockDataPoints(List<DataPoint> points)
		{
			if (points.Count == 0)
				return;

			BeginUpdate();

			lock (dataLock)
				StockDataPoints.AddRange(points);
			changedDataPoints = true;
			//start = points[0].Time;
			//end = points[points.Count - 1].Time;
			CalculateBounds();

			EndUpdate();
		}

		public void SetTickRange(TickRange tickRange)
		{
			if (tickRange.DataPoints.Count == 0)
				return;

			BeginUpdate();

			lock (dataLock)
			{
				StockDataPoints.Clear();
				StockDataPoints.AddRange(tickRange.DataPoints);
				changedDataPoints = true;
			}

			CalculateBounds();
			start = tickRange.Start;
			end = tickRange.End;

			EndUpdate();
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

			BeginUpdate();
			try
			{
				holdsTestData = true;
				for (int i = 0; i < args.Length; i += 2)
					TestAddStockDataPoint(args[i], (double)args[i + 1]);
			}
			finally
			{
				EndUpdate();
			}

			CalculateBounds();
		}

		/// <summary>
		/// Adds a sequence of prices as stock data points, each offset by one second.
		/// </summary>
		/// <param name="args"></param>
		public void TestAddPriceSequence(params decimal[] args)
		{
			BeginUpdate();
			try
			{
				holdsTestData = true;
				for (int i = 0; i < args.Length; i++)
					TestAddStockDataPoint(args[i], i);
			}
			finally
			{
				EndUpdate();
			}

			CalculateBounds();
		}

		/// <summary>
		/// Adds a test stock data point.
		/// </summary>
		/// <param name="price"></param>
		/// <param name="offsetSeconds"></param>
		public void TestAddStockDataPoint(decimal price, double offsetSeconds)
		{
			holdsTestData = true;
			AddStockPosition(
				new CustomTick() { LastTradePrice = price, HighestBidPrice = price, LowestAskPrice = price, Symbol = "BTC" },
				DateTime.MinValue + TimeSpan.FromSeconds(offsetSeconds));
		}

		/// <summary>
		/// Generates a specified number of data points, each offset from DateTime's MinValue by one second more than the 
		/// previous point.
		/// </summary>
		/// <param name="count">The number of data points to create.</param>
		public void TestAddRandomPriceSequence(int count)
		{
			BeginUpdate();
			try
			{
				holdsTestData = true;
				RandomTickGenerator testTickerGenerator = new RandomTickGenerator();
				for (int i = 0; i < count; i++)
				{
					CustomTick newCustomTick = testTickerGenerator.GetNewCustomTick();
					AddStockPosition(newCustomTick, DateTime.MinValue + TimeSpan.FromSeconds(i));
				}
			}
			finally
			{
				EndUpdate();
			}
		}

		/// <summary>
		/// Stretches the time span across all the data points to match the specified duration.
		/// </summary>
		public void TestStretchTimeSpanTo(TimeSpan targetTimeSpan)
		{
			holdsTestData = true;
			double stretchFactor = (double)targetTimeSpan.Ticks / SpanAcross.Ticks;
			lock (dataLock)
			{
				foreach (DataPoint dataPoint in StockDataPoints)
				{
					long ticksToPoint = dataPoint.Time.Ticks - start.Ticks;
					double adjustedTicks = ticksToPoint * stretchFactor;
					dataPoint.Time = start + TimeSpan.FromTicks((long)adjustedTicks);
				}
			}
			SetTimeBounds();
		}

		/// <summary>
		/// Marks this TradeHistory as needing to save, and saves the history after the specified 
		/// time elapses (when a new data point arrives after the specified time span).
		/// </summary>
		/// <param name="timeSpan">The TimeSpan to wait before saving.</param>
		/// <param name="fileName">The name of the file to save to.</param>
		public void SaveDataIn(TimeSpan timeSpan, string fileName)
		{
			saveFileName = fileName;
			needToSaveData = true;
			saveTime = DateTime.Now + timeSpan;
		}

		public void AddSellSignal(DataPointsSnapshot snapshot)
		{
			lock (dataLock)
			{
				SellSignals.Add(snapshot.LastDataPoint);
			}
		}

		public void AddBuySignal(DataPointsSnapshot snapshot)
		{
			bool addBuySignalStats = false;
			lock (dataLock)
			{
				addBuySignalStats = BuySignals.Count == 0;
				BuySignals.Add(snapshot.LastDataPoint);
			}

			Debugger.Log(0, "Debug", $"Buy Signal for {SymbolPair} at {DateTime.Now}.\n");

			if (addBuySignalStats)
			{
				AveragePriceAtBuySignal = snapshot.GetAveragePriceExceptLastTwo();
				StandardDeviationAtBuySignal = snapshot.GetStandardDeviationExceptLastTwo();
				Debugger.Log(0, "Debug", $"  Average price up to now: {PriceConverter.GetUsdStr(AveragePriceAtBuySignal, SymbolPair)}\n");
				Debugger.Log(0, "Debug", $"  Standard deviation: {PriceConverter.GetUsdStr(AveragePriceAtBuySignal, SymbolPair)}\n");
				Debugger.Log(0, "Debug", $"  Price (USD): {PriceConverter.GetUsdStr(snapshot.LastDataPoint.Tick.LastTradePrice, SymbolPair)}\n");
				Debugger.Log(0, "Debug", $"  Volume (USD): {PriceConverter.GetUsdStr(snapshot.LastDataPoint.Tick.QuoteVolume, SymbolPair)}\n\n");
			}
		}

		public void GetFirstLastLowHigh(out DataPoint first, out DataPoint last, out DataPoint low, out DataPoint high, DateTime start, DateTime end)
		{
			List<DataPoint> pointsInRange;

			lock (dataLock)
				pointsInRange = StockDataPoints.Where(x => x.Time >= start && x.Time <= end).ToList();

			TradeHistoryHelper.GetFirstLastLowHigh(pointsInRange, out first, out last, out low, out high);
		}

		public DataPointsSnapshot GetTruncatedSnapshot(DateTime start, DateTime end)
		{
			return TradeHistoryHelper.GetTruncatedSnapshot(this, start, end);
		}

		public void SaveNow()
		{
			if (!needToSaveData)
				return;

			DataPointsSnapshot snapshot = GetSnapshot();
			int minutesSinceBuy = 99;
			string snapshotJson = JsonConvert.SerializeObject(snapshot);
			if (snapshot.BuySignals != null && snapshot.BuySignals.Count > 0)
			{
				TimeSpan spanSinceBuy = DateTime.Now - snapshot.BuySignals[0].Time;
				minutesSinceBuy = (int)Math.Floor(spanSinceBuy.TotalMinutes);
			}

			saveFileName = saveFileName.Replace("$minutesPast$", minutesSinceBuy.ToString());

			File.WriteAllText(saveFileName, snapshotJson);
			needToSaveData = false;

			BuySignals.Clear();
		}

		public void Sold()
		{
			BuySignals = new List<DataPoint>();
			SellSignals = new List<DataPoint>();
			RemoveAgingDataPoints();
			CalculateBounds();
		}
	}
}

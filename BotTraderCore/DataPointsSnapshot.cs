using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Collections;

namespace BotTraderCore
{
	public class DataPointsSnapshot: ITradeHistory
	{
		DataPoint lastDataPoint;
		DataPoint secondToLastDataPoint;
		DataPoint thirdToLastDataPoint;

		public DataPoint LastDataPoint => lastDataPoint;
		public DataPoint SecondToLastDataPoint => secondToLastDataPoint;
		public DataPoint ThirdToLastDataPoint => thirdToLastDataPoint;

		readonly IReadOnlyCollection<DataPoint> dataPoints;
		readonly IReadOnlyCollection<ChangeSummary> changeSummaries;

		decimal quoteCurrencyToUsdConversion = 1;

		public DataPointsSnapshot(List<DataPoint> dataPoints, DateTime start, DateTime end, decimal low, decimal high, List<ChangeSummary> changeSummaries, ChangeSummary activeChangeSummary, List<DataPoint> buySignals, string symbolPair, decimal quoteCurrencyToUsdConversion, decimal averagePriceAtBuySignal, decimal standardDeviationAtBuySignal)
		{
			StandardDeviationAtBuySignal = standardDeviationAtBuySignal;
			AveragePriceAtBuySignal = averagePriceAtBuySignal;

			SymbolPair = symbolPair;

			if (quoteCurrencyToUsdConversion == 0)
				GetPriceConversion();
			else
				this.quoteCurrencyToUsdConversion = quoteCurrencyToUsdConversion;
			
			if (buySignals == null)
				BuySignals = new List<DataPoint>();
			else
				BuySignals = new List<DataPoint>(buySignals);
			High = high;
			Low = low;
			End = end;
			Start = start;
			List<DataPoint> dataPointsList = dataPoints.ToList();

			if (dataPointsList.Count >= 1)
				lastDataPoint = dataPointsList[dataPointsList.Count - 1];

			if (dataPointsList.Count >= 2)
				secondToLastDataPoint = dataPointsList[dataPointsList.Count - 2];

			if (dataPointsList.Count >= 3)
				thirdToLastDataPoint = dataPointsList[dataPointsList.Count - 3];

			this.dataPoints = new ReadOnlyCollection<DataPoint>(dataPointsList);

			List<ChangeSummary> localChangeSummaries;
			if (changeSummaries == null)
				localChangeSummaries = new List<ChangeSummary>();
			else
				localChangeSummaries = changeSummaries.ToList();

			if (activeChangeSummary != null)
				localChangeSummaries.Add(activeChangeSummary);
			this.changeSummaries = new ReadOnlyCollection<ChangeSummary>(localChangeSummaries);
		}

		public IReadOnlyCollection<DataPoint> DataPoints { get => dataPoints; }
		public IReadOnlyCollection<ChangeSummary> ChangeSummaries { get => changeSummaries; }


		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public decimal Low { get; set; }
		public decimal High { get; set; }

		/// <summary>
		/// DataPoints representing buy signals for this snapshot.
		/// </summary>
		public List<DataPoint> BuySignals { get; set; }

		public decimal AveragePrice => GetAveragePrice();

		public decimal StandardDeviation => GetStandardDeviation();

		public int DataPointCount => DataPoints.Count;

		public decimal PercentInView => ValueSpan / High * 100;

		public decimal QuoteCurrencyToUsdConversion => quoteCurrencyToUsdConversion;

		public TimeSpan SpanAcross => End - Start;

		public List<DataPoint> StockDataPoints => DataPoints.ToList();

		public decimal ValueSpan => High - Low;

		public string SymbolPair { get; set; }
		public decimal AveragePriceAtBuySignal { get; set; }
		public decimal StandardDeviationAtBuySignal { get; set; }

		decimal average = decimal.MinValue;
		decimal averageMinusTwo = decimal.MinValue;
		decimal standardDeviation = decimal.MinValue;
		decimal standardDeviationMinusTwo = decimal.MinValue;

		public decimal GetAveragePrice()
		{
			if (average == decimal.MinValue)
				average = CalculateAveragePrice();

			return average;
		}

		/// <summary>
		/// Gets the average for all points except the last two.
		/// </summary>
		public decimal GetAveragePriceExceptLastTwo()
		{
			if (averageMinusTwo == decimal.MinValue)
				averageMinusTwo = CalculateAveragePrice(2);

			return averageMinusTwo;
		}

		public decimal GetStandardDeviation()
		{
			if (standardDeviation == decimal.MinValue)
				standardDeviation = CalculateStandardDeviation();
			return standardDeviation;
		}

		/// <summary>
		/// Gets the standard deviation for all points except the last two.
		/// </summary>
		public decimal GetStandardDeviationExceptLastTwo()
		{
			if (standardDeviationMinusTwo == decimal.MinValue)
				standardDeviationMinusTwo = CalculateStandardDeviation(2);
			return standardDeviationMinusTwo;
		}

		private decimal CalculateAveragePrice(int numEndPointsToExclude = 0)
		{
			decimal sum = 0;
			decimal count = 0;
			int lastIndex = DataPoints.Count - 1 - numEndPointsToExclude;
			int index = 0;
			foreach (DataPoint stockDataPoint in DataPoints)
			{
				sum += stockDataPoint.Tick.LastTradePrice * stockDataPoint.Weight;
				count += stockDataPoint.Weight;
				if (index >= lastIndex)
					break;
				index++;
			}
			if (count == 0)
				return 0;
			return sum / count;
		}

		private decimal CalculateStandardDeviation(int numEndPointsToExclude = 0)
		{
			decimal sum = 0;
			decimal count = 0;
			decimal averagePrice;

			if (numEndPointsToExclude == 0)
				averagePrice = GetAveragePrice();
			else if (numEndPointsToExclude == 2)
				averagePrice = GetAveragePriceExceptLastTwo();
			else
				averagePrice = CalculateAveragePrice(numEndPointsToExclude);

			int lastIndex = DataPoints.Count - 1 - numEndPointsToExclude;
			int index = 0;
			foreach (DataPoint stockDataPoint in DataPoints)
			{
				decimal deviation = stockDataPoint.Tick.LastTradePrice - averagePrice;
				decimal deviationSquared = deviation * deviation;
				sum += deviationSquared * stockDataPoint.Weight;
				count += stockDataPoint.Weight;
				if (index >= lastIndex)
					break;
				index++;
			}
			if (count == 0)
				return 0m;
			decimal variance = sum / count;
			return (decimal)Math.Sqrt((double)variance);
		}

		public int GetIndexOfPointOnOrBefore(DateTime time, int leftStartIndex = 0)
		{
			DataPoint lastPreviousDataPoint = null;
			int lastPreviousIndex = -1;

			int left = leftStartIndex;
			int right = DataPoints.Count - 1;
			while (left <= right)
			{
				int middle = left + (right - left) / 2;

				DataPoint middleDataPoint = DataPoints.ElementAt(middle);
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
		/// Returns true if the two specified prices are close in value (to within 0.02%).
		/// </summary>
		bool AreClose(decimal price1, decimal price2)
		{
			decimal averagePrice = (price1 + price2) / 2;
			return Math.Abs(price1 - price2) < averagePrice * 0.0001m;
		}

		public void AddChangeSummaries(List<DataPoint> results, double segmentQuarterSpanSeconds)
		{
			if (ChangeSummaries.Count == 0)
				return;

			DataPoint lastStockPoint = null;

			foreach (ChangeSummary changeSummary in ChangeSummaries)
			{
				results.Add(changeSummary.Start);
				results.Add(changeSummary.Low);
				results.Add(changeSummary.High);
				results.Add(changeSummary.End);
			}

			results.Sort((x, y) => Math.Sign((x.Time - y.Time).TotalSeconds));

			decimal average = results.Select(m => m.Tick.LastTradePrice).Average();

			DateTime lastTime = DateTime.MinValue;
			List<DataPoint> stockDataPointsToRemove = new List<DataPoint>();
			DataPoint priorToLastDataPoint = null;
			foreach (DataPoint stockPoint in results)
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

			foreach (DataPoint stockDataPointToRemove in stockDataPointsToRemove)
				results.Remove(stockDataPointToRemove);
		}

		/// <summary>
		/// Returns a single point that represents the specified range (an average of all data points in that space, or the
		/// previous point's value if there are no points in the range).
		/// </summary>
		/// <param name="startSegment"></param>
		/// <param name="endSegment"></param>
		public DataPoint GetPointInRange(
			DateTime startSegment,
			DateTime endSegment,
			ref int leftStartIndex)
		{
			int startIndex = GetIndexOfPointOnOrBefore(startSegment, leftStartIndex);

			if (startIndex == -1)
				return null;

			// The length of the first value is the time of the second tick minus the startSegment.
			// The length of the last value is the endSegment minus the time of the last tick.
			// All other tick lengths are the time of that tick minus the time of the last tick.

			decimal totalLastTradePrice = 0;
			decimal totalHighestBidPrice = 0;
			decimal totalQuoteVolume = 0;
			decimal totalLowestAskPrice = 0;
			decimal totalDurationSeconds = 0;
			int index = startIndex;
			DataPoint stockDataPoint;
			while (index < DataPoints.Count)
			{
				stockDataPoint = DataPoints.ElementAt(index);
				if (stockDataPoint.Time > endSegment)
					break;

				index++;

				DateTime startTime;
				if (index == startIndex) // This is the first point.
					startTime = startSegment;
				else
					startTime = stockDataPoint.Time;

				DateTime endTime;
				if (index < DataPoints.Count)
				{
					DataPoint nextStockDataPoint = DataPoints.ElementAt(index);
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
				totalQuoteVolume += durationSeconds * tick.QuoteVolume;
				totalLowestAskPrice += durationSeconds * tick.LowestAskPrice;
			}

			if (totalDurationSeconds == 0)
				return null;

			decimal lastTradePrice = totalLastTradePrice / totalDurationSeconds;
			decimal highestBidPrice = totalHighestBidPrice / totalDurationSeconds;
			decimal quoteVolume = totalQuoteVolume / totalDurationSeconds;
			decimal lowestAskPrice = totalLowestAskPrice / totalDurationSeconds;
			return new DataPoint(
								new CustomTick()
								{
									LastTradePrice = lastTradePrice,
									HighestBidPrice = highestBidPrice,
									LowestAskPrice = lowestAskPrice,
									QuoteVolume = quoteVolume
								})
			{
				Time = startSegment
			};
		}

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
		public List<DataPoint> GetDataPointsAcrossSegments(int segmentCount, bool addChangeSummaries, bool distributeDensityAcrossFlats)
		{
			List<DataPoint> results = new List<DataPoint>();

			TimeSpan dataTimeSpan = TimeSpan.FromTicks(
							(End - Start).Ticks / segmentCount);
			DateTime startSegment = Start;
			DateTime endSegment = Start + dataTimeSpan;

			double secondsPerSegment = dataTimeSpan.TotalSeconds;
			double segmentQuarterSpanSeconds = secondsPerSegment / 4.0;

			int leftStartIndex = 0;
			do
			{
				DataPoint dp = GetPointInRange(startSegment, endSegment, ref leftStartIndex);

				if (dp == null)
					break;

				results.Add(dp);
				startSegment += dataTimeSpan;
				endSegment += dataTimeSpan;

				if (leftStartIndex < DataPoints.Count - 1)
				{
					DataPoint nextPoint = DataPoints.ElementAt(leftStartIndex);
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


			if (DataPoints.Count > 0)
				results.Add(DataPoints.ElementAt(DataPoints.Count - 1));

			if (addChangeSummaries)
				AddChangeSummaries(results, segmentQuarterSpanSeconds);

			return results;
		}

		public DataPointsSnapshot GetSnapshot()
		{
			return this;
		}

		TickRange GetTickRange()
		{
			return TradeHistoryHelper.GetTickRange(this);
		}

		public void SaveTickRange(string fullPathToFile)
		{
			string serializeObject = JsonConvert.SerializeObject(GetTickRange(), Formatting.Indented);
			File.WriteAllText(fullPathToFile, serializeObject);
		}

		public List<DataPoint> GetAllPointsBefore(DateTime start)
		{
			return DataPoints.Where(x => x.Time < start).ToList();
		}

		public void GetPriceConversion()
		{
			if (string.IsNullOrWhiteSpace(SymbolPair))
				quoteCurrencyToUsdConversion = 1;
			else
				quoteCurrencyToUsdConversion = PriceConverter.GetPriceUsd(SymbolPair);
		}

		public void Loaded()
		{
			if (BuySignals.Count == 0)
				return;
			if (StandardDeviationAtBuySignal != 0 && AveragePriceAtBuySignal != 0)
				return;
			int numEndPointsToExclude = DataPoints.Where(x => x.Time >= BuySignals[0].Time).Count() + 2;
			StandardDeviationAtBuySignal = CalculateStandardDeviation(numEndPointsToExclude);
			AveragePriceAtBuySignal = CalculateAveragePrice(numEndPointsToExclude);
		}

		public void GetFirstLastLowHigh(out DataPoint first, out DataPoint last, out DataPoint low, out DataPoint high, DateTime start, DateTime end)
		{
			List<DataPoint> pointsInRange = TradeHistoryHelper.GetPointsInRange(StockDataPoints, start, end);
			TradeHistoryHelper.GetFirstLastLowHigh(pointsInRange, out first, out last, out low, out high);
		}

		public DataPointsSnapshot GetTruncatedSnapshot(DateTime start, DateTime end)
		{
			return TradeHistoryHelper.GetTruncatedSnapshot(this, start, end);
		}
	}
}

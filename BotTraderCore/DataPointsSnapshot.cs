using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BotTraderCore
{
	public class DataPointsSnapshot
	{
		DataPoint lastDataPoint;
		DataPoint secondToLastDataPoint;
		DataPoint thirdToLastDataPoint;
		
		public DataPoint LastDataPoint => lastDataPoint;
		public DataPoint SecondToLastDataPoint => secondToLastDataPoint;
		public DataPoint ThirdToLastDataPoint => thirdToLastDataPoint;
		
		readonly IReadOnlyCollection<DataPoint> dataPoints;
		readonly IReadOnlyCollection<ChangeSummary> changeSummaries;

		public DataPointsSnapshot(List<DataPoint> dataPoints, DateTime start, DateTime end, decimal low, decimal high, List<ChangeSummary> changeSummaries, ChangeSummary activeChangeSummary, List<DataPoint> buySignals)
		{
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
			List<ChangeSummary> localChangeSummaries = changeSummaries.ToList();
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
			decimal variance = sum / count;
			return (decimal)Math.Sqrt((double)variance);
		}
	}
}

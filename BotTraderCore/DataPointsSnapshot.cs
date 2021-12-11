using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BotTraderCore
{
	public class DataPointsSnapshot
	{
		readonly IReadOnlyCollection<DataPoint> dataPoints;
		readonly IReadOnlyCollection<ChangeSummary> changeSummaries;

		public DataPointsSnapshot(List<DataPoint> dataPoints, DateTime start, DateTime end, decimal low, decimal high, List<ChangeSummary> changeSummaries, ChangeSummary activeChangeSummary)
		{
			High = high;
			Low = low;
			End = end;
			Start = start;
			this.dataPoints = new ReadOnlyCollection<DataPoint>(dataPoints.ToList());
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

		decimal average = decimal.MinValue;
		decimal standardDeviation = decimal.MinValue;

		public decimal GetAveragePrice()
		{
			if (average == decimal.MinValue)
			{
				decimal sum = 0;
				decimal count = 0;
				foreach (DataPoint stockDataPoint in DataPoints)
				{
					sum += stockDataPoint.Tick.LastTradePrice * stockDataPoint.Weight;
					count += stockDataPoint.Weight;
				}
				average = sum / count;
			}
			return average;
		}

		public decimal GetStandardDeviation()
		{
			if (standardDeviation == decimal.MinValue)
			{
				decimal sum = 0;
				decimal count = 0;
				decimal averagePrice = GetAveragePrice();
				foreach (DataPoint stockDataPoint in DataPoints)
				{
					decimal deviation = stockDataPoint.Tick.LastTradePrice - averagePrice;
					decimal deviationSquared = deviation * deviation;
					sum += deviationSquared * stockDataPoint.Weight;
					count += stockDataPoint.Weight;
				}
				decimal variance = sum / count;
				standardDeviation = (decimal)Math.Sqrt((double)variance);
			}
			return standardDeviation;
		}
	}
}

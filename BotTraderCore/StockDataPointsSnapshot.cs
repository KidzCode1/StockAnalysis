using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTraderCore
{
	public class StockDataPointsSnapshot
	{
		List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();
		List<ChangeSummary> changeSummaries = new List<ChangeSummary>();

		public StockDataPointsSnapshot(List<StockDataPoint> dataPoints, DateTime start, DateTime end, decimal low, decimal high, List<ChangeSummary> changeSummaries, ChangeSummary activeChangeSummary)
		{
			High = high;
			Low = low;
			End = end;
			Start = start;
			stockDataPoints.AddRange(dataPoints);
			this.changeSummaries.AddRange(changeSummaries);

			if (activeChangeSummary != null)
				this.changeSummaries.Add(activeChangeSummary);
		}

		public List<StockDataPoint> StockDataPoints { get => stockDataPoints; set => stockDataPoints = value; }
		public List<ChangeSummary> ChangeSummaries { get => changeSummaries; set => changeSummaries = value; }

		
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public decimal Low { get; set; }
		public decimal High { get; set; }

	}
}

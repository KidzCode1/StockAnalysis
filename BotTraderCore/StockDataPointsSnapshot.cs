using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BotTraderCore
{
	public class StockDataPointsSnapshot
	{
		IReadOnlyCollection<StockDataPoint> dataPoints;
		IReadOnlyCollection<ChangeSummary> changeSummaries;

		public StockDataPointsSnapshot(List<StockDataPoint> dataPoints, DateTime start, DateTime end, decimal low, decimal high, List<ChangeSummary> changeSummaries, ChangeSummary activeChangeSummary)
		{
			High = high;
			Low = low;
			End = end;
			Start = start;
			this.dataPoints = new ReadOnlyCollection<StockDataPoint>(dataPoints.ToList());
			List<ChangeSummary> localChangeSummaries = changeSummaries.ToList();
			if (activeChangeSummary != null)
				localChangeSummaries.Add(activeChangeSummary);
			this.changeSummaries = new ReadOnlyCollection<ChangeSummary>(localChangeSummaries);
		}

		public IReadOnlyCollection<StockDataPoint> DataPoints { get => dataPoints; }
		public IReadOnlyCollection<ChangeSummary> ChangeSummaries { get => changeSummaries; }

		
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public decimal Low { get; set; }
		public decimal High { get; set; }
	}
}

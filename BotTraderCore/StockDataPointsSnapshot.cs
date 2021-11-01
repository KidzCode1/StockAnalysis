using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTraderCore
{
	public class StockDataPointsSnapshot
	{
		List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();
		public StockDataPointsSnapshot(List<StockDataPoint> stockDataPoints, DateTime start, DateTime end, decimal low, decimal high)
		{
			High = high;
			Low = low;
			End = end;
			Start = start;
			this.stockDataPoints.AddRange(stockDataPoints);
		}

		public List<StockDataPoint> StockDataPoints { get => stockDataPoints; set => stockDataPoints = value; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public decimal Low { get; set; }
		public decimal High { get; set; }

	}
}

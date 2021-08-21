using System;
using System.Collections.Generic;
using System.Linq;

namespace BotTraderCore
{
	public class TickRange
	{
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public StockDataPoint High { get; set; }
		public StockDataPoint Low { get; set; }

		/// <summary>
		/// The value before the start of the selected range. Could be null!!!
		/// </summary>
		public StockDataPoint ValueBeforeRangeStarts { get; set; }

		public List<StockDataPoint> DataPoints { get; set; }

		public TickRange()
		{

		}
		public void CalculateLowAndHigh()
		{
			StockDataPoint lowestSoFar = null;
			StockDataPoint highestSoFar = null;

			foreach (StockDataPoint stockDataPoint in DataPoints)
			{
				if (lowestSoFar == null)
					lowestSoFar = stockDataPoint;
				else if (stockDataPoint.Tick.LastTradePrice < lowestSoFar.Tick.LastTradePrice)
					lowestSoFar = stockDataPoint;

				if (highestSoFar == null)
					highestSoFar = stockDataPoint;
				else if (stockDataPoint.Tick.LastTradePrice > highestSoFar.Tick.LastTradePrice)
					highestSoFar = stockDataPoint;
			}

			Low = lowestSoFar;
			High = highestSoFar;
		}
	}
}

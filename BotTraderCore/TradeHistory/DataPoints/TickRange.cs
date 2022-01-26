using System;
using System.Collections.Generic;
using System.Linq;

namespace BotTraderCore
{
	public class TickRange
	{
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public DataPoint High { get; set; }
		public DataPoint Low { get; set; }

		/// <summary>
		/// The value before the start of the selected range. Could be null!!!
		/// </summary>
		public DataPoint ValueBeforeRangeStarts { get; set; }

		public List<DataPoint> DataPoints { get; set; }

		public TickRange()
		{

		}
		public void CalculateLowAndHigh()
		{
			DataPoint lowestSoFar = null;
			DataPoint highestSoFar = null;

			foreach (DataPoint stockDataPoint in DataPoints)
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

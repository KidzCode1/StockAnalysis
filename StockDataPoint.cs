using Bittrex.Net.Objects;
using System;
using System.Linq;

namespace StockAnalysis
{
	public class StockDataPoint
	{
		public CustomTick Tick { get; set; }
		public DateTime Time { get; set; }
		public int Weight { get; set; } = 1;
		public StockDataPoint(CustomTick tick)
		{
			Tick = tick;
			Time = DateTime.Now;
		}
		public StockDataPoint()
		{

		}
	}
}

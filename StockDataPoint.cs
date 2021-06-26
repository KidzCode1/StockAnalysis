using Bittrex.Net.Objects;
using System;
using System.Linq;

namespace StockAnalysis
{
	public class StockDataPoint
	{
		public BittrexTick Tick { get; set; }
		public DateTime Time { get; set; }
		public int Weight { get; set; } = 1;
		public StockDataPoint(BittrexTick tick)
		{
			Tick = tick;
			Time = DateTime.Now;
		}
		public StockDataPoint()
		{

		}
	}
}

using Bittrex.Net.Objects;
using System;
using System.Linq;

namespace StockAnalysis
{
	public class StockDataPoint
	{
		public BittrexTick Tick { get; set; }
		public DateTime Time { get; set; }
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

using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTraderCore
{
	public static class TradeHistoryHelper
	{
		public static TickRange GetTickRange(ITradeHistory tradeHistory)
		{
			TickRange tickRange = new TickRange() { Start = tradeHistory.Start, End = tradeHistory.End, DataPoints = new List<DataPoint>() };
			tickRange.DataPoints.AddRange(tradeHistory.StockDataPoints);
			if (tradeHistory.StockDataPoints.Count > 0)
				tickRange.ValueBeforeRangeStarts = tradeHistory.StockDataPoints[0];
			tickRange.CalculateLowAndHigh();
			return tickRange;
		}
	}
}

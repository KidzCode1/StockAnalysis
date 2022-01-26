using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTraderCore
{
	public class AvailableTrades
	{
		public string Symbol { get; set; }

		readonly Dictionary<string, string> channelMap = new Dictionary<string, string>();

		public AvailableTrades(string symbol)
		{
			Symbol = symbol;
		}

		public void AddBinanceTradingChannel(string toSymbol, string binanceChannelName)
		{
			channelMap[toSymbol] = binanceChannelName;
		}

		public bool CanTradeTo(string symbol)
		{
			return channelMap.ContainsKey(symbol);
		}

		public string GetChannelName(string symbol)
		{
			return channelMap[symbol];
		}
	}
}

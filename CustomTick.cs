using Bittrex.Net.Objects;
using System;
using System.Linq;

namespace StockAnalysis
{
	public class CustomTick
	{
		//
		// Summary:
		//     Symbol of the ticker
		public string Symbol { get; set; } = "";
		//
		// Summary:
		//     The price of the last trade
		public decimal LastTradeRate { get; set; }
		//
		// Summary:
		//     The highest bid price
		public decimal BidRate { get; set; }
		//
		// Summary:
		//     The lowest ask price
		public decimal AskRate { get; set; }

		// Constructors allocate (set aside or provide) memory for an **instance** of a Class.
		// Constructors instantiate. Create **new** things.
		public CustomTick(BittrexTick bittrexTick)  // Happens here.
		{ 
			// At this point, the instance is created, and every field & property have their default values.
			// Initialization...
			Symbol = bittrexTick.Symbol;
			LastTradeRate = bittrexTick.LastTradeRate;
			BidRate = bittrexTick.BidRate;
			AskRate = bittrexTick.AskRate;
		}
	}
}

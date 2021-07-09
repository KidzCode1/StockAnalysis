using Bittrex.Net.Objects;
using Newtonsoft.Json;
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
		[JsonProperty(PropertyName ="Price")]
		public decimal LastTradePrice { get; set; }
		[JsonProperty(PropertyName = "Bid")]
		public decimal HighestBidPrice { get; set; }
		[JsonProperty(PropertyName = "Ask")]
		public decimal LowestAskPrice { get; set; }

		// Constructors allocate (set aside or provide) memory for an **instance** of a Class.
		// Constructors instantiate. Create **new** things.
		public CustomTick(BittrexTick bittrexTick)  // Happens here.
		{ 
			// At this point, the instance is created, and every field & property have their default values.
			// Initialization...
			Symbol = bittrexTick.Symbol;
			LastTradePrice = bittrexTick.LastTradeRate;
			HighestBidPrice = bittrexTick.BidRate;
			LowestAskPrice = bittrexTick.AskRate;
		}

		public CustomTick()
		{
		}

		public static bool operator ==(CustomTick left, CustomTick right)
		{
			if ((object)left == null)
				return (object)right == null;
			else
				return left.Equals(right);
		}

		public static bool operator !=(CustomTick left, CustomTick right)
		{
			return !(left == right);
		}
		public override int GetHashCode()
		{
			// TODO: Modify this hash code calculation, if desired.
			return base.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			if (obj is CustomTick)
				return Equals((CustomTick)obj);
			else if (obj is CustomTick)
				return Equals((CustomTick)obj);
			else
				return base.Equals(obj);
		}
		
		public bool Equals(CustomTick obj)
		{
			// For structs, you can remove the following check:
			if (ReferenceEquals(obj, null))
				return false;
			if (HighestBidPrice != obj.HighestBidPrice)
				return false;
			if (LowestAskPrice != obj.LowestAskPrice)
				return false;
			if (LastTradePrice != obj.LastTradePrice)
				return false;
			if (Symbol != obj.Symbol)
				return false;

			return true;
		}
	}
}

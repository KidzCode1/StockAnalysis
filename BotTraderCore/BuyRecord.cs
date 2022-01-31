using System;
using System.Linq;

namespace BotTraderCore
{
	public class BuyRecord
	{
		public string SymbolPurchased { get; set; }
		public string QuoteCurrency { get; set; }
		public decimal AmountPurchased { get; set; }
		public BuyRecord()
		{

		}
	}
}

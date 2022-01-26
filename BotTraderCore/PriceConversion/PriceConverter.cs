using System;
using System.Linq;

namespace BotTraderCore
{
	public static class PriceConverter
	{
		public static string GetUsdStr(decimal amount, string symbolPair, int decimals = 9)
		{
			decimal adjustedAmount = GetPriceUsd(symbolPair) * amount;
			if (adjustedAmount >= 1000)
				decimals = 0;
			else if (adjustedAmount >= 10)
				decimals = 2;
			else if (adjustedAmount >= 1)
				decimals = 3;
			else if (adjustedAmount >= 0.1m)
				decimals = 4;
			else if (adjustedAmount >= 0.01m)
				decimals = 5;
			else if (adjustedAmount >= 0.001m)
				decimals = 6;
			else if (adjustedAmount >= 0.0001m)
				decimals = 7;

			return "$" + adjustedAmount.GetNum(decimals);
		}

		public static void RegisterPriceConverter(IPriceConverter converter)
		{
			priceConverter = converter;
		}

		public static decimal GetPriceUsd(string symbol)
		{
			return priceConverter.GetPriceUsd(symbol);
		}

		static IPriceConverter priceConverter { get; set; }
	}
}

using System;
using System.Linq;

namespace BotTraderCore
{
	public static class PriceConverter
	{
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

using System;
using System.Linq;

namespace BotTraderCore
{
	public interface IPriceConverter
	{
		decimal GetPriceUsd(string symbolName);
	}
}

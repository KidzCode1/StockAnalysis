using System;
using System.Linq;
using Bittrex.Net.Objects;

namespace BotTraderCore
{
	/// <summary>
	/// A class for generating random stock tick data with characteristics similar to existing trade currencies.
	/// </summary>
	public class RandomTickGenerator
	{
		readonly Random random = new Random();

		public string Symbol { get; set; } = "test";
		public decimal TradePrice { get; set; }
		public int MoveDuration { get; set; }
		public int Direction { get; set; }
		public RandomTickGenerator()
		{
			Reset();
		}

		public void Reset()
		{
			TradePrice = random.Next(100) + 5;
			MoveDuration = random.Next(10) * 5;
			Direction = 1;
		}

		public void AddPoint()
		{
			MoveDuration--;
			if (MoveDuration <= 0)
			{
				if (random.Next(100) < 50)
					Direction = -1;
				else
					Direction = 1;

				MoveDuration = random.Next(10) * 5;
			}

			if (random.Next(100) < 15)
				Direction = -Direction;
			else if (random.Next(100) < 12)
				Direction = 0;

			if (TradePrice < 5 && Direction == -1)
				Direction = 1;

			int localDirection = Direction;
			if (random.Next(100) < 10)
				localDirection = -localDirection;

			decimal distanceToMove = TradePrice * (decimal)random.NextDouble() * 3 / 100 * localDirection;

			TradePrice += distanceToMove;
		}

		public BittrexTick GetNewBittrexTick()
		{
			AddPoint();
			return new BittrexTick() { LastTradeRate = TradePrice, Symbol = Symbol, AskRate = TradePrice + 1, BidRate = TradePrice - 1 };
		}

		public CustomTick GetNewCustomTick()
		{
			AddPoint();
			return new CustomTick() { LastTradePrice = TradePrice, Symbol = Symbol, LowestAskPrice = TradePrice + 1, HighestBidPrice = TradePrice - 1 };
		}
	}
}

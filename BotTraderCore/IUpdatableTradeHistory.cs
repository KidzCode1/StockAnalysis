using System;
using System.Linq;

namespace BotTraderCore
{
	public interface IUpdatableTradeHistory : ITradeHistory
	{
		void AddStockPositionWithUpdate(CustomTick data, DateTime? timeOverride);
		void CalculateBounds();
		void Clear();
		void SetTickRange(TickRange tickRange);
		void SetQuoteCurrencyToUsdConversion(decimal amount);

		void TestAddDataPoints(params decimal[] args);
		void TestAddPriceSequence(params decimal[] args);
		void TestAddRandomPriceSequence(int count);
		void TestAddStockDataPoint(decimal price, double offsetSeconds);
		void TestStretchTimeSpanTo(TimeSpan targetTimeSpan);

		bool ChangedSinceLastDataDensityQuery { get; set; }
		int MaxDataPointsToKeep { get; set; }

		TickRange GetPointsAroundTime(DateTime timeCenterPoint, int timeSpanSeconds);
		TickRange GetPointsInRange(DateTime start, DateTime end);
	}
}

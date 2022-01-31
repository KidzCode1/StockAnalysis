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

		public static void GetFirstLastLowHigh(List<DataPoint> stockDataPoints, out DataPoint first, out DataPoint last, out DataPoint low, out DataPoint high)
		{
			decimal lowestSoFar = decimal.MaxValue;
			decimal highestSoFar = decimal.MinValue;
			low = null;
			high = null;
			first = null;
			last = null;
			if (stockDataPoints == null || stockDataPoints.Count == 0)
				return;
			first = stockDataPoints[0];
			last = stockDataPoints[stockDataPoints.Count - 1];
			foreach (DataPoint dataPoint in stockDataPoints)
			{
				decimal lastTradePrice = dataPoint.Tick.LastTradePrice;

				if (lastTradePrice < lowestSoFar)
				{
					low = dataPoint;
					lowestSoFar = lastTradePrice;
				}

				if (lastTradePrice > highestSoFar)
				{
					high = dataPoint;
					highestSoFar = lastTradePrice;
				}
			}
		}

		public static List<DataPoint> GetPointsInRange(List<DataPoint> dataPoints, DateTime start, DateTime end)
		{
			return dataPoints.Where(x => x.Time >= start && x.Time <= end).ToList();
		}

		public static DataPointsSnapshot GetTruncatedSnapshot(ITradeHistory tradeHistory, DateTime start, DateTime end)
		{
			List<DataPoint> pointsInRange = GetPointsInRange(tradeHistory.StockDataPoints, start, end);
			DataPoint high;
			DataPoint low;
			DataPoint first;
			DataPoint last;
			GetFirstLastLowHigh(pointsInRange, out first, out last, out low, out high);
			DataPointsSnapshot dataPointsSnapshot = new DataPointsSnapshot(pointsInRange, first.Time, last.Time, low.Tick.LastTradePrice, high.Tick.LastTradePrice, null, null, tradeHistory.BuySignals, tradeHistory.SymbolPair, tradeHistory.QuoteCurrencyToUsdConversion, tradeHistory.AveragePriceAtBuySignal, tradeHistory.StandardDeviationAtBuySignal);

			if (tradeHistory.SellSignals != null)
				dataPointsSnapshot.SellSignals = new List<DataPoint>(tradeHistory.SellSignals);
			else
				dataPointsSnapshot.SellSignals = new List<DataPoint>();

			dataPointsSnapshot.BuySignalAction = tradeHistory.BuySignalAction;

			return dataPointsSnapshot;
		}
	}
}

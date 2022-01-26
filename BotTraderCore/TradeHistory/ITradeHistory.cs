using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTraderCore
{
	public interface ITradeHistory
	{
		List<DataPoint> GetDataPointsAcrossSegments(int segmentCount, bool addChangeSummaries = false, bool distributeDensityAcrossFlats = false);
		
		DataPointsSnapshot GetSnapshot();
		DataPointsSnapshot GetTruncatedSnapshot(DateTime start, DateTime end);

		void SaveTickRange(string fullPathToFile);
		void GetFirstLastLowHigh(out DataPoint first, out DataPoint last, out DataPoint low, out DataPoint high, DateTime start, DateTime end);

		decimal AveragePrice { get; }
		decimal StandardDeviation { get; }

		List<DataPoint> BuySignals { get; set; }
		List<DataPoint> SellSignals { get; set; }

		/// <summary>
		/// The average price at the time the first buy signal arrived.
		/// </summary>
		decimal AveragePriceAtBuySignal { get; set; }

		/// <summary>
		/// The StandardDeviation at the time the first buy signal arrived.
		/// </summary>
		decimal StandardDeviationAtBuySignal { get; set; }
		
		int DataPointCount { get; }
		
		DateTime Start { get; }
		DateTime End { get; }
		decimal High { get; }
		decimal Low { get; }
		
		decimal PercentInView { get; }
		decimal QuoteCurrencyToUsdConversion { get; }
		TimeSpan SpanAcross { get; }
		List<DataPoint> StockDataPoints { get; }
		string SymbolPair { get; set; }
		decimal ValueSpan { get; }
	}
}

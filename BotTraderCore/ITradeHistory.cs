using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTraderCore
{
	public interface ITradeHistory
	{
		List<DataPoint> GetDataPointsAcrossSegments(int segmentCount, bool addChangeSummaries = false, bool distributeDensityAcrossFlats = false);
		
		DataPointsSnapshot GetSnapshot();
		
		void SaveTickRange(string fullPathToFile);

		decimal AveragePrice { get; }
		List<DataPoint> BuySignals { get; set; }
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

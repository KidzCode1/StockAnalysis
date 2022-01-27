using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTraderCore
{
	public interface ITradeHistory
	{
		/// <summary>
		/// Gets a list of StockDataPoints based on the specified segmentCount. So if I have 11 data points and I  specify
		/// five segments, this function will return six points. The first five points will contain averages of data at
		/// indexes 0 & 1, 2 & 3, 4 & 5, 6 & 7, and 8 & 9. The last point will always contain the last point in this
		/// ChartTranslator (no averaging or repeating).
		/// </summary>
		/// <param name="segmentCount">
		/// The number of segments to break the existing data into. Data points  will be repeated if more segments span a
		/// range than actual data points (in the ChartTranslator). Similarly, data points will be averaged if more than one
		/// appear in a single segment.
		/// </param>
		/// <returns>Returns the calculated StockDataPoints. The count will always be segmentCount plus one.</returns>
		List<DataPoint> GetDataPointsAcrossSegments(int segmentCount, bool addChangeSummaries = false, bool distributeDensityAcrossFlats = false);
		
		/// <summary>
		/// Gets a snapshot of this trade history.
		/// </summary>
		DataPointsSnapshot GetSnapshot();

		/// <summary>
		/// Gets a truncated snapshot of this trade history.
		/// </summary>
		/// <param name="start">The start of the snapshot to return.</param>
		/// <param name="end">The end of the snapshot to return.</param>
		DataPointsSnapshot GetTruncatedSnapshot(DateTime start, DateTime end);

		/// <summary>
		/// Saves a TickRange (as JSON) from this trade history.
		/// </summary>
		/// <param name="fullPathToFile"></param>
		void SaveTickRange(string fullPathToFile);

		/// <summary>
		/// Gets the bounds of a specified time range in this trade history, in terms of time and currency.
		/// </summary>
		/// <param name="first">The first DataPoint in the trade history.</param>
		/// <param name="last">The last DataPoint in the trade history.</param>
		/// <param name="low">The lowest DataPoint in the trade history.</param>
		/// <param name="high">The highest DataPoint in the trade history.</param>
		/// <param name="start">The start time (to scan) in the trade history.</param>
		/// <param name="end">The end time (to scan) in the trade history.</param>
		void GetFirstLastLowHigh(out DataPoint first, out DataPoint last, out DataPoint low, out DataPoint high, DateTime start, DateTime end);

		/// <summary>
		/// The average price of all points in this trade history.
		/// </summary>
		decimal AveragePrice { get; }

		/// <summary>
		/// The standard deviation of all points in this trade history.
		/// </summary>
		decimal StandardDeviation { get; }

		/// <summary>
		/// A list of DataPoints indicating where a buy signal was found.
		/// </summary>
		List<DataPoint> BuySignals { get; set; }

		/// <summary>
		/// A list of DataPoints indicating where a sell signal was found.
		/// </summary>
		List<DataPoint> SellSignals { get; set; }

		/// <summary>
		/// BuySignal event handlers can set this to true. If true, sell signal generators will scan the data in this trade history after any change, looking for sell signals.
		/// </summary>
		bool Traded { get; set; }

		/// <summary>
		/// The average price at the time the first buy signal arrived.
		/// </summary>
		decimal AveragePriceAtBuySignal { get; set; }

		/// <summary>
		/// The StandardDeviation at the time the first buy signal arrived.
		/// </summary>
		decimal StandardDeviationAtBuySignal { get; set; }
		
		/// <summary>
		/// The number of DataPoints in this trade history.
		/// </summary>
		int DataPointCount { get; }

		/// <summary>
		/// The start of this trade history.
		/// </summary>
		DateTime Start { get; }

		/// <summary>
		/// The end of this trade history.
		/// </summary>
		DateTime End { get; }

		/// <summary>
		/// The highest currency value in this trade history.
		/// </summary>
		decimal High { get; }

		/// <summary>
		/// The lowest currency value in this trade history.
		/// </summary>
		decimal Low { get; }

		/// <summary>
		/// The high-to-low quote currency amount in this trade history.
		/// </summary>
		decimal ValueSpan { get; }

		/// <summary>
		/// The percent of the highest value in this trade history represented by the entire trade history.
		/// This is an indicator of how much of the value we don't see.
		/// </summary>
		decimal PercentInView { get; }

		/// <summary>
		/// The quote currency to USD conversion rate. Multiply this by any quote currency values to get to USD.
		/// </summary>
		decimal QuoteCurrencyToUsdConversion { get; }

		/// <summary>
		/// The time span across the trade history (from the first to the last DataPoint).
		/// </summary>
		TimeSpan SpanAcross { get; }

		/// <summary>
		/// A list of all DataPoints in this trade history.
		/// </summary>
		List<DataPoint> StockDataPoints { get; }

		/// <summary>
		/// The symbol pair for this trade history.
		/// </summary>
		string SymbolPair { get; set; }
	}
}

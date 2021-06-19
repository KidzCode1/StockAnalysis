using Bittrex.Net.Objects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace StockAnalysis
{
	public class ChartTranslator
	{
		List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();

		public ChartTranslator(double chartWidthPixels, double chartHeightPixels)
		{
			this.chartWidthPixels = chartWidthPixels;
			this.chartHeightPixels = chartHeightPixels;
		}

		decimal high = 37000;
		decimal low = 35000;
		readonly double chartHeightPixels;
		readonly double chartWidthPixels;
		DateTime start;
		DateTime end;

		public List<StockDataPoint> StockDataPoints { get => stockDataPoints; }

		void CalculateBounds()
		{
			high = 0;
			low = decimal.MaxValue;
			foreach (StockDataPoint stockDataPoint in stockDataPoints)
			{
				if (stockDataPoint.Tick.LastTradeRate < low)
					low = stockDataPoint.Tick.LastTradeRate;
				if (stockDataPoint.Tick.LastTradeRate > high)
					high = stockDataPoint.Tick.LastTradeRate;
			}

			if (high == low)  // Only one data point?
			{
				// Offset both for the charting:
				high += 1;
				low -= 1;
				if (low < 0)
					low = 0;
			}

			if (stockDataPoints.Count == 0)
			{
				start = DateTime.Now;
				end = DateTime.Now;
				return;
			}

			start = stockDataPoints[0].Time;
			end = stockDataPoints[stockDataPoints.Count - 1].Time;
		}

		public double GetStockPositionX(DateTime time)
		{
			TimeSpan distanceToPoint = time - start;
			TimeSpan totalSpanAcross = end - start;
			double totalSecondsAcross = totalSpanAcross.TotalSeconds;
			double secondsToPoint = distanceToPoint.TotalSeconds;
			double percentAcross = secondsToPoint / totalSecondsAcross;

			return percentAcross * chartWidthPixels;
		}

		public DateTime GetTimeFromX(double xPos)
		{
			double percentAcross = xPos / chartWidthPixels;
			TimeSpan totalSpanAcross = end - start;
			double timeAcrossSeconds = percentAcross * totalSpanAcross.TotalSeconds;
			return start + TimeSpan.FromSeconds(timeAcrossSeconds);
		}

		public double GetStockPositionY(decimal lastTradeRate)
		{
			// Make sure the text is changed on the UI thread!
			decimal amountAboveBottom = lastTradeRate - low;
			decimal chartHeightDollars = high - low;

			// ![](99A33C7E4008781D520BA988900A3B50.png;;;0.01945,0.01945)

			decimal percentOfChartHeightFromBottom = amountAboveBottom / chartHeightDollars;
			double distanceFromBottomPixels = (double)percentOfChartHeightFromBottom * chartHeightPixels;
			double distanceFromTopPixels = chartHeightPixels - distanceFromBottomPixels;
			return distanceFromTopPixels;
		}

		public StockDataPoint AddStockPosition(BittrexTick data)
		{
			StockDataPoint stockDataPoint = new StockDataPoint(data);
			stockDataPoints.Add(stockDataPoint);
			CalculateBounds();
			return stockDataPoint;
		}

		public StockDataPoint GetNearestPoint(DateTime time)
		{
			double closestSpanSoFar = double.MaxValue;
			StockDataPoint closestDataPoint = null;
			foreach (StockDataPoint stockDataPoint in StockDataPoints)
			{
				double distanceInTime = Math.Abs((stockDataPoint.Time - time).TotalSeconds);
				if (distanceInTime < closestSpanSoFar)
				{
					closestSpanSoFar = distanceInTime;
					closestDataPoint = stockDataPoint;
				}
			}
			return closestDataPoint;
		}

	}
}

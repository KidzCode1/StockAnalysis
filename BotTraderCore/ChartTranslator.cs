using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace BotTraderCore
{
	public class ChartTranslator
	{
		public TradeHistory TradeHistory { get; set; } = new TradeHistory();

		public ChartTranslator()
		{
		}

		public DateTime GetTime(double x, double chartWidthPixels)
		{
			if (x < 0)
				return TradeHistory.Start;
			TimeSpan totalSpanAcross = TradeHistory.End - TradeHistory.Start;
			double totalSecondsAcross = totalSpanAcross.TotalSeconds;
			double percentAcross = x / chartWidthPixels;
			if (percentAcross > 1)
				return TradeHistory.End;
			double secondsToPoint = percentAcross * totalSecondsAcross;

			return TradeHistory.Start + TimeSpan.FromSeconds(secondsToPoint);
		}

		public decimal GetPrice(double yPos, double chartHeightPixels)
		{
			if (yPos < 0)
				return TradeHistory.High;

			double percentUp = yPos / chartHeightPixels;

			if (percentUp > 1)
				return TradeHistory.Low;

			decimal totalPriceSpan = TradeHistory.High - TradeHistory.Low;
			decimal totalPriceDelta = (decimal)percentUp * totalPriceSpan;
			return TradeHistory.High - totalPriceDelta;
		}

		public double GetStockPositionX(DateTime time, double chartWidthPixels)
		{
			if (time == DateTime.MinValue)
				return 0d;

			if (time == DateTime.MaxValue)
				return chartWidthPixels;

			TimeSpan distanceToPoint = time - TradeHistory.Start;
			TimeSpan totalSpanAcross = TradeHistory.End - TradeHistory.Start;
			double totalSecondsAcross = totalSpanAcross.TotalSeconds;
			if (totalSecondsAcross == 0)
				return 0;
			double secondsToPoint = distanceToPoint.TotalSeconds;
			double percentAcross = secondsToPoint / totalSecondsAcross;

			return percentAcross * chartWidthPixels;
		}

		public double GetStockPositionY(decimal price, double chartHeightPixels)
		{
			if (price == decimal.MinValue)
				return chartHeightPixels;

			if (price == decimal.MaxValue)
				return 0;
			// Make sure the text is changed on the UI thread!
			decimal amountAboveBottom = price - TradeHistory.Low;
			decimal chartHeightDollars = TradeHistory.High - TradeHistory.Low;

			// ![](99A33C7E4008781D520BA988900A3B50.png;;;0.01110,0.01110)

			decimal percentOfChartHeightFromBottom = amountAboveBottom / chartHeightDollars;
			double distanceFromBottomPixels = (double)percentOfChartHeightFromBottom * chartHeightPixels;
			double distanceFromTopPixels = chartHeightPixels - distanceFromBottomPixels;
			return distanceFromTopPixels;
		}

		public List<PointXY> GetMovingAverages(int timeSpanSeconds, double chartWidthPixels, double chartHeightPixels)
		{
			TimeSpan timeAcross = TradeHistory.SpanAcross;
			double totalSecondsAcross = timeAcross.TotalSeconds;
			const int numberOfDataPoints = 200;

			List<PointXY> points = new List<PointXY>();

			if (TradeHistory.Start == DateTime.MinValue)
				return points;

			double secondsPerDataPoint = totalSecondsAcross / numberOfDataPoints;
			for (int i = 0; i < numberOfDataPoints; i++)
			{
				DateTime timeAtDataPoint = TradeHistory.Start + TimeSpan.FromSeconds(i * secondsPerDataPoint);
				TickRange tickRange = TradeHistory.GetPointsAroundTime(timeAtDataPoint, timeSpanSeconds);
				decimal averagePrice = TradeHistory.GetAveragePrice(tickRange.DataPoints);
				if (averagePrice == decimal.MinValue)
					continue;
				double stockPositionX = GetStockPositionX(timeAtDataPoint, chartWidthPixels);
				double stockPositionY = GetStockPositionY(averagePrice, chartHeightPixels);
				PointXY point = new PointXY(stockPositionX, stockPositionY);
				points.Add(point);
			}
			return points;
		}
	}
}

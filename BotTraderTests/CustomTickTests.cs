using Bittrex.Net.Objects;
using BotTraderCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace BotTraderTests
{
	[TestClass]
	public class SerializationTests
	{
		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}
		[TestMethod]
		public void TestSaveLoad()
		{
			string fivePointsFile = Folders.GetTestFilePath("FivePoints.json");
			string testSaveFile = Folders.GetTestFilePath("FivePointsTestSave.json");
			if (File.Exists(testSaveFile))
				File.Delete(testSaveFile);
			List<StockDataPoint> loadedPoints = StockDataPoint.Load(fivePointsFile);
			Assert.AreEqual(5, loadedPoints.Count);

			loadedPoints.Save(testSaveFile);
			List<StockDataPoint> newLoadedPoints = StockDataPoint.Load(testSaveFile);
			Assert.IsTrue(newLoadedPoints.Matches(loadedPoints));
			loadedPoints[0].Time = DateTime.Now;
			Assert.IsFalse(newLoadedPoints.Matches(loadedPoints));
			newLoadedPoints = StockDataPoint.Load(testSaveFile);
			newLoadedPoints[0].Weight++;
			Assert.IsFalse(newLoadedPoints.Matches(loadedPoints));

			newLoadedPoints = StockDataPoint.Load(testSaveFile);
			newLoadedPoints[2].Tick.HighestBidPrice += 0.0000001m;
			Assert.IsFalse(newLoadedPoints.Matches(loadedPoints));

			newLoadedPoints = StockDataPoint.Load(testSaveFile);
			newLoadedPoints[3].Tick.LastTradePrice += 0.0000001m;
			Assert.IsFalse(newLoadedPoints.Matches(loadedPoints));

			newLoadedPoints = StockDataPoint.Load(testSaveFile);
			newLoadedPoints[4].Tick.LowestAskPrice += 0.0000001m;
			Assert.IsFalse(newLoadedPoints.Matches(loadedPoints));

			newLoadedPoints = StockDataPoint.Load(testSaveFile);
			newLoadedPoints[1].Tick.Symbol += ".";
			Assert.IsFalse(newLoadedPoints.Matches(loadedPoints));
		}
	}

	[TestClass]
	public class CustomTickTests
	{
		[TestMethod]
		public void TestMethod1()
		{
			BittrexTick bittrexTick = new BittrexTick();
			bittrexTick.AskRate = 10;
			bittrexTick.BidRate = 11;
			bittrexTick.LastTradeRate = 10.5m;
			bittrexTick.Symbol = "BTC-USDT";
			CustomTick customTick = new CustomTick(bittrexTick);
			Assert.AreEqual(10, customTick.LowestAskPrice);
			Assert.AreEqual(11, customTick.HighestBidPrice);
			Assert.AreEqual(10.5m, customTick.LastTradePrice);
			Assert.AreEqual("BTC-USDT", customTick.Symbol);
		}
	}
}

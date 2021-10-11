using BotTraderCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BotTraderTests
{
	[TestClass]
	public class GeneratedTests
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
		
		//`![](d4c5ed17-22d1-4964-b6a1-f071c676f5c4.png;;;0.03551,0.03551)
[TestMethod]
public void Test1()
{
	TickRange range = TestData.Load("249e141e-da86-4f46-b7db-4721d77441b3.json");
	DateTime var0 = DateTime.Parse("2021 Aug 27 10:48:18.8574523");
	decimal var1 = decimal.Parse("48222.62908065651064144105248");
}

		[TestMethod]
		public void TestRightGreaterThanLeft()
		{
			TickRange range = TestData.Load("c4585917-4953-440d-a33c-d1cd26b73cbd.json");
			DateTime Right = DateTime.Parse("2021 Aug 22 09:34:04.2064045");
			DateTime Left = DateTime.Parse("2021 Aug 22 09:29:57.4714045");
			Assert.IsTrue(Right > Left);
			Assert.IsTrue(range.High.Tick.LastTradePrice > range.Low.Tick.LastTradePrice);
		}
	}
}

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

		[TestMethod]

		public void Test4()
		{
			TickRange range = DataHelper.Load("9ae5f99d-5211-4c75-9a73-7df61a832ff1.json");
			// 
		}
		[TestMethod]
		public void TestRightGreaterThanLeft()
		{
			TickRange range = DataHelper.Load("c4585917-4953-440d-a33c-d1cd26b73cbd.json");
			DateTime Right = DateTime.Parse("2021 Aug 22 09:34:04.2064045");
			DateTime Left = DateTime.Parse("2021 Aug 22 09:29:57.4714045");
			Assert.IsTrue(Right > Left);
			Assert.IsTrue(range.High.Tick.LastTradePrice > range.Low.Tick.LastTradePrice);
		}
	}
}

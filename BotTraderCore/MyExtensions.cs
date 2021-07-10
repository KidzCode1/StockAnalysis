using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BotTraderCore
{
	public static class MyExtensions
	{
		public static void Save(this List<StockDataPoint> points, string fileName)
		{
			string serializeObject = JsonConvert.SerializeObject(points, Formatting.Indented);
			File.WriteAllText(fileName, serializeObject);
		}

		public static bool Matches(this List<StockDataPoint> range1, List<StockDataPoint> range2)
		{
			if (range1.Count != range2.Count)
				return false;

			for (int i = 0; i < range1.Count; i++)
				if (!range1[i].Equals(range2[i]))
					return false;

			return true;
		}
	}
}

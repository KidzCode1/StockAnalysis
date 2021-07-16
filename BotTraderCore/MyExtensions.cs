using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BotTraderCore
{
	public static class MyExtensions
	{
		/// <summary>
		/// Gets a string version of a number with thousands separator, decimal point, and no trailing zeros.
		/// </summary>
		/// <param name="value">The decimal to convert.</param>
		/// <param name="numDigits">The number of decimal points to keep in the conversion. Optional. Default is to keep all digits (29).</param>
		public static string GetNum(this decimal value, int numDigits = 29)
		{
			string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			string strDec = value.ToString("#,0." + new string('#', numDigits), CultureInfo.CurrentCulture);
			return strDec.Contains(sep) ? strDec.TrimEnd('0').TrimEnd(sep.ToCharArray()) : strDec;
		}

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

using Bittrex.Net.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BotTraderCore
{
	public class StockDataPoint
	{
		public CustomTick Tick { get; set; }
		public DateTime Time { get; set; }

		public int Weight { get; set; }
		public StockDataPoint(CustomTick tick)
		{
			Tick = tick;
			Time = DateTime.Now;
			Weight = 1;
		}
		public StockDataPoint()
		{
			Weight = 1;
		}

		public static bool operator ==(StockDataPoint left, StockDataPoint right)
		{
			if (ReferenceEquals(left, null))
				return ReferenceEquals(right, null);
			else
				return left.Equals(right);
		}
		public static bool operator !=(StockDataPoint left, StockDataPoint right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			// TODO: Modify this hash code calculation, if desired.
			return base.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is StockDataPoint)
				return Equals((StockDataPoint)obj);
			else
				return base.Equals(obj);
		}

		public bool Equals(StockDataPoint comparePoint)
		{
			// For structs, you can remove the following check:
			if (ReferenceEquals(comparePoint, null))
				return false;
			if (Time != comparePoint.Time)
				return false;

			if (Weight != comparePoint.Weight)
				return false;

			if (Tick != comparePoint.Tick)
				return false;

			return true;
		}

		public static List<StockDataPoint> Load(string fullPathToFile)
		{
			string readFromFileStr = File.ReadAllText(fullPathToFile);
			List<StockDataPoint> data = JsonConvert.DeserializeObject<List<StockDataPoint>>(readFromFileStr);
			return data;
		}
	}
}

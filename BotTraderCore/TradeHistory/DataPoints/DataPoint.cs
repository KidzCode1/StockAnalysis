using Bittrex.Net.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BotTraderCore
{
	public class DataPoint
	{
		public CustomTick Tick { get; set; }
		public DateTime Time { get; set; }

		public int Weight { get; set; }
		public DataPoint(CustomTick tick)
		{
			Tick = tick;
			Time = DateTime.Now;
			Weight = 1;
		}
		public DataPoint()
		{
			Weight = 1;
		}

		public static bool operator ==(DataPoint left, DataPoint right)
		{
			if (left is null)
				return right is null;
			else
				return left.Equals(right);
		}
		public static bool operator !=(DataPoint left, DataPoint right)
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
			if (obj is DataPoint stockDataPoint)
				return Equals(stockDataPoint);
			else
				return base.Equals(obj);
		}

		public bool Equals(DataPoint comparePoint)
		{
			// For structs, you can remove the following check:
			if (comparePoint is null)
				return false;
			if (Time != comparePoint.Time)
				return false;

			if (Weight != comparePoint.Weight)
				return false;

			if (Tick != comparePoint.Tick)
				return false;

			return true;
		}

		public static List<DataPoint> Load(string fullPathToFile)
		{
			string readFromFileStr = File.ReadAllText(fullPathToFile);
			List<DataPoint> data = JsonConvert.DeserializeObject<List<DataPoint>>(readFromFileStr);
			return data;
		}

		public DataPoint Clone(DateTime timeOverride)
		{
			return new DataPoint(Tick) { Time = timeOverride };
		}
	}
}

using System;
using System.Linq;

namespace TickGraphCore
{
	public class CustomAdornment
	{
		public string Key { get; set; } // Like iconTimePoint
		public decimal Price { get; set; }
		public DateTime Time { get; set; }
		public double Size { get; set; }
		public double LeftOffset { get; set; }
		public double TopOffset { get; set; }
		public CustomAdornment()
		{

		}
	}
}

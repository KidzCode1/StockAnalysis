using Bittrex.Net.Objects;
using System;
using System.Linq;

namespace BotTraderCore
{
	/// <summary>
	/// Stores a range summary (between two flat segments) that includes
	/// the high and low in that range. Used to render more accurate graphs
	/// with fewer data points.
	/// </summary>
	public class ChangeSummary
	{
		public DataPoint Start { get; set; }
		public DataPoint End { get; set; }
		public DataPoint High { get; set; }
		public DataPoint Low { get; set; }

		/// <summary>
		/// The number of points this summary represents.
		/// </summary>
		public int PointCount { get; set; }
		public ChangeSummary()
		{

		}
	}
}

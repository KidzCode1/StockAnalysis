using System;
using System.Linq;

namespace BotTraderCore
{
	public class PointXY
	{
		public double X { get; set; }
		public double Y { get; set; }
		public PointXY(double x, double y)
		{
			X = x;
			Y = y;
		}

		public PointXY()
		{

		}
	}
}

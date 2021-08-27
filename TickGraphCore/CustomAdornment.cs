using System;
using System.Linq;
using System.Windows.Media;

namespace TickGraphCore
{
	public class CustomAdornment
	{
		public string Key { get; set; } // Like iconTimePoint
		public decimal Price { get; set; }
		public DateTime Time { get; set; }
		public double Size { get; set; }
		public double IconLeftOffset { get; set; }
		public double IconTopOffset { get; set; }
		public string Name { get; set; }
		public double LabelLeftOffset { get; set; }
		public double LabelTopOffset { get; set; }
		public LabelAlignment LabelAlignment { get; set; }
		public DashedLineOption DashedLineOption { get; set; }
		public Color Color { get; set; }
		public CustomAdornment()
		{
			
		}
	}
}

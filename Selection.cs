using System;
using System.Linq;

namespace StockAnalysis
{
	public static class Selection
	{
		public static event EventHandler OnChange;
		public static event EventHandler OnChanging;
		public static void TriggerOnChange(object sender, EventArgs e)
		{
			OnChange?.Invoke(sender, e);
		}
		public static void TriggerOnChanging(object sender, EventArgs e)
		{
			OnChanging?.Invoke(sender, e);
		}
		public static DateTime Start
		{
			get
			{
				if (Cursor < Anchor)
					return Cursor;
				return Anchor;
			}
		}

		public static DateTime End
		{
			get
			{
				if (Cursor > Anchor)
					return Cursor;
				return Anchor;
			}
		}
		public static DateTime Cursor { get; set; }  // Where we ended the drag (or click).
		public static DateTime Anchor { get; set; }  // Where we started the drag (or click).
		public static SelectionModes Mode { get; set; }
		public static bool Exists => Cursor != Anchor;

		public static bool IsInBounds(double x, double y)
		{
			bool xIsInBounds = x >= 0 && x < 1900;
			bool yIsInBounds = y >= 0 && y < 700;
			return xIsInBounds && yIsInBounds;
		}
		public static void Changed()
		{
			TriggerOnChange(null, EventArgs.Empty);
		}
		public static void Changing()
		{
			TriggerOnChanging(null, EventArgs.Empty);
			// Actively changing it.
		}
	}
}

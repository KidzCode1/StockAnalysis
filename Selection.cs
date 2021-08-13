using System;
using System.Linq;

namespace StockAnalysis
{
	public class Selection
	{
		public event EventHandler OnChange;
		public event EventHandler OnChanging;
		public void TriggerOnChange(object sender, EventArgs e)
		{
			OnChange?.Invoke(sender, e);
		}
		public void TriggerOnChanging(object sender, EventArgs e)
		{
			OnChanging?.Invoke(sender, e);
		}
		public DateTime Start
		{
			get
			{
				if (Cursor < Anchor)
					return Cursor;
				return Anchor;
			}
		}

		public DateTime End
		{
			get
			{
				if (Cursor > Anchor)
					return Cursor;
				return Anchor;
			}
		}
		public DateTime Cursor { get; private set; }  // Where we ended the drag (or click).
		public DateTime Anchor { get; set; }  // Where we started the drag (or click).
		public SelectionModes Mode { get; set; }
		public bool Exists => Cursor != Anchor;

		public bool IsInBounds(double x, double y)
		{
			bool xIsInBounds = x >= 0 && x < 1900;
			bool yIsInBounds = y >= 0 && y < 700;
			return xIsInBounds && yIsInBounds;
		}

		public void Changed()
		{
			TriggerOnChange(null, EventArgs.Empty);
		}

		public void Changing()
		{
			TriggerOnChanging(null, EventArgs.Empty);
			// Actively changing it.
		}

		public void Set(DateTime start, DateTime end)
		{
			if (Cursor == start && Anchor == end)
				return;

			Cursor = start;
			Anchor = end;
			Changed();
		}

		public void SetChangingCursor(DateTime newTime)
		{
			Cursor = newTime;
			Changing();
		}

		public void SetFinalCursor(DateTime newTime)
		{
			Cursor = newTime;
			Mode = SelectionModes.Normal;
			Changed();
		}
	}
}

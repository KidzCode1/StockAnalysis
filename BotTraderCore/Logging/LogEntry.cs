using System;
using System.Linq;

namespace BotTraderCore
{
	public class LogEntry
	{
		public string Text { get; set; }

		public LogEntryKind Kind { get; set; }
		DateTime time;

		public DateTime Time => time;

		public LogEntry(string text, LogEntryKind kind = LogEntryKind.Info)
		{
			Kind = kind;
			Text = text;
			time = DateTime.Now;
		}
	}
}

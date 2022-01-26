using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BotTraderCore
{
	public static class Log
	{
		static List<LogEntry> entries = new List<LogEntry>();
		static int updateCounter;
		static LogEntry lastEntryAdded;

		public static List<LogEntry> Entries { get => entries; }

		public static event EventHandler<LogEntry> LogUpdated;

		static void NewEntryAdded(LogEntry entry)
		{
			LogUpdated?.Invoke(null, entry);
		}

		public static void Info(string message)
		{
			AddEntry(message, LogEntryKind.Info);
		}
		
		public static void Warning(string message)
		{
			AddEntry(message, LogEntryKind.Warning);
		}
		
		public static void Exception(Exception ex, [CallerMemberName] string member = "")
		{
			BeginUpdate();
			try
			{
				AddEntry($"{ex.GetType()} in {member}", LogEntryKind.Exception);
				AddEntry(ex.Message, LogEntryKind.Exception);
				AddEntry(ex.StackTrace, LogEntryKind.Exception);
			}
			finally
			{
				EndUpdate();
			}
		}
		
		public static void Debug(string message)
		{
			AddEntry(message, LogEntryKind.Debug);
		}

		public static void Error(string message)
		{
			AddEntry(message, LogEntryKind.Error);
		}
		
		private static void AddEntry(string message, LogEntryKind kind)
		{
			lastEntryAdded = new LogEntry(message, kind);
			entries.Add(lastEntryAdded);
			if (updateCounter == 0)
				NewEntryAdded(lastEntryAdded);
		}

		static void BeginUpdate()
		{
			updateCounter++;
		}

		static void EndUpdate()
		{
			updateCounter--;
			if (updateCounter == 0)
				NewEntryAdded(lastEntryAdded);
		}

	}
}

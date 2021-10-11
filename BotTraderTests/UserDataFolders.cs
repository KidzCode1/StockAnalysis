using BotTraderCore;
using System;
using System.IO;
using System.Linq;

namespace BotTraderTests
{
	public static class UserDataFolders
	{
		public static string GetFilePath(string fileName)
		{
			return Folders.Combine(applicationPath, fileName);
		}

		public static string GetTestFilePath(string fileName)
		{
			return Folders.Combine(testCaseFolder, fileName);
		}

		static string applicationPath;
		static string testCaseFolder;

		public static string TestCaseFolder { get => testCaseFolder; }
		public static string ApplicationPath { get => applicationPath; }

		static UserDataFolders()
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			applicationPath = Folders.Combine(folderPath, "StockTrader");
			testCaseFolder = Folders.Combine(applicationPath, "Test Cases");

			if (!Directory.Exists(applicationPath))
				Directory.CreateDirectory(applicationPath);

			if (!Directory.Exists(testCaseFolder))
				Directory.CreateDirectory(testCaseFolder);
		}
	}
}

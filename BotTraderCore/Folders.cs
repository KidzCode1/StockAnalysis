using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotTraderCore
{
	public static class Folders
	{
		static string Combine(string path1, string path2, string path3 = "")
		{
			if (string.IsNullOrWhiteSpace(path3))
				return Path.Combine(path1, path2);
			return Path.Combine(path1, path2, path3);
		}

		public static string GetFilePath(string fileName)
		{
			return Combine(applicationPath, fileName);
		}

		public static string GetTestFilePath(string fileName)
		{
			return Combine(testCaseFolder, fileName);
		}

		static string applicationPath;
		static string testCaseFolder;

		public static string TestCaseFolder { get => testCaseFolder; }
		public static string ApplicationPath { get => applicationPath; }

		static Folders()
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			applicationPath = Combine(folderPath, "StockTrader");
			testCaseFolder = Combine(applicationPath, "Test Cases");

			if (!Directory.Exists(applicationPath))
				Directory.CreateDirectory(applicationPath);

			if (!Directory.Exists(testCaseFolder))
				Directory.CreateDirectory(testCaseFolder);
		}
	}
}

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
		public static string Combine(string path1, string path2, string path3 = "")
		{
			if (string.IsNullOrWhiteSpace(path3))
				return Path.Combine(path1, path2);
			return Path.Combine(path1, path2, path3);
		}

		public static string GetProjectFolderName()
		{
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			if (baseDirectory.EndsWith("\\"))
				baseDirectory = baseDirectory.Substring(0, baseDirectory.Length - 1);
			const string binDebugFolder = "\\bin\\Debug";
			if (baseDirectory.EndsWith(binDebugFolder))
				return baseDirectory.Substring(0, baseDirectory.Length - binDebugFolder.Length);
			return baseDirectory;
		}
	}
}

using BotTraderCore;
using Newtonsoft.Json;
using System;
using System.IO;

namespace BotTraderCore
{
	public static class TestData
	{
		static string dataFolder;
		static TestData()
		{
			string projectFolder = Folders.GetProjectFolderName();
			dataFolder = Path.Combine(projectFolder, "TestData");
		}

		public static TickRange Load(string dataFileName)
		{
			string fullPathTestDataFileName = Path.Combine(dataFolder, dataFileName);
			string json = File.ReadAllText(fullPathTestDataFileName);
			return JsonConvert.DeserializeObject<TickRange>(json);
		}
	}
}

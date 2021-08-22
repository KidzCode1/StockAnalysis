using System;
using BotTraderCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TestCaseGeneratorCore;
using TickGraphCore;
using System.Collections.ObjectModel;

namespace TestCaseGeneratorUI
{
	/// <summary>
	/// Interaction logic for FrmTestGenerator.xaml
	/// </summary>
	public partial class FrmTestGenerator : Window
	{
		public FrmTestGenerator()
		{
			InitializeComponent();
			lstVariables.ItemsSource = variables;
		}

		private void btnSelectAll_Click(object sender, RoutedEventArgs e)
		{
			tickGraph.SelectAll();
		}

		private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{

		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		ObservableCollection<TestVariable> variables = new ObservableCollection<TestVariable>();

		List<CustomAdornment> customAdornments;

		// Call this any time we change the variables...
		void RebuildVariableAdornments()
		{
			if (variables.Count == 0)
			{
				customAdornments = null;
				return;
			}

			if (customAdornments != null)
				customAdornments.Clear();


			foreach (TestVariable testVariable in variables)
			{
				CustomAdornment customAdornment = new CustomAdornment();
				customAdornment.Key = testVariable.Key;
				customAdornment.Time = testVariable.Time;
				customAdornment.Price = testVariable.Price;
				customAdornment.Size = testVariable.Size;
				customAdornment.LeftOffset = testVariable.LeftOffset;
				customAdornment.TopOffset = testVariable.TopOffset;

				if (customAdornments == null)
					customAdornments = new List<CustomAdornment>();
				customAdornments.Add(customAdornment);
			}
		}

		void AddVariable(TestVariable testVariable)
		{
			variables.Add(testVariable);
			RebuildVariableAdornments();
			DrawGraph();
		}

		int numVariablesCreated = 0;
		DateTime timeAtMouse;
		decimal priceAtMouse;

		string GetNewVariableName()
		{
			return "var" + numVariablesCreated++;
		}

		private void miTime_Click(object sender, RoutedEventArgs e)
		{
			AddVariable(new TestVariableTime(GetNewVariableName(), timeAtMouse));
		}

		private void miPrice_Click(object sender, RoutedEventArgs e)
		{
			AddVariable(new TestVariablePrice(GetNewVariableName(), priceAtMouse));
		}

		private void miDataPoint_Click(object sender, RoutedEventArgs e)
		{

		}

		private void miDataRange_Click(object sender, RoutedEventArgs e)
		{

		}

		MenuItem GetMenuItem(string menuItemName)
		{
			ContextMenu menu = Resources["AddVariableMenu"] as ContextMenu;
			foreach (object item in menu.Items)
				if (item is MenuItem menuItem && menuItem.Name == menuItemName)
					return menuItem;
			return null;
		}

		private void ContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			timeAtMouse = tickGraph.GetTimeAtMouse();
			priceAtMouse = tickGraph.GetPriceAtMouse();

			MenuItem miPrice = GetMenuItem("miPrice");
			MenuItem miTime = GetMenuItem("miTime");
			MenuItem miDataPoint = GetMenuItem("miDataPoint");
			MenuItem miDataRange = GetMenuItem("miDataRange");

			if (tickGraph.Selection.Exists)
			{
				miDataRange.Visibility = Visibility.Visible;
				miDataPoint.Visibility = Visibility.Collapsed;
				miTime.Visibility = Visibility.Collapsed;
				miPrice.Visibility = Visibility.Collapsed;
			}
			else
			{
				miDataRange.Visibility = Visibility.Collapsed;
				miDataPoint.Visibility = Visibility.Visible;
				miTime.Visibility = Visibility.Visible;
				miPrice.Visibility = Visibility.Visible;
			}
		}

		public void SetChartTranslator(ChartTranslator chartTranslator)
		{
			tickGraph.SetChartTranslator(chartTranslator);
			DrawGraph();
		}

		private void DrawGraph()
		{
			tickGraph.DrawGraph(customAdornments);
		}

		string SaveScreenShot()
		{
			string screenShotName;
			// do work here.
			string projectFolder = Folders.GetProjectFolderName();

			screenShotName = Guid.NewGuid().ToString() + ".png";
			string fullPathToFile = System.IO.Path.Combine(projectFolder, ".cr\\images", screenShotName);
			int width = (int)tickGraph.ActualWidth;
			int height = (int)tickGraph.ActualHeight;
			RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

			tickGraph.HideCoreAdornments();
			renderTargetBitmap.Render(tickGraph);
			tickGraph.ShowCoreAdornments();

			PngBitmapEncoder pngImage = new PngBitmapEncoder();
			pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
			using (System.IO.Stream fileStream = System.IO.File.Create(fullPathToFile))
				pngImage.Save(fileStream);
			return screenShotName;
		}
		private void btnGenerateTest_Click(object sender, RoutedEventArgs e)
		{
			string testDataFileName = Guid.NewGuid().ToString() + ".json";
			string projectFolder = Folders.GetProjectFolderName();
			string testCaseFolder = System.IO.Path.Combine(projectFolder, "BotTraderTests\\TestData");
			string fullPathTestDataFileName = System.IO.Path.Combine(testCaseFolder, testDataFileName);
			tickGraph.SaveData(fullPathTestDataFileName);

			string screenShotName = SaveScreenShot();
			StringBuilder code = new StringBuilder();

			string testMethodName = tbxTestCaseName.Text;
			code.AppendLine($"//`![]({screenShotName};;;0.02500,0.02500)");
			code.AppendLine("[TestMethod]");
			code.AppendLine($"public void {testMethodName}()");
			code.AppendLine("{");
			code.AppendLine($"\tTickRange range = DataHelper.Load(\"{testDataFileName}\");");

			// TODO: Take a screen shot of the app!!!
			// TODO: Save the data in chartTranslator!!!
			// TODO: Build the start of the test case!!!
			foreach (TestVariable testVariable in variables)
			{
				testVariable.GenerateInitialization(code);
			}

			code.AppendLine("}");
			string codeSoFar = code.ToString();
			// TODO: Build the end of the test case!!!
			//			tickGraph.SaveData(Folders.GetTestFilePath("Test3.json"));

			Clipboard.SetText(code.ToString());

		}
		public void SetTestName(string str)
		{
			tbxTestCaseName.Text = str;
		}
	}
}

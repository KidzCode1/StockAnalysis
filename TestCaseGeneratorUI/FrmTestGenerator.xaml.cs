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
		void AddVariable(TestVariable testVariable)
		{
			variables.Add(testVariable);
		}

		int numVariablesCreated = 0;

		string GetNewVariableName()
		{
			return "var" + numVariablesCreated++;
		}

		private void miTime_Click(object sender, RoutedEventArgs e)
		{
			AddVariable(new TestVariableTime(GetNewVariableName(), tickGraph.GetTimeAtMouse()));
		}

		private void miPrice_Click(object sender, RoutedEventArgs e)
		{

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
			tickGraph.DrawGraph();
		}

		private void btnGenerateTest_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder code = new StringBuilder();
			// TODO: Take a screen shot of the app!!!
			// TODO: Save the data in chartTranslator!!!
			// TODO: Build the start of the test case!!!
			foreach (TestVariable testVariable in variables)
			{
				testVariable.GenerateInitialization(code);
			}
			// TODO: Build the end of the test case!!!
		}
	}
}

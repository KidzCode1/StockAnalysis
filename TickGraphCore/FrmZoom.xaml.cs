using BotTraderCore;
using System;
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

namespace TickGraphCore
{
	/// <summary>
	/// Interaction logic for FrmZoom.xaml
	/// </summary>
	public partial class FrmZoom : Window
	{
		public FrmZoom()
		{
			InitializeComponent();
			tickGraphZoom.SetChartTranslator(new ChartTranslator());
		}

		public void SetTradeHistory(ITradeHistory tradeHistory)
		{
			tickGraphZoom.SetTradeHistory(tradeHistory);
			tickGraphZoom.DrawGraph();
		}
	}
}

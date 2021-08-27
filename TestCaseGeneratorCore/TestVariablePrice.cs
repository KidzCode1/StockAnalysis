using System;
using System.Linq;
using System.Text;
using TickGraphCore;

namespace TestCaseGeneratorCore
{
	public class TestVariablePrice : TestVariable
	{
		public decimal Value { get; set; }

		public TestVariablePrice(string name, decimal value) : base(name)
		{
			Value = value;
		}


		public override string Key => "iconPricePoint";  // The name of the resource for the down arrow in the TickGraph's XAML.
		public override DateTime Time => DateTime.MinValue;  // Put this at the left of the graph
		public override decimal Price => Value;
		public override double Size => 50;
		public override double IconLeftOffset => -10;
		public override double IconTopOffset => -Size / 2;
		public override double LabelTopOffset => - 3 * Size / 4;
		public override DashedLineOption DashedLineOption => DashedLineOption.Horizontal;


		public override void GenerateInitialization(StringBuilder code)
		{
			code.AppendLine($"\tdecimal {Name} = decimal.Parse(\"{Value}\");");
		}
	}
}

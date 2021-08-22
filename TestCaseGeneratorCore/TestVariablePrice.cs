using System;
using System.Linq;
using System.Text;

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
		public override double LeftOffset => -10;
		public override double TopOffset => -Size / 2;

		public override void GenerateInitialization(StringBuilder code)
		{
			code.AppendLine($"\tdecimal {Name} = decimal.Parse(\"{Value}\");");
		}
	}
}

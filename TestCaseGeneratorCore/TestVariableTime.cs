using System;
using System.Linq;
using System.Text;

namespace TestCaseGeneratorCore
{
	public class TestVariableTime : TestVariable
	{
		// TODO: We better set Value for this to work!!!
		public DateTime Value { get; set; }
		public TestVariableTime(string name, DateTime value) : base(name)
		{
			Value = value;
		}

		public override string Key => "iconTimePoint";
		public override DateTime Time => Value;
		public override decimal Price => decimal.MaxValue;  // Put this at the top of the graph
		public override double Size => 50;
		public override double LeftOffset => -Size / 2;
		public override double TopOffset => -10;

		public override void GenerateInitialization(StringBuilder code)
		{
			code.AppendLine($"\tDateTime {Name} = DateTime.Parse(\"{Value:yyy MMM dd hh:mm:ss.fffffff}\");");
		}
	}
}

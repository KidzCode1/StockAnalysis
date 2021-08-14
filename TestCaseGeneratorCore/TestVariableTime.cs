using System;
using System.Linq;
using System.Text;

namespace TestCaseGeneratorCore
{
	public class TestVariableTime : TestVariable
	{
		// TODO: We better set Value for this to work!!!
		public DateTime Value { get; set; }
		public TestVariableTime(string name, DateTime value): base(name)
		{
			Value = value;
		}

		public override void GenerateInitialization(StringBuilder code)
		{
			code.AppendLine($"DateTime {Name} = DateTime.Parse(\"{Value:yyy MMM dd hh:mm:ss.fffffff}\");");
		}
	}
}

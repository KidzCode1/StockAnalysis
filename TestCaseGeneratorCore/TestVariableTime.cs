using System;
using System.Linq;
using System.Text;

namespace TestCaseGeneratorCore
{
	public class TestVariableTime : TestVariable
	{
		// TODO: We better set Value for this to work!!!
		public DateTime Value { get; set; }
		public TestVariableTime()
		{

		}

		public override void GenerateInitialization(StringBuilder code)
		{
			code.AppendLine($"DateTime {Name} = DateTime.Parse(\"{Value}\");");
		}
	}
}

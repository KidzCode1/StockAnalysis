using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCaseGeneratorCore
{
	public abstract class TestVariable
	{
		public abstract void GenerateInitialization(StringBuilder code);
		public string Name { get; set; }
		public TestVariable()
		{
			
		}
	}
}

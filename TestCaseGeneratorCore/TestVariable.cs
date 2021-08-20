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
		public virtual string Key => "";
		public virtual DateTime Time => DateTime.MinValue;
		public virtual decimal Price => decimal.MinValue;
		public virtual double Size => 0;
		public virtual double LeftOffset => 0;
		public virtual double TopOffset => 0;

		public TestVariable(string name)
		{
			Name = name;
		}
	}
}

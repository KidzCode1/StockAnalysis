using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TickGraphCore;

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
		public virtual double IconLeftOffset => 0;
		public virtual double IconTopOffset => 0;
		public virtual double LabelLeftOffset => 0;
		public virtual double LabelTopOffset => 0;
		public virtual LabelAlignment LabelAlignment => 0;
		public virtual DashedLineOption DashedLineOption => DashedLineOption.None;
		public virtual Color Color => Colors.Red;

		public TestVariable(string name)
		{
			Name = name;
		}
	}
}

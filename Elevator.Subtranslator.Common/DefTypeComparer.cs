using System.Collections.Generic;

namespace Elevator.Subtranslator.Common
{
	public class DefTypeComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			string clearXDefType = x.Replace("Defs", "Def");
			string clearYDefType = y.Replace("Defs", "Def");

			return clearXDefType == clearYDefType;
		}

		public int GetHashCode(string obj)
		{
			string clearDefType = obj.Replace("Defs", "Def");
			return obj.Replace("Defs", "Def").GetHashCode();
		}
	}
}

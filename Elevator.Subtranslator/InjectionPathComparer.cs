using System.Collections.Generic;

namespace Elevator.Subtranslator
{
	class InjectionPathComparer : IEqualityComparer<Injection>
	{
		public bool Equals(Injection x, Injection y)
		{
			string clearXDefType = x.DefType.Replace("Defs", "Def");
			string clearYDefType = y.DefType.Replace("Defs", "Def");

			return clearXDefType == clearYDefType && x.DefPath == y.DefPath;
		}

		public int GetHashCode(Injection obj)
		{
			string clearDefType = obj.DefType.Replace("Defs", "").Replace("Def", "");
			return clearDefType.GetHashCode() * 17 + obj.DefPath.GetHashCode();
		}
	}
}

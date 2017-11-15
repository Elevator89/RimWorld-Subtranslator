using System.Collections.Generic;

namespace Elevator.Subtranslator.Common
{
	public class InjectionPathComparer : IEqualityComparer<Injection>
	{
		private readonly DefTypeComparer _defTypeComparer = new DefTypeComparer();

		public bool Equals(Injection x, Injection y)
		{

			return _defTypeComparer.Equals(x.DefType, y.DefType) && x.DefPath == y.DefPath;
		}

		public int GetHashCode(Injection obj)
		{
			return _defTypeComparer.GetHashCode(obj.DefType) * 17 + obj.DefPath.GetHashCode();
		}
	}

}

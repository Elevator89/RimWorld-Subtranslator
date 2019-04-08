using System.Collections.Generic;

namespace Elevator.Subtranslator.Common
{
	public class InjectionTypeTranslationComparer : IEqualityComparer<Injection>
	{
		public bool Equals(Injection x, Injection y)
		{
			return x.DefType == y.DefType && x.Translation == y.Translation;
		}

		public int GetHashCode(Injection obj)
		{
			return obj.DefType.GetHashCode() + 37 * obj.Translation.GetHashCode();
		}
	}
}

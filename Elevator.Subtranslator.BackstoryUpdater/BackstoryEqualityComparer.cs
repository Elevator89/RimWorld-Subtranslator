using System.Collections.Generic;

namespace Elevator.Subtranslator.BackstoryUpdater
{
	public class BackstoryEqualityComparer : IEqualityComparer<Backstory>
	{
		public bool Equals(Backstory x, Backstory y)
		{
			return Backstory.GetIdentifier(x) == Backstory.GetIdentifier(y);
		}

		public int GetHashCode(Backstory backstory)
		{
			return Backstory.GetIdentifier(backstory).GetHashCode();
		}
	}
}

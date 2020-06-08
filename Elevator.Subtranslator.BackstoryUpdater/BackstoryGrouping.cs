using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Elevator.Subtranslator.BackstoryUpdater
{
	public class CategorizedBackstories : IGrouping<string, Backstory>
	{
		public readonly string Category;
		private readonly IEnumerable<Backstory> _backstories;

		public string Key => Category;

		public CategorizedBackstories(string category, IEnumerable<Backstory> backstories)
		{
			Category = category;
			_backstories = backstories;
		}

		public IEnumerator<Backstory> GetEnumerator()
		{
			return _backstories.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _backstories.GetEnumerator();
		}
	}
}

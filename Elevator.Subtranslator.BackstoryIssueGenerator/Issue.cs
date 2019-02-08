using System.Collections.Generic;
using System.Linq;

namespace Elevator.Subtranslator.BackstoryIssueGenerator
{
	public class Issue
	{
		private List<string> _stories;

		public void AddStory(string name)
		{
			_stories.Add(name);
		}

		public override string ToString()
		{
			return string.Format("Backstories: {0}..{1}", _stories.First(), _stories.Last());
		}
	}
}

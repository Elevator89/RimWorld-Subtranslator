using System.Collections;
using System.Collections.Generic;

namespace Elevator.Subtranslator.Common.JoinExtensions
{
	public class JoinedEnumerable<T> : IEnumerable<T>
	{
		public readonly IEnumerable<T> Source;
		public bool IsOuter;

		public JoinedEnumerable(IEnumerable<T> source) { Source = source; }

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return Source.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return Source.GetEnumerator(); }
	}
}

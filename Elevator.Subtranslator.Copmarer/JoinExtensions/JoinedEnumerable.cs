using System;
using System.Collections.Generic;
using System.Linq;

namespace Elevator.Subtranslator.Comparer.JoinExtensions
{
	public static class JoinedEnumerable
	{
		public static JoinedEnumerable<TElement> Inner<TElement>(this IEnumerable<TElement> source)
		{
			return Wrap(source, false);
		}

		public static JoinedEnumerable<TElement> Outer<TElement>(this IEnumerable<TElement> source)
		{
			return Wrap(source, true);
		}

		public static JoinedEnumerable<TElement> Wrap<TElement>(IEnumerable<TElement> source, bool isOuter)
		{
			JoinedEnumerable<TElement> joinedSource
				= source as JoinedEnumerable<TElement> ??
					new JoinedEnumerable<TElement>(source);
			joinedSource.IsOuter = isOuter;
			return joinedSource;
		}

		public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this JoinedEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer = null)
		{
			if (outer == null)
				throw new ArgumentNullException("outer");
			if (inner == null)
				throw new ArgumentNullException("inner");
			if (outerKeySelector == null)
				throw new ArgumentNullException("outerKeySelector");
			if (innerKeySelector == null)
				throw new ArgumentNullException("innerKeySelector");
			if (resultSelector == null)
				throw new ArgumentNullException("resultSelector");

			bool leftOuter = outer.IsOuter;
			bool rightOuter = (inner is JoinedEnumerable<TInner>) && ((JoinedEnumerable<TInner>)inner).IsOuter;

			if (leftOuter && rightOuter)
				return FullOuterJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

			if (leftOuter)
				return LeftOuterJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

			if (rightOuter)
				return RightOuterJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

			return Enumerable.Join(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
		}

		public static IEnumerable<TResult> LeftOuterJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer = null)
		{
			var innerLookup = inner.ToLookup(innerKeySelector, comparer);

			foreach (var outerItem in outer)
				foreach (var innerItem in innerLookup[outerKeySelector(outerItem)].DefaultIfEmpty())
					yield return resultSelector(outerItem, innerItem);
		}

		public static IEnumerable<TResult> RightOuterJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer = null)
		{
			var outerLookup = outer.ToLookup(outerKeySelector, comparer);

			foreach (var innerItem in inner)
				foreach (var outerItem in outerLookup[innerKeySelector(innerItem)].DefaultIfEmpty())
					yield return resultSelector(outerItem, innerItem);
		}

		public static IEnumerable<TResult> FullOuterJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer = null)
		{
			var outerLookup = outer.ToLookup(outerKeySelector, comparer);
			var innerLookup = inner.ToLookup(innerKeySelector, comparer);

			foreach (var innerGrouping in innerLookup)
				if (!outerLookup.Contains(innerGrouping.Key))
					foreach (TInner innerItem in innerGrouping)
						yield return resultSelector(default(TOuter), innerItem);

			foreach (var outerGrouping in outerLookup)
				foreach (var innerItem in innerLookup[outerGrouping.Key].DefaultIfEmpty())
					foreach (var outerItem in outerGrouping)
						yield return resultSelector(outerItem, innerItem);
		}
	}
}

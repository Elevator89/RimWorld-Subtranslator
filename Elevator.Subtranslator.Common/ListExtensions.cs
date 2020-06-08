using System;
using System.Collections.Generic;
using System.Linq;

namespace Elevator.Subtranslator.Common
{
	public static class ListExtensions
	{
		public static void Divide<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out List<TSource> positives, out List<TSource> negatives)
		{
			positives = new List<TSource>();
			negatives = new List<TSource>();

			foreach (TSource item in source)
			{
				if (predicate(item))
					positives.Add(item);
				else
					negatives.Add(item);
			}
		}

		public static TSource MinValue<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector, out TValue minValue) where TValue : IComparable<TValue>
		{
			int minIndex = MinIndex(source, selector, out minValue);
			return source.ElementAtOrDefault(minIndex);
		}

		public static int MinIndex<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector) where TValue : IComparable<TValue>
		{
			return MinIndex(source, selector, out TValue _);
		}

		public static int MinIndex<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector, out TValue minValue) where TValue : IComparable<TValue>
		{
			minValue = default(TValue);

			if (!source.Any())
				return -1;

			int i = 0;
			int minIndex = i;
			minValue = selector(source.First());

			foreach (TSource item in source.Skip(1))
			{
				++i;

				TValue value = selector(item);
				if (value.CompareTo(minValue) < 0)
				{
					minIndex = i;
					minValue = value;
				}
			}

			return minIndex;
		}
	}
}

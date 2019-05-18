namespace Elevator.Subtranslator.LabelDecliner
{
	public static class StringExt
	{
		public static int IndexOfAny(this string str, params string[] anyOf)
		{
			int minIndex = int.MaxValue;

			foreach (string value in anyOf)
			{
				int index = str.IndexOf(value);
				if (index == -1)
					continue;

				if (index < minIndex)
				{
					minIndex = index;
				}
			}

			return minIndex == int.MaxValue ? -1 : minIndex;
		}
	}
}

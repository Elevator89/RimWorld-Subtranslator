namespace Elevator.Subtranslator.Common
{
    public static class StringExtensions
    {
        public static string DecapitalizeFirst(this string input)
        {
            if (input.Length == 0)
                return input;

            char[] chars = input.ToCharArray();
            chars[0] = char.ToLower(chars[0]);
            return new string(chars);
        }

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

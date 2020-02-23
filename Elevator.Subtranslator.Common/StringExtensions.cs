using System;

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

        public static string CapitalizeFirst(this string input)
        {
            if (input.Length == 0)
                return input;

            char[] chars = input.ToCharArray();
            chars[0] = char.ToUpper(chars[0]);
            return new string(chars);
        }

        public static string FixNewLines(this string input)
        {
            return input.Replace("\\n", Environment.NewLine).Replace(Environment.NewLine, "\n").Replace("\n", Environment.NewLine);
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

using System.Text;

namespace Elevator.Subtranslator.BackstoryUpdater
{
    public class VerseGenTextMock
    {
        public static int StableStringHash(string str)
        {
            if (str == null)
            {
                return 0;
            }
            int num = 23;
            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                num = num * 31 + str[i];
            }
            return num;
        }

        public static string RemoveNonAlphanumeric(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsLetterOrDigit(s[i]))
                {
                    sb.Append(s[i]);
                }
            }
            return sb.ToString();
        }

        public static string CapitalizedNoSpaces(string s)
        {
            string[] array = s.Split(' ');
            StringBuilder stringBuilder = new StringBuilder();
            string[] array2 = array;
            foreach (string text in array2)
            {
                if (text.Length > 0)
                {
                    stringBuilder.Append(char.ToUpper(text[0]));
                }
                if (text.Length > 1)
                {
                    stringBuilder.Append(text.Substring(1));
                }
            }
            return stringBuilder.ToString();
        }
    }
}

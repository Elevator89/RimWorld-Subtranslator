using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Verse;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string str;

            LanguageWorker_Russian languageWorker = new LanguageWorker_Russian();

            while (true)
            {
                Console.Write("Enter text: ");
                str = Console.ReadLine();
                if (string.IsNullOrEmpty(str))
                    return;

                //Console.WriteLine("Title Case: {0}", languageWorker.ToTitleCase(str));
                Console.WriteLine("Title Case: {0}", languageWorker.PostProcessed(str));
            }
        }
    }
}

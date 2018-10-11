using CommandLine;
using System.Linq;
using System.Xml.Linq;

namespace Elevator.Subtranslator.TagCaseLower
{
    class Options
    {
        [Option('t', "target", Required = true, HelpText = "Path to the original backstories file.")]
        public string TargetFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "Path to the output file.")]
        public string OutputFile { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
            if (parseResult.Errors.Any())
                return;

            Options options = parseResult.Value;

            XDocument backstoriesDoc = XDocument.Load(options.TargetFile, LoadOptions.PreserveWhitespace);

            foreach (XElement backstory in backstoriesDoc.Root.Elements())
            {
                DecapitalizeValueIfNotNull(backstory.Element("title"));
                DecapitalizeValueIfNotNull(backstory.Element("titleFemale"));
                DecapitalizeValueIfNotNull(backstory.Element("titleShort"));
                DecapitalizeValueIfNotNull(backstory.Element("titleShortFemale"));
            }

            backstoriesDoc.Save(options.OutputFile);
        }

        static void DecapitalizeValueIfNotNull(XElement element)
        {
            if (element == null)
                return;

            element.SetValue(DecapitalizeFirst(element.Value));
        }

        static string DecapitalizeFirst(string input)
        {
            if (input.Length == 0) return input;

            char[] chars = input.ToCharArray();
            chars[0] = char.ToLower(chars[0]);
            return new string(chars);
        }
    }
}

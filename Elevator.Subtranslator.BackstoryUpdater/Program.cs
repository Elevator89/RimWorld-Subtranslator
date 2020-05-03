using CommandLine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Elevator.Subtranslator.BackstoryUpdater
{
    class Options
    {
        [Option('r', "resources", Required = true, HelpText = "Path to unpacked Unity_Assets_Files\\resources directory")]
        public string ResourcesDirectory { get; set; }

        [Option('t', "translatedBackstories", Required = true, HelpText = "Path to translated backsories file")]
        public string TranslatedBackstoriesFile { get; set; }

        [Option('o', "outputBackstories", Required = true, HelpText = "Path to translated output backsories file")]
        public string OutputBackstories { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
            if (parseResult.Errors.Any())
                return;

            Options options = parseResult.Value;

            XDocument translatedBackstoriesDoc = XDocument.Load(options.TranslatedBackstoriesFile, LoadOptions.None);
            Dictionary<string, Backstory> translatedBackstories = translatedBackstoriesDoc.Root.Elements().Select(XmlHelper.ReadBackstoryElementTranslated).ToDictionary(backstory => backstory.Id);

            XDocument outputDoc = new XDocument();
            XElement root = new XElement("BackstoryTranslations");
            outputDoc.Add(root);
            outputDoc.Root.Add(XmlHelper.NewLine);

            foreach (CategorizedBackstories categorizedBackstories in XmlHelper.GetAllResourceBackstories(options.ResourcesDirectory))
            {
                outputDoc.Root.Add(XmlHelper.NewLine);
                outputDoc.Root.Add(XmlHelper.Tab, new XComment($" Category: {categorizedBackstories.Category} "), XmlHelper.NewLine);

                foreach (Backstory backstory in categorizedBackstories)
                {
                    if (translatedBackstories.TryGetValue(backstory.Id, out Backstory translatedBackstory))
                    {
                        outputDoc.Root.Add(XmlHelper.NewLine);
                        outputDoc.Root.Add(XmlHelper.Tab, new XComment($" {GetHint(backstory)} "), XmlHelper.NewLine);
                        outputDoc.Root.Add(XmlHelper.Tab, XmlHelper.BuildBackstoryElementTranslatedWithEnglishComments(backstory, translatedBackstory), XmlHelper.NewLine);
                        translatedBackstories.Remove(backstory.Id);
                    }
                    else
                    {
                        outputDoc.Root.Add(XmlHelper.NewLine);
                        outputDoc.Root.Add(XmlHelper.Tab, new XComment($" {GetHint(backstory)} "), XmlHelper.NewLine);
                        outputDoc.Root.Add(XmlHelper.Tab, XmlHelper.BuildBackstoryElementTodoWithEnglishComments(backstory), XmlHelper.NewLine);
                    }
                }
            }

            if (translatedBackstories.Count > 0)
            {
                outputDoc.Root.Add(XmlHelper.NewLine);
                outputDoc.Root.Add(XmlHelper.Tab, new XComment($" Translated but unused "), XmlHelper.NewLine);

                foreach (Backstory translatedBackstory in translatedBackstories.Values)
                {
                    outputDoc.Root.Add(XmlHelper.NewLine);
                    outputDoc.Root.Add(XmlHelper.Tab, new XComment($" {GetHint(translatedBackstory)} "), XmlHelper.NewLine);
                    outputDoc.Root.Add(XmlHelper.Tab, XmlHelper.BuildBackstoryElementSimple(translatedBackstory), XmlHelper.NewLine);
                }
            }

            outputDoc.Root.Add(XmlHelper.NewLine);
            outputDoc.Save(options.OutputBackstories, SaveOptions.None);
        }

        private static string GetHint(Backstory backstory)
        {
            StringBuilder hintBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(backstory.FirstName))
            {
                hintBuilder.Append($"{backstory.FirstName} ");

                if (!string.IsNullOrEmpty(backstory.NickName))
                    hintBuilder.Append($"\"{backstory.NickName}\" ");

                hintBuilder.Append($"{backstory.LastName}");
            }

            if (!string.IsNullOrEmpty(backstory.Gender))
                hintBuilder.Append($", {backstory.Gender}, ");

            hintBuilder.Append($"{backstory.Slot}");

            return hintBuilder.ToString();
        }
    }
}

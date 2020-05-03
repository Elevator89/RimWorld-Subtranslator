using CommandLine;
using Elevator.Subtranslator.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Elevator.Subtranslator.BackstoryUpdater
{
    class Options
    {
        [Option('p', "resourcesPrev", Required = false, DefaultValue = "", HelpText = "Path to unpacked Unity_Assets_Files\\resources directory for the previous version")]
        public string ResourcesDirectoryPrev { get; set; }

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

            bool migrate = !string.IsNullOrEmpty(options.ResourcesDirectoryPrev);

            HashSet<Backstory> backstoriesPrev = migrate ? new HashSet<Backstory>(XmlHelper.GetAllResourceBackstories(options.ResourcesDirectoryPrev).SelectMany(caegorized => caegorized), new BackstoryEqualityComparer()) : null;

            foreach (CategorizedBackstories categorizedBackstories in XmlHelper.GetAllResourceBackstories(options.ResourcesDirectory))
            {
                outputDoc.Root.Add(XmlHelper.NewLine);
                outputDoc.Root.Add(XmlHelper.Tab, new XComment($" Category: {categorizedBackstories.Category} "), XmlHelper.NewLine);

                int counter = 0;

                foreach (Backstory backstory in categorizedBackstories)
                {
                    Console.Write($"\rProcessing category: {categorizedBackstories.Category} ({++counter})");

                    string currentBackstoryId = null;

                    if (migrate)
                    {
                        Backstory bestMatchPrevBackstory = FindBestMatch(backstoriesPrev, backstory);
                        if (bestMatchPrevBackstory != null)
                        {
                            currentBackstoryId = bestMatchPrevBackstory.Id;
                            backstoriesPrev.Remove(bestMatchPrevBackstory);
                        }
                    }
                    else
                    {
                        currentBackstoryId = backstory.Id;
                    }

                    if (currentBackstoryId != null && translatedBackstories.TryGetValue(currentBackstoryId, out Backstory translatedBackstory))
                    {
                        outputDoc.Root.Add(XmlHelper.NewLine);
                        outputDoc.Root.Add(XmlHelper.Tab, new XComment($" {GetHint(backstory)} "), XmlHelper.NewLine);
                        outputDoc.Root.Add(XmlHelper.Tab, XmlHelper.BuildBackstoryElementTranslatedWithEnglishComments(backstory, translatedBackstory), XmlHelper.NewLine);
                        translatedBackstories.Remove(currentBackstoryId);
                    }
                    else
                    {
                        outputDoc.Root.Add(XmlHelper.NewLine);
                        outputDoc.Root.Add(XmlHelper.Tab, new XComment($" {GetHint(backstory)} "), XmlHelper.NewLine);
                        outputDoc.Root.Add(XmlHelper.Tab, XmlHelper.BuildBackstoryElementTodoWithEnglishComments(backstory), XmlHelper.NewLine);
                    }
                }

                Console.WriteLine();
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

        private static Backstory FindBestMatch(IEnumerable<Backstory> backstories, Backstory backstory)
        {
            string id = Backstory.GetIdentifier(backstory);
            Backstory matchById = backstories.FirstOrDefault(bs => Backstory.GetIdentifier(bs) == id);

            if (matchById != null)
                return matchById;

            if (backstory.FirstName != null && backstory.LastName != null)
            {
                Backstory matchBySolidName = backstories.FirstOrDefault(bs => bs.FirstName == backstory.FirstName && bs.LastName == backstory.LastName && bs.Slot == backstory.Slot);

                if (matchBySolidName != null)
                    return matchBySolidName;
            }

            LevenshteinMeter _levenshteinMeter = new LevenshteinMeter(1, 1, 1);
            Backstory matchByDescription = backstories.MinValue(bs => _levenshteinMeter.GetNormedDistanceQ(bs.Description, backstory.Description, 0.5f), out float minDist);

            if (matchByDescription != null && minDist < 0.3f)
                return matchByDescription;

            return null;
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

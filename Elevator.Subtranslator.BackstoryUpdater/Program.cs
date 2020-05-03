using CommandLine;
using Elevator.Subtranslator.Common;
using Fastenshtein;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;

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

    /// <summary>
    /// -r "G:\Rimworld translation\_Tools\resources-2408\Unity_Assets_Files\resources" -t "G:\Rimworld translation\_Translation\RimWorld-ru\Core\Backstories\Backstories.xml" -o "G:\Rimworld translation\_Translation\RimWorld-ru\Core\Backstories\Backstories-new.xml"
    /// -p "G:\Rimworld translation\_Tools\resources-2408\Unity_Assets_Files\resources" -r "G:\Rimworld translation\_Tools\resources\Unity_Assets_Files\resources" -t "G:\Rimworld translation\_Translation\RimWorld-ru\Core\Backstories\Backstories.xml" -o "G:\Rimworld translation\_Translation\RimWorld-ru\Core\Backstories\Backstories-new.xml"
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
            if (parseResult.Errors.Any())
                return;

            Options options = parseResult.Value;

            XDocument translatedBackstoriesDoc = XDocument.Load(options.TranslatedBackstoriesFile, LoadOptions.None);
            List<Backstory> translatedBackstories = translatedBackstoriesDoc.Root.Elements().Select(XmlHelper.ReadBackstoryElementTranslated).ToList();

            bool migrate = !string.IsNullOrEmpty(options.ResourcesDirectoryPrev);

            Dictionary<string, Backstory> regularBackstories = LoadResourceBackstoriesRegular(options.ResourcesDirectory).OrderBy(bs => bs.Id).ToDictionary(bs => bs.Id);
            Dictionary<string, Backstory> solidBackstories = LoadResourceBackstoriesSolid(options.ResourcesDirectory).OrderBy(bs => bs.FirstName + bs.LastName).ThenBy(bs => (int)bs.Slot).ToDictionary(bs => bs.Id);

            Dictionary<string, string> oldToNewIdsRegular = null;
            Dictionary<string, string> oldToNewIdsSolid = null;
            if (migrate)
            {
                HashSet<Backstory> backstoriesPrevRegular = new HashSet<Backstory>(LoadResourceBackstoriesRegular(options.ResourcesDirectoryPrev), new BackstoryEqualityComparer());
                HashSet<Backstory> backstoriesPrevSolid = new HashSet<Backstory>(LoadResourceBackstoriesSolid(options.ResourcesDirectoryPrev), new BackstoryEqualityComparer());

                oldToNewIdsRegular = new Dictionary<string, string>();
                FillBackstoryMigrationMap(backstoriesPrevRegular, regularBackstories.Values, oldToNewIdsRegular);
                Console.WriteLine();
                oldToNewIdsSolid = new Dictionary<string, string>();
                FillBackstoryMigrationMap(backstoriesPrevSolid, solidBackstories.Values, oldToNewIdsSolid);
                Console.WriteLine();
            }

            List<Backstory> backstoriesTranslatedRegular = new List<Backstory>();
            List<Backstory> backstoriesTranslatedSolid = new List<Backstory>();
            List<Backstory> backstoriesTranslatedUnused = new List<Backstory>();

            foreach (Backstory translatedBackstory in translatedBackstories)
            {
                string resourceBackstoryIdRegular = translatedBackstory.Id;
                string resourceBackstoryIdSolid = translatedBackstory.Id;

                if (migrate)
                {
                    oldToNewIdsRegular.TryGetValue(translatedBackstory.Id, out resourceBackstoryIdRegular);
                    oldToNewIdsSolid.TryGetValue(translatedBackstory.Id, out resourceBackstoryIdSolid);
                }

                if (resourceBackstoryIdRegular != null && regularBackstories.TryGetValue(resourceBackstoryIdRegular, out Backstory regularBackstory))
                {
                    translatedBackstory.Id = resourceBackstoryIdRegular;
                    backstoriesTranslatedRegular.Add(translatedBackstory);
                }
                else if (resourceBackstoryIdSolid != null && solidBackstories.TryGetValue(resourceBackstoryIdSolid, out Backstory solidBackstory))
                {
                    translatedBackstory.Id = resourceBackstoryIdSolid;
                    backstoriesTranslatedSolid.Add(translatedBackstory);
                }
                else
                {
                    backstoriesTranslatedUnused.Add(translatedBackstory);
                }
            }

            XDocument outputDoc = new XDocument();
            XElement root = new XElement("BackstoryTranslations");
            outputDoc.Add(root);
            outputDoc.Root.Add(XmlHelper.NewLine);

            foreach (Backstory bs in backstoriesTranslatedRegular)
            {
                Backstory regularBackstory = regularBackstories[bs.Id];
                AddBackstoryElementWithHint(outputDoc, GetHint(regularBackstory), XmlHelper.BuildBackstoryElementTranslatedWithEnglishComments(regularBackstory, bs));
                regularBackstories.Remove(bs.Id);
            }

            foreach (Backstory bs in regularBackstories.Values)
                AddBackstoryElementWithHint(outputDoc, GetHint(bs), XmlHelper.BuildBackstoryElementTodoWithEnglishComments(bs));

            foreach (Backstory bs in backstoriesTranslatedSolid)
            {
                Backstory solidBackstory = solidBackstories[bs.Id];
                AddBackstoryElementWithHint(outputDoc, GetHint(solidBackstory), XmlHelper.BuildBackstoryElementTranslatedWithEnglishComments(solidBackstory, bs));
                solidBackstories.Remove(bs.Id);
            }

            foreach (Backstory bs in solidBackstories.Values)
                AddBackstoryElementWithHint(outputDoc, GetHint(bs), XmlHelper.BuildBackstoryElementTodoWithEnglishComments(bs));

            outputDoc.Root.Add(XmlHelper.NewLine);
            outputDoc.Root.Add(XmlHelper.Tab, new XComment($" UNUSED "), XmlHelper.NewLine);

            foreach (Backstory bs in backstoriesTranslatedUnused)
                AddBackstoryElementWithHint(outputDoc, GetHint(bs), XmlHelper.BuildBackstoryElementSimple(bs));

            outputDoc.Root.Add(XmlHelper.NewLine);
            outputDoc.Save(options.OutputBackstories, SaveOptions.None);
        }

        private static void FillBackstoryMigrationMap(HashSet<Backstory> prevBackstories, IEnumerable<Backstory> backstories, Dictionary<string, string> oldToNewIds)
        {
            int count = backstories.Count();
            int i = 0;

            foreach (Backstory backstory in backstories)
            {
                Console.Write($"\rMapping: {++i}/{count}");

                Backstory bestMatchPrevBackstory = FindBestMatch(prevBackstories, backstory);
                if (bestMatchPrevBackstory != null)
                {
                    oldToNewIds[bestMatchPrevBackstory.Id] = backstory.Id;
                    prevBackstories.Remove(bestMatchPrevBackstory);
                }
            }
        }

        private static void AddBackstoryElementWithHint(XDocument outputDoc, string hint, XElement backstoryElement)
        {
            outputDoc.Root.Add(XmlHelper.NewLine);

            if (!string.IsNullOrEmpty(hint))
                outputDoc.Root.Add(XmlHelper.Tab, new XComment($" {hint} "), XmlHelper.NewLine);

            outputDoc.Root.Add(XmlHelper.Tab, backstoryElement, XmlHelper.NewLine);
        }

        private static Backstory FindBestMatch(HashSet<Backstory> backstories, Backstory backstory)
        {
            string id = Backstory.GetIdentifier(backstory);
            Backstory matchById = backstories.FirstOrDefault(bs => Backstory.GetIdentifier(bs) == id);

            if (matchById != null)
                return matchById;

            if (Backstory.IsSolid(backstory))
            {
                Backstory matchBySolidName = backstories.FirstOrDefault(bs => bs.FirstName == backstory.FirstName && bs.LastName == backstory.LastName && bs.Slot == backstory.Slot);

                if (matchBySolidName != null)
                    return matchBySolidName;
            }

            Levenshtein backstoryLevenshtein = new Levenshtein(backstory.Description);

            Backstory matchByDescription = backstories.MinValue(bs => backstoryLevenshtein.DistanceFrom(bs.Description), out int minDist);

            if (matchByDescription != null && minDist < 0.5f * backstory.Description.Length)
                return matchByDescription;

            return null;
        }

        private static string GetHint(Backstory backstory)
        {
            StringBuilder hintBuilder = new StringBuilder();

            if (Backstory.IsSolid(backstory))
            {
                hintBuilder.Append($"{backstory.FirstName} ");

                if (!string.IsNullOrEmpty(backstory.NickName))
                    hintBuilder.Append($"\"{backstory.NickName}\" ");

                hintBuilder.Append($"{backstory.LastName}");

                if (!string.IsNullOrEmpty(backstory.Gender))
                    hintBuilder.Append($", {backstory.Gender}");

                hintBuilder.Append($", {backstory.Slot}");
            }
            else
            {
                if (!string.IsNullOrEmpty(backstory.Category))
                    hintBuilder.Append($"{backstory.Category}");
            }

            return hintBuilder.ToString();
        }

        private static IEnumerable<Backstory> LoadResourceBackstoriesRegular(string resourcesDirectory)
        {
            XmlSchemaSet backstoriesSchemaSet = new XmlSchemaSet();
            backstoriesSchemaSet.Add(XmlSchema.Read(new StringReader(Properties.Resources.BackstoriesSchema), ValidateSchema));

            foreach (string resourceFileName in Directory.EnumerateFiles(resourcesDirectory, "*.xml", SearchOption.TopDirectoryOnly))
            {
                XDocument doc = XDocument.Load(resourceFileName, LoadOptions.None);
                if (XmlHelper.IsValid(doc, backstoriesSchemaSet))
                {
                    string category = Path.GetFileNameWithoutExtension(resourceFileName);

                    foreach (XElement element in doc.Root.Elements())
                    {
                        Backstory backstory = XmlHelper.ReadBackstoryElementResource(element);
                        backstory.Category = category;
                        yield return backstory;
                    }
                }
            }
        }

        private static IEnumerable<Backstory> LoadResourceBackstoriesSolid(string resourcesDirectory)
        {
            XmlSchemaSet playerCreatedBiosSchemaSet = new XmlSchemaSet();
            playerCreatedBiosSchemaSet.Add(XmlSchema.Read(new StringReader(Properties.Resources.PlayerCreatedBiosSchema), ValidateSchema));

            foreach (string resourceFileName in Directory.EnumerateFiles(resourcesDirectory, "*.xml", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(resourceFileName);

                XDocument doc = XDocument.Load(resourceFileName, LoadOptions.None);
                if (XmlHelper.IsValid(doc, playerCreatedBiosSchemaSet))
                {
                    string category = Path.GetFileNameWithoutExtension(resourceFileName);

                    foreach (XElement pawnBioElement in doc.Root.Elements())
                    {
                        if (XmlHelper.TryReadPawnBioBackstories(pawnBioElement, out Backstory childBackstory, out Backstory adultBackstory))
                        {
                            childBackstory.Category = category;
                            adultBackstory.Category = category;

                            yield return childBackstory;
                            yield return adultBackstory;
                        }
                    }
                }
            }
        }

        private static void ValidateSchema(object sender, ValidationEventArgs e)
        {
            throw new XmlSchemaValidationException(e.Message, e.Exception, e.Exception.LineNumber, e.Exception.LinePosition);
        }
    }
}

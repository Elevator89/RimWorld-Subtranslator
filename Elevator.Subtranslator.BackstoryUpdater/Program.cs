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
    /// -p "G:\Rimworld translation\_Tools\resources-2408\Unity_Assets_Files\resources" -r "G:\Rimworld translation\_Tools\resources\Unity_Assets_Files\resources" -t "G:\Rimworld translation\_Translation\RimWorld-ru\Core\Backstories\Backstories.xml" -o "G:\Rimworld translation\_Translation\RimWorld-ru\Core\Backstories\Backstories-new.xml"
    /// -r "G:\Rimworld translation\_Tools\resources-2408\Unity_Assets_Files\resources" -t "G:\Rimworld translation\_Translation\RimWorld-ru\Core\Backstories\Backstories.xml" -o "G:\Rimworld translation\_Translation\RimWorld-ru\Core\Backstories\Backstories-new.xml"
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
            Dictionary<string, Backstory> translatedBackstories = translatedBackstoriesDoc.Root.Elements().Select(XmlHelper.ReadBackstoryElementTranslated).ToDictionary(backstory => backstory.Id);

            XDocument outputDoc = new XDocument();
            XElement root = new XElement("BackstoryTranslations");
            outputDoc.Add(root);
            outputDoc.Root.Add(XmlHelper.NewLine);

            bool migrate = !string.IsNullOrEmpty(options.ResourcesDirectoryPrev);

            HashSet<Backstory> backstoriesPrev = migrate ? new HashSet<Backstory>(GetAllResourceBackstories(options.ResourcesDirectoryPrev), new BackstoryEqualityComparer()) : null;

            List<Backstory> orderedBackstories = OrderBackstories(GetAllResourceBackstories(options.ResourcesDirectory)).ToList();
            for (int i = 0; i < orderedBackstories.Count; ++i)
            {
                Console.Write($"\rProcessing: {i + 1}/{orderedBackstories.Count}");

                Backstory backstory = orderedBackstories[i];

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
                    AddBackstoryElementWithHint(outputDoc, GetHint(backstory), XmlHelper.BuildBackstoryElementTranslatedWithEnglishComments(backstory, translatedBackstory));
                    translatedBackstories.Remove(currentBackstoryId);
                }
                else
                {
                    AddBackstoryElementWithHint(outputDoc, GetHint(backstory), XmlHelper.BuildBackstoryElementTodoWithEnglishComments(backstory));
                }
            }


            if (translatedBackstories.Count > 0)
            {
                outputDoc.Root.Add(XmlHelper.NewLine);
                outputDoc.Root.Add(XmlHelper.Tab, new XComment($" Translated but unused "), XmlHelper.NewLine);

                foreach (Backstory translatedBackstory in translatedBackstories.Values)
                {
                    AddBackstoryElementWithHint(outputDoc, GetHint(translatedBackstory), XmlHelper.BuildBackstoryElementSimple(translatedBackstory));
                }
            }

            outputDoc.Root.Add(XmlHelper.NewLine);
            outputDoc.Save(options.OutputBackstories, SaveOptions.None);
        }

        private static void AddBackstoryElementWithHint(XDocument outputDoc, string hint, XElement backsotyElement)
        {
            outputDoc.Root.Add(XmlHelper.NewLine);

            if (!string.IsNullOrEmpty(hint))
                outputDoc.Root.Add(XmlHelper.Tab, new XComment($" {hint} "), XmlHelper.NewLine);

            outputDoc.Root.Add(XmlHelper.Tab, XmlHelper.BuildBackstoryElementTodoWithEnglishComments(backstory), XmlHelper.NewLine);
        }

        public static IEnumerable<Backstory> OrderBackstories(IEnumerable<Backstory> backstories)
        {
            backstories.Divide(bs => Backstory.IsSolid(bs), out List<Backstory> solid, out List<Backstory> regular);

            regular = regular.OrderBy(bs => bs.Id).ToList();
            solid = solid.OrderBy(bs => bs.FirstName + bs.LastName).ThenBy(bs => (int)bs.Slot).ToList();

            foreach (Backstory backstory in regular)
                yield return backstory;

            foreach (Backstory backstory in solid)
                yield return backstory;
        }

        public static IEnumerable<Backstory> GetAllResourceBackstories(string resourcesDirectory)
        {
            XmlSchemaSet backstoriesSchemaSet = new XmlSchemaSet();
            backstoriesSchemaSet.Add(XmlSchema.Read(new StringReader(Properties.Resources.BackstoriesSchema), ValidateSchema));

            XmlSchemaSet playerCreatedBiosSchemaSet = new XmlSchemaSet();
            playerCreatedBiosSchemaSet.Add(XmlSchema.Read(new StringReader(Properties.Resources.PlayerCreatedBiosSchema), ValidateSchema));

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

        private static Backstory FindBestMatch(HashSet<Backstory> backstories, Backstory backstory)
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

        private static void ValidateSchema(object sender, ValidationEventArgs e)
        {
            throw new XmlSchemaValidationException(e.Message, e.Exception, e.Exception.LineNumber, e.Exception.LinePosition);
        }
    }
}

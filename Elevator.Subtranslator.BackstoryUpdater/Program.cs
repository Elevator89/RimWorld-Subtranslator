using CommandLine;
using Elevator.Subtranslator.Common;
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
        [Option('r', "resources", Required = true, HelpText = "Path to unpacked Unity_Assets_Files\\resources directory")]
        public string ResourcesDirectory { get; set; }

        [Option('f', "freshBackstories", Required = true, HelpText = "Path to fresh backsories file")]
        public string FreshBackstoriesFile { get; set; }

        [Option('p', "prevBackstories", Required = false, DefaultValue = "", HelpText = "Path to old backsories file")]
        public string PrevBackstoriesFile { get; set; }

        [Option('t', "translatedBackstories", Required = true, HelpText = "Path to translated backsories file")]
        public string TranslatedBackstoriesFile { get; set; }

        [Option('o', "outputBackstories", Required = true, HelpText = "Path to translated output backsories file")]
        public string OutputBackstories { get; set; }
    }

    class Program
    {
        private static readonly XText _newLine = new XText(Environment.NewLine);
        private static readonly XText _tab = new XText("\t");
        private static readonly XText _tab2 = new XText("\t\t");

        static void Main(string[] args)
        {
            ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
            if (parseResult.Errors.Any())
                return;

            Options options = parseResult.Value;

            XmlSchemaSet backstoriesSchemaSet = new XmlSchemaSet();
            backstoriesSchemaSet.Add(XmlSchema.Read(new StringReader(Properties.Resources.BackstoriesSchema), ValidateSchema));

            XmlSchemaSet playerCreatedBiosSchemaSet = new XmlSchemaSet();
            playerCreatedBiosSchemaSet.Add(XmlSchema.Read(new StringReader(Properties.Resources.PlayerCreatedBiosSchema), ValidateSchema));

            XDocument freshBackstoriesDoc = XDocument.Load(options.FreshBackstoriesFile, LoadOptions.None);
            List<Backstory> freshBackstories = freshBackstoriesDoc.Root.Elements().Select(ReadTranslationBackstory).ToList();

            bool migrate = !string.IsNullOrEmpty(options.PrevBackstoriesFile);

            List<Backstory> prevBackstories = null;
            if (migrate)
            {
                XDocument prevBackstoriesDoc = XDocument.Load(options.PrevBackstoriesFile, LoadOptions.None);
                prevBackstories = prevBackstoriesDoc.Root.Elements().Select(ReadTranslationBackstory).ToList();
            }

            XDocument translatedBackstoriesDoc = XDocument.Load(options.TranslatedBackstoriesFile, LoadOptions.None);
            List<Backstory> translatedBackstories = translatedBackstoriesDoc.Root.Elements().Select(ReadTranslationBackstory).ToList();

            XDocument outputDoc = new XDocument();
            XElement root = new XElement("Backstories");
            outputDoc.Add(root);

            foreach (string resourceFileName in Directory.EnumerateFiles(options.ResourcesDirectory, "*.xml", SearchOption.TopDirectoryOnly))
            {
                string category = Path.GetFileNameWithoutExtension(resourceFileName);

                XDocument doc = XDocument.Load(resourceFileName, LoadOptions.None);
                if (IsValid(doc, backstoriesSchemaSet))
                {
                    outputDoc.Root.Add(_newLine, _tab, new XComment(category), _newLine);

                    foreach (XElement backstoryElement in doc.Root.Elements())
                    {
                        Backstory backstory = ReadResourceBackstory(backstoryElement);

                        if (migrate)
                            MigrateBackstory(backstory, prevBackstories, freshBackstories, translatedBackstories);
                        else
                            UpdateBackstory(backstory, freshBackstories, translatedBackstories);

                        outputDoc.Root.Add(_newLine, _tab, new XComment(GetHint(backstory)), _newLine);
                        outputDoc.Root.Add(_tab, GetElement(backstory), _newLine);
                    }
                }
            }

            foreach (string resourceFileName in Directory.EnumerateFiles(options.ResourcesDirectory, "*.xml", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(resourceFileName);

                XDocument doc = XDocument.Load(resourceFileName, LoadOptions.None);
                if (IsValid(doc, playerCreatedBiosSchemaSet))
                {
                    foreach (XElement pawnBioElement in doc.Root.Elements())
                    {
                        if (TryReadPawnBioBackstories(pawnBioElement, out Backstory childBackstory, out Backstory adultBackstory))
                        {
                            if (migrate)
                                MigrateBackstory(childBackstory, prevBackstories, freshBackstories, translatedBackstories);
                            else
                                UpdateBackstory(childBackstory, freshBackstories, translatedBackstories);

                            if (migrate)
                                MigrateBackstory(adultBackstory, prevBackstories, freshBackstories, translatedBackstories);
                            else
                                UpdateBackstory(adultBackstory, freshBackstories, translatedBackstories);

                            outputDoc.Root.Add(_newLine, _tab, new XComment($" {GetHint(childBackstory)} "), _newLine);
                            outputDoc.Root.Add(_tab, GetElement(childBackstory), _newLine);

                            outputDoc.Root.Add(_newLine, _tab, new XComment($" {GetHint(adultBackstory)} "), _newLine);
                            outputDoc.Root.Add(_tab, GetElement(adultBackstory), _newLine);
                        }
                    }
                }
            }

            outputDoc.Save(options.OutputBackstories, SaveOptions.None);
        }

        private static bool IsValid(XDocument doc, XmlSchemaSet schemaSet)
        {
            bool isValid = true;

            doc.Validate(schemaSet, (object sender, ValidationEventArgs e) => isValid = false);
            return isValid;
        }

        private static void UpdateBackstory(Backstory backstory, IEnumerable<Backstory> freshBackstories, IEnumerable<Backstory> translatedBackstories)
        {
            Backstory freshBackstory = freshBackstories.First(fresh => BackstoriesAreEqual(fresh, backstory));
            backstory.Id = freshBackstory.Id;

            Backstory translatedBackstory = translatedBackstories.First(translated => translated.Id == freshBackstory.Id);
            backstory.Title = translatedBackstory.Title;
            backstory.TitleShort = translatedBackstory.TitleShort;
            backstory.TitleFemale = translatedBackstory.TitleFemale;
            backstory.TitleShortFemale = translatedBackstory.TitleShortFemale;
            backstory.Description = translatedBackstory.Description;
        }

        private static void MigrateBackstory(Backstory backstory, IEnumerable<Backstory> prevBackstories, IEnumerable<Backstory> freshBackstories, IEnumerable<Backstory> translatedBackstories)
        {
            Backstory freshBackstory = freshBackstories.First(fresh => BackstoriesAreEqual(fresh, backstory));
            Backstory prevBackstory = prevBackstories.FirstOrDefault(prev => BackstoriesAreEqual(prev, backstory));

            backstory.Id = freshBackstory.Id;

            if (prevBackstory == null)
            {
                Console.WriteLine($"NEW {backstory.Id} {backstory.Title} {backstory.Title} {backstory.Description}");
            }
            else
            {
                Backstory translatedBackstory = translatedBackstories.First(translated => translated.Id == prevBackstory.Id);
                backstory.Title = translatedBackstory.Title;
                backstory.TitleShort = translatedBackstory.TitleShort;
                backstory.TitleFemale = translatedBackstory.TitleFemale;
                backstory.TitleShortFemale = translatedBackstory.TitleShortFemale;
                backstory.Description = translatedBackstory.Description;
            }
        }

        private static LevenshteinMeter _levenshteinMeter = new LevenshteinMeter(1, 1, 1);

        private static bool BackstoriesAreEqual(Backstory a, Backstory b)
        {
            return
                string.Equals(a.Title, b.Title, StringComparison.InvariantCultureIgnoreCase) &&
                _levenshteinMeter.GetNormedDistanceQ(a.Description, b.Description, 0.2f) < 0.1f;
        }

        private static bool TryReadPawnBioBackstories(XElement bioElem, out Backstory child, out Backstory adult)
        {
            string firstName = bioElem.Element("Name", true).Element("First", true).Value;
            string lastName = bioElem.Element("Name", true).Element("Last", true).Value;
            string nickName = bioElem.Element("Name", true).Element("Nick", true)?.Value;
            string gender = bioElem.Element("Gender", true).Value;

            XElement childhoodElement = bioElem.Element("Childhood", true);
            XElement adulthoodElement = bioElem.Element("Adulthood", true);

            child = adult = null;

            if (childhoodElement == null && adulthoodElement == null)
                return false;

            child = ReadResourceBackstory(childhoodElement);
            adult = ReadResourceBackstory(adulthoodElement);

            child.Slot = BackstorySlot.Childhood;
            adult.Slot = BackstorySlot.Adulthood;

            child.FirstName = adult.FirstName = firstName;
            child.LastName = adult.LastName = lastName;
            child.NickName = adult.NickName = nickName;
            child.Gender = adult.Gender = gender;

            return true;
        }

        private static Backstory ReadResourceBackstory(XElement storyElem)
        {
            return new Backstory()
            {
                Title = storyElem.Element("title", true).Value,
                TitleFemale = storyElem.Element("titleFemale", true)?.Value,
                TitleShort = storyElem.Element("titleShort", true).Value,
                TitleShortFemale = storyElem.Element("titleShortFemale", true)?.Value,
                Description = storyElem.Element("baseDesc", true).Value.FixNewLines().Trim(),
                Slot = ParseSlot(storyElem)
            };
        }

        private static Backstory ReadTranslationBackstory(XElement storyElem)
        {
            return new Backstory()
            {
                Id = storyElem.Name.LocalName,
                Title = storyElem.Element("title", true).Value,
                TitleFemale = storyElem.Element("titleFemale", true)?.Value,
                TitleShort = storyElem.Element("titleShort", true).Value,
                TitleShortFemale = storyElem.Element("titleShortFemale", true)?.Value,
                Description = storyElem.Element("desc", true).Value.FixNewLines().Trim(),
                Slot = BackstorySlot.Unknown
            };
        }

        private static BackstorySlot ParseSlot(XElement storyElem)
        {
            XElement slotElem = storyElem.Element("slot", true);
            if (slotElem == null)
                return BackstorySlot.Unknown;

            return (BackstorySlot)Enum.Parse(typeof(BackstorySlot), slotElem.Value, true);
        }

        private static void ValidateSchema(object sender, ValidationEventArgs e)
        {
            throw new XmlSchemaValidationException(e.Message, e.Exception, e.Exception.LineNumber, e.Exception.LinePosition);
        }

        private static XElement GetElement(Backstory backstory)
        {
            XElement backstoryElement = new XElement(backstory.Id);
            XElement title = new XElement("title", backstory.Title);
            XElement titleFemale = new XElement("titleFemale", backstory.TitleFemale ?? backstory.Title);
            XElement titleShort = new XElement("titleShort", backstory.TitleShort);
            XElement titleShortFemale = new XElement("titleShortFemale", backstory.TitleShortFemale ?? backstory.TitleShort);
            XElement desc = new XElement("desc", backstory.Description);

            backstoryElement.Add(_newLine);
            backstoryElement.Add(_tab2, title, _newLine);
            backstoryElement.Add(_tab2, titleFemale, _newLine);
            backstoryElement.Add(_tab2, titleShort, _newLine);
            backstoryElement.Add(_tab2, titleShortFemale, _newLine);
            backstoryElement.Add(_tab2, desc, _newLine, _tab);

            return backstoryElement;
        }

        private static string GetHint(Backstory backstory)
        {
            StringBuilder hintBuilder = new StringBuilder();

            if (backstory.FirstName != null)
            {
                hintBuilder.Append($"{backstory.FirstName} ");

                if (backstory.NickName != null)
                    hintBuilder.Append($"\"{backstory.NickName}\" ");

                hintBuilder.Append($"{backstory.LastName}");
            }

            if (backstory.Gender != null)
                hintBuilder.Append($", {backstory.Gender}, ");

            hintBuilder.Append($"{backstory.Slot}");

            return hintBuilder.ToString();
        }
    }
}

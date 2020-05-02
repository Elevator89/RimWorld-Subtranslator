﻿using CommandLine;
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

            XDocument translatedBackstoriesDoc = XDocument.Load(options.TranslatedBackstoriesFile, LoadOptions.None);
            Dictionary<string, Backstory> translatedBackstories = translatedBackstoriesDoc.Root.Elements().Select(ReadTranslationBackstory).ToDictionary(backstory => backstory.Id);

            XDocument outputDoc = new XDocument();
            XElement root = new XElement("Backstories");
            outputDoc.Add(root);

            foreach (CategorizedBackstories categorizedBackstories in GetAllResourceBackstories(options.ResourcesDirectory))
            {
                outputDoc.Root.Add(_newLine, _tab, new XComment(categorizedBackstories.Category), _newLine);

                foreach (Backstory backstory in categorizedBackstories)
                {
                    if (translatedBackstories.TryGetValue(backstory.Id, out Backstory translatedBackstory))
                    {
                        outputDoc.Root.Add(_newLine, _tab, new XComment(GetHint(backstory)), _newLine);
                        outputDoc.Root.Add(_tab, GetElementWithEnglishComments(backstory, translatedBackstory), _newLine);
                    }
                }
            }

            outputDoc.Save(options.OutputBackstories, SaveOptions.None);
        }

        private static IEnumerable<CategorizedBackstories> GetAllResourceBackstories(string resourcesDirectory)
        {
            XmlSchemaSet backstoriesSchemaSet = new XmlSchemaSet();
            backstoriesSchemaSet.Add(XmlSchema.Read(new StringReader(Properties.Resources.BackstoriesSchema), ValidateSchema));

            XmlSchemaSet playerCreatedBiosSchemaSet = new XmlSchemaSet();
            playerCreatedBiosSchemaSet.Add(XmlSchema.Read(new StringReader(Properties.Resources.PlayerCreatedBiosSchema), ValidateSchema));

            foreach (string resourceFileName in Directory.EnumerateFiles(resourcesDirectory, "*.xml", SearchOption.TopDirectoryOnly))
            {
                XDocument doc = XDocument.Load(resourceFileName, LoadOptions.None);
                if (IsValid(doc, backstoriesSchemaSet))
                {
                    string category = Path.GetFileNameWithoutExtension(resourceFileName);
                    yield return new CategorizedBackstories(category, doc.Root.Elements().Select(ReadResourceBackstory));
                }
            }

            foreach (string resourceFileName in Directory.EnumerateFiles(resourcesDirectory, "*.xml", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(resourceFileName);

                XDocument doc = XDocument.Load(resourceFileName, LoadOptions.None);
                if (IsValid(doc, playerCreatedBiosSchemaSet))
                {
                    string category = Path.GetFileNameWithoutExtension(resourceFileName);
                    List<Backstory> solidBackstories = new List<Backstory>();

                    foreach (XElement pawnBioElement in doc.Root.Elements())
                    {
                        if (TryReadPawnBioBackstories(pawnBioElement, out Backstory childBackstory, out Backstory adultBackstory))
                        {
                            solidBackstories.Add(childBackstory);
                            solidBackstories.Add(adultBackstory);
                        }
                    }
                    yield return new CategorizedBackstories(category, solidBackstories);
                }
            }
        }

        private static bool IsValid(XDocument doc, XmlSchemaSet schemaSet)
        {
            bool isValid = true;

            doc.Validate(schemaSet, (object sender, ValidationEventArgs e) => isValid = false);
            return isValid;
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

            child.Id = Backstory.GetIdentifier(child);
            adult.Id = Backstory.GetIdentifier(adult);

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

            backstoryElement.Add(_newLine);
            backstoryElement.Add(_tab2, new XElement("title", backstory.Title), _newLine);
            backstoryElement.Add(_tab2, new XElement("titleFemale", backstory.TitleFemale ?? backstory.Title), _newLine);
            backstoryElement.Add(_tab2, new XElement("titleShort", backstory.TitleShort), _newLine);
            backstoryElement.Add(_tab2, new XElement("titleShortFemale", backstory.TitleShortFemale ?? backstory.TitleShort), _newLine);
            backstoryElement.Add(_tab2, new XElement("desc", backstory.Description), _newLine, _tab);

            return backstoryElement;
        }

        private static XElement GetElementWithEnglishComments(Backstory originalBackstory, Backstory translatedBackstory)
        {
            XElement backstoryElement = new XElement(translatedBackstory.Id);

            backstoryElement.Add(_newLine);

            backstoryElement.Add(_tab2, new XComment(originalBackstory.Title), _newLine);
            backstoryElement.Add(_tab2, new XElement("title", translatedBackstory.Title), _newLine);

            if (!string.IsNullOrEmpty(originalBackstory.TitleFemale))
                backstoryElement.Add(_tab2, new XComment(originalBackstory.TitleFemale), _newLine);
            backstoryElement.Add(_tab2, new XElement("titleFemale", translatedBackstory.TitleFemale ?? translatedBackstory.Title), _newLine);

            backstoryElement.Add(_tab2, new XComment(originalBackstory.TitleShort), _newLine);
            backstoryElement.Add(_tab2, new XElement("titleShort", translatedBackstory.TitleShort), _newLine);

            if (!string.IsNullOrEmpty(originalBackstory.TitleFemale))
                backstoryElement.Add(_tab2, new XComment(originalBackstory.TitleShortFemale), _newLine);
            backstoryElement.Add(_tab2, new XElement("titleShortFemale", translatedBackstory.TitleShortFemale ?? translatedBackstory.TitleShort), _newLine);

            backstoryElement.Add(_tab2, new XComment(originalBackstory.Description), _newLine);
            backstoryElement.Add(_tab2, new XElement("desc", translatedBackstory.Description), _newLine, _tab);

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

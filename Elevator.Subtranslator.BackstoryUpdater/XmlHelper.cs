using Elevator.Subtranslator.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Elevator.Subtranslator.BackstoryUpdater
{
    public static class XmlHelper
    {
        public static readonly XText NewLine = new XText(Environment.NewLine);
        public static readonly XText Tab = new XText("\t");
        public static readonly XText Tab2 = new XText("\t\t");

        public static IEnumerable<CategorizedBackstories> GetAllResourceBackstories(string resourcesDirectory)
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
                    yield return new CategorizedBackstories(category, doc.Root.Elements().Select(ReadBackstoryElementResource));
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

        public static bool IsValid(XDocument doc, XmlSchemaSet schemaSet)
        {
            bool isValid = true;

            doc.Validate(schemaSet, (object sender, ValidationEventArgs e) => isValid = false);
            return isValid;
        }

        public static bool TryReadPawnBioBackstories(XElement bioElem, out Backstory child, out Backstory adult)
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

            child = ReadBackstoryElementResource(childhoodElement);
            adult = ReadBackstoryElementResource(adulthoodElement);

            child.Slot = BackstorySlot.Childhood;
            adult.Slot = BackstorySlot.Adulthood;

            child.FirstName = adult.FirstName = firstName;
            child.LastName = adult.LastName = lastName;
            child.NickName = adult.NickName = nickName;
            child.Gender = adult.Gender = gender;

            return true;
        }

        public static Backstory ReadBackstoryElementResource(XElement storyElem)
        {
            Backstory backstory = new Backstory()
            {
                Title = storyElem.Element("title", true).Value,
                TitleFemale = storyElem.Element("titleFemale", true)?.Value,
                TitleShort = storyElem.Element("titleShort", true).Value,
                TitleShortFemale = storyElem.Element("titleShortFemale", true)?.Value,
                Description = storyElem.Element("baseDesc", true).Value,
                Slot = ParseSlot(storyElem)
            };

            //Like in RimWorld.Backstory.PostLoad
            backstory.Description = backstory.Description.TrimEnd();
            backstory.Description = backstory.Description.Replace("\\r", "\r");
            backstory.Description = backstory.Description.Replace("\\n", "\n");
            backstory.Description = backstory.Description.Replace("\r", "");

            backstory.Id = Backstory.GetIdentifier(backstory);

            backstory.Description = backstory.Description.FixNewLines().Trim();
            return backstory;
        }

        public static Backstory ReadBackstoryElementTranslated(XElement storyElem)
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

        public static XElement BuildBackstoryElementSimple(Backstory backstory)
        {
            XElement backstoryElement = new XElement(backstory.Id);

            backstoryElement.Add(NewLine);
            backstoryElement.Add(Tab2, new XElement("title", backstory.Title), NewLine);

            if (!string.IsNullOrEmpty(backstory.TitleFemale))
                backstoryElement.Add(Tab2, new XElement("titleFemale", backstory.TitleFemale), NewLine);

            backstoryElement.Add(Tab2, new XElement("titleShort", backstory.TitleShort), NewLine);

            if (!string.IsNullOrEmpty(backstory.TitleShortFemale))
                backstoryElement.Add(Tab2, new XElement("titleShortFemale", backstory.TitleShortFemale), NewLine);

            backstoryElement.Add(Tab2, new XElement("desc", backstory.Description), NewLine, Tab);

            return backstoryElement;
        }

        public static XElement BuildBackstoryElementTranslatedWithEnglishComments(Backstory originalBackstory, Backstory translatedBackstory)
        {
            XElement backstoryElement = new XElement(translatedBackstory.Id);

            backstoryElement.Add(NewLine);

            backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.Title} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("title", translatedBackstory.Title), NewLine);

            if (!string.IsNullOrEmpty(originalBackstory.TitleFemale))
            {
                backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleFemale} "), NewLine);
                backstoryElement.Add(Tab2, new XElement("titleFemale", translatedBackstory.TitleFemale ?? "TODO"), NewLine);
            }
            else if (!string.IsNullOrEmpty(translatedBackstory.TitleFemale))
                backstoryElement.Add(Tab2, new XElement("titleFemale", translatedBackstory.TitleFemale), NewLine);

            backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleShort} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("titleShort", translatedBackstory.TitleShort), NewLine);

            if (!string.IsNullOrEmpty(originalBackstory.TitleShortFemale))
            {
                backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleShortFemale} "), NewLine);
                backstoryElement.Add(Tab2, new XElement("titleShortFemale", translatedBackstory.TitleShortFemale ?? "TODO"), NewLine);
            }
            else if (!string.IsNullOrEmpty(translatedBackstory.TitleShortFemale))
                backstoryElement.Add(Tab2, new XElement("titleShortFemale", translatedBackstory.TitleShortFemale), NewLine);

            backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.Description} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("desc", translatedBackstory.Description), NewLine);
            backstoryElement.Add(Tab);

            return backstoryElement;
        }

        public static XElement BuildBackstoryElementTodoWithEnglishComments(Backstory originalBackstory)
        {
            XElement backstoryElement = new XElement(originalBackstory.Id);

            backstoryElement.Add(NewLine);

            backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.Title} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("title", "TODO"), NewLine);

            if (!string.IsNullOrEmpty(originalBackstory.TitleFemale))
                backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleFemale} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("titleFemale", "TODO"), NewLine);

            backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleShort} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("titleShort", "TODO"), NewLine);

            if (!string.IsNullOrEmpty(originalBackstory.TitleFemale))
                backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleShortFemale} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("titleShortFemale", "TODO"), NewLine);

            backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.Description} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("desc", "TODO"), NewLine);
            backstoryElement.Add(Tab);

            return backstoryElement;
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
    }
}

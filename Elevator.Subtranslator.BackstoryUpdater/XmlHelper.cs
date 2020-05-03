using Elevator.Subtranslator.Common;
using System;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Elevator.Subtranslator.BackstoryUpdater
{
    public static class XmlHelper
    {
        public static readonly XText NewLine = new XText(Environment.NewLine);
        public static readonly XText Tab = new XText("\t");
        public static readonly XText Tab2 = new XText("\t\t");

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
            backstory.Description = backstory.Description.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\r", "");

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
            XElement backstoryElement = new XElement(originalBackstory.Id);

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
            backstoryElement.Add(Tab2, new XElement("title", originalBackstory.Title), NewLine);

            if (!string.IsNullOrEmpty(originalBackstory.TitleFemale))
            {
                backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleFemale} "), NewLine);
                backstoryElement.Add(Tab2, new XElement("titleFemale", originalBackstory.TitleFemale), NewLine);
            }

            backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleShort} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("titleShort", originalBackstory.TitleShort), NewLine);

            if (!string.IsNullOrEmpty(originalBackstory.TitleFemale))
            {
                backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.TitleShortFemale} "), NewLine);
                backstoryElement.Add(Tab2, new XElement("titleShortFemale", originalBackstory.TitleShortFemale), NewLine);
            }

            backstoryElement.Add(Tab2, new XComment($" EN: {originalBackstory.Description} "), NewLine);
            backstoryElement.Add(Tab2, new XElement("desc", originalBackstory.Description), NewLine);
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
    }
}

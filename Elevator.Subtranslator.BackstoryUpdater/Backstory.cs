using System;

namespace Elevator.Subtranslator.BackstoryUpdater
{
    public class Backstory
    {
        public string Id;
        public string FirstName;
        public string LastName;
        public string NickName;
        public string Gender;
        public string Title;
        public string TitleFemale;
        public string TitleShort;
        public string TitleShortFemale;
        public string Description;
        public BackstorySlot Slot;

        public static string GetIdentifier(Backstory backstory)
        {
            if (!string.IsNullOrEmpty(backstory.Id))
                return backstory.Id;

            int num = Math.Abs(VerseGenTextMock.StableStringHash(backstory.Description) % 100);
            string s = backstory.Title.Replace('-', ' ');
            s = VerseGenTextMock.CapitalizedNoSpaces(s);
            return VerseGenTextMock.RemoveNonAlphanumeric(s) + num;
        }
    }
}

﻿using System;

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
		public string Category;

		public static string GetIdentifier(Backstory backstory)
		{
			if (!string.IsNullOrEmpty(backstory.Id))
				return backstory.Id;

			//Like in RimWorld.Backstory.PostLoad. Sensitive!
			string descriptionForHash = backstory.Description.TrimEnd().Replace("\\r", "").Replace("\\n", "\n");

			int num = Math.Abs(VerseGenTextMock.StableStringHash(descriptionForHash) % 100);
			string s = backstory.Title.Replace('-', ' ');
			s = VerseGenTextMock.CapitalizedNoSpaces(s);
			return VerseGenTextMock.RemoveNonAlphanumeric(s) + num;
		}

		public static bool IsSolid(Backstory backstory)
		{
			return !string.IsNullOrEmpty(backstory.FirstName);
		}
	}
}

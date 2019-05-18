using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Elevator.Subtranslator.BackstorySolidAnalyzer
{
	public class SolidStoryHiglighter
	{
		public XDocument HighlightSolidStories(XDocument origBackstories, List<SolidPawn> solidPawns)
		{
			XDocument backstories = new XDocument(origBackstories);
			XElement backstoryTranslations = backstories.Root;

			Dictionary<string, List<SolidPawn>> storyTagMap = GenerateStoryTagMap(solidPawns);

			XText tab = new XText(Environment.NewLine + "\t");
			XText newLine = new XText(Environment.NewLine);

			foreach (XElement backstory in backstoryTranslations.Elements())
			{
				XNode prevSibling = backstory.PreviousNode?.PreviousNode;
				bool hintExists = prevSibling != null && prevSibling is XProcessingInstruction;

				if (!hintExists && GenerateHint(backstory.Name.LocalName, storyTagMap, out XProcessingInstruction hint))
				{
					backstory.AddBeforeSelf(hint, tab);
				}
			}

			return backstories;
		}

		private bool GenerateHint(string backstoryTag, Dictionary<string, List<SolidPawn>> storyTagMap, out XProcessingInstruction hint)
		{
			List<SolidPawn> solidPawns;
			if (storyTagMap.TryGetValue(backstoryTag, out solidPawns))
			{
				hint = new XProcessingInstruction("solid", GenerateHintText(solidPawns));
				return true;
			}

			hint = null;
			return false;
		}

		private string GenerateHintText(List<SolidPawn> stories)
		{
			if (stories.Count == 0)
				return null;

			if (stories.Count == 1)
			{
				return string.Format("{0} ({1}) ", FormatName(stories[0]), stories[0].Gender);
			}

			StringBuilder sb = new StringBuilder();
			foreach (SolidPawn story in stories)
			{
				sb.AppendFormat("{0} ({1}); ", FormatName(story), story.Gender);
			}
			return sb.ToString();
		}

		private string FormatName(SolidPawn pawn)
		{
			return
				string.IsNullOrWhiteSpace(pawn.NickName)
				? string.Format("{0} {1}", pawn.FirstName, pawn.LastName)
				: string.Format("{0} \"{1}\" {2}", pawn.FirstName, pawn.NickName, pawn.LastName);
		}

		private static Dictionary<string, List<SolidPawn>> GenerateStoryTagMap(IEnumerable<SolidPawn> solidPawns)
		{
			Dictionary<string, List<SolidPawn>> storyMap = new Dictionary<string, List<SolidPawn>>(StringComparer.InvariantCultureIgnoreCase);

			foreach (SolidPawn pawn in solidPawns)
			{
				AddValueItem(storyMap, pawn.ChildhoodTag, pawn);
				AddValueItem(storyMap, pawn.AdulthoodTag, pawn);
			}

			return storyMap;
		}

		private static void AddValueItem<TKey, TValueItem>(Dictionary<TKey, List<TValueItem>> dictionary, TKey key, TValueItem item)
		{
			if (dictionary.ContainsKey(key))
			{
				dictionary[key].Add(item);
			}
			else
			{
				dictionary[key] = new List<TValueItem>() { item };
			}
		}

		private static IEnumerable<string> SplitPacalStyle(string input)
		{
			int prevCapIndex = 0;

			foreach (int capIndex in GetIndicesOfCapitalLetters(input))
			{
				if (capIndex > prevCapIndex)
				{
					yield return input.Substring(prevCapIndex, capIndex - prevCapIndex);
				}
				prevCapIndex = capIndex;
			}
			yield return input.Substring(prevCapIndex, input.Length - prevCapIndex);
		}

		private static IEnumerable<int> GetIndicesOfCapitalLetters(string input)
		{
			for (int i = 0; i < input.Length; ++i)
			{
				if (!char.IsLower(input[i]))
					yield return i;
			}
		}

		private static string TrimDigits(string input)
		{
			int indexOfFirstDigit = input.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
			return input.Substring(0, indexOfFirstDigit);
		}
	}
}

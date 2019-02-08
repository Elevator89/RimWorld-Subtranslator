using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Elevator.Subtranslator.BackstorySolidAnalyzer
{
	public class Analyzer
	{
		public IEnumerable<SolidPawn> GetSolidPawns(XDocument creations)
		{
			XElement root = creations.Root;

			foreach (XElement pawnBio in root.Elements("PawnBio"))
			{
				List<XElement> elements = pawnBio.Elements().ToList();
				XElement name = pawnBio.Element("Name");
				XElement gender = pawnBio.Element("Gender");
				XElement childhood = pawnBio.Element("Childhood");
				XElement adulthood = pawnBio.Element("Adulthood");

				if (name != null && gender != null && childhood != null && adulthood != null)
				{
					yield return new SolidPawn
					{
						FirstName = name.Element("First").Value.Trim(),
						LastName = name.Element("Last").Value.Trim(),
						NickName = name.Element("Nick").Value.Trim(),
						Gender = gender.Value,
						ChildhoodTag = null,
						ChildhoodTitle = childhood.Element("Title").Value.Trim(),
						ChildhoodDescription = childhood.Element("BaseDesc").Value.Trim(),
						AdulthoodTag = null,
						AdulthoodTitle = adulthood.Element("Title").Value.Trim(),
						AdulthoodDescription = adulthood.Element("BaseDesc").Value.Trim(),
					};
				}
			}
		}

		public List<SolidPawn> FillSolidPawnTags(XDocument cleanBackstories, List<SolidPawn> solidPawns)
		{
			XElement root = cleanBackstories.Root;

			Dictionary<string, SolidPawn> childhoodDescMap = new Dictionary<string, SolidPawn>();
			Dictionary<string, SolidPawn> adulthoodDescMap = new Dictionary<string, SolidPawn>();

			foreach (SolidPawn solidPawn in solidPawns)
			{
				childhoodDescMap[solidPawn.ChildhoodDescription] = solidPawn;
				adulthoodDescMap[solidPawn.AdulthoodDescription] = solidPawn;
			}

			foreach (XElement backstory in root.Elements())
			{
				string backstoryTag = backstory.Name.LocalName;
				string backstoryTitle = backstory.Element("title").Value.Trim();
				string backstoryDesc = backstory.Element("desc").Value.Trim();

				SolidPawn solidPawn;
				if (childhoodDescMap.TryGetValue(backstoryDesc, out solidPawn))
				{
					solidPawn.ChildhoodTag = backstoryTag;
				}
				else if (adulthoodDescMap.TryGetValue(backstoryDesc, out solidPawn))
				{
					solidPawn.AdulthoodTag = backstoryTag;
				}
			}
			return solidPawns;
		}
	}
}

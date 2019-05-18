using Fclp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Elevator.Subtranslator.BackstoryReindexer
{
	class Program
	{
		public class ApplicationArguments
		{
			public string OldOrigBackstoriesFileName { get; set; }
			public string NewOrigBackstoriesFileName { get; set; }
			public string TranslatedBackstoriesInputFileName { get; set; }
			public string TranslatedBackstoriesOutputFileName { get; set; }
		}

		static void Main(string[] args)
		{
			// create a generic parser for the ApplicationArguments type
			var p = new FluentCommandLineParser<ApplicationArguments>();

			// specify which property the value will be assigned too.
			p.Setup(arg => arg.OldOrigBackstoriesFileName)
			 .As('l', "old") // define the short and long option name
			 .Required() // using the standard fluent Api to declare this Option as required.
			 .WithDescription("Old backstories original file path");           // specify which property the value will be assigned too.

			p.Setup(arg => arg.NewOrigBackstoriesFileName)
			 .As('n', "new")
			 .Required()
			 .WithDescription("New backstories original file path");

			p.Setup(arg => arg.TranslatedBackstoriesInputFileName)
			 .As('t', "translated")
			 .Required()
			 .WithDescription("Translated backstories input file path");

			p.Setup(arg => arg.TranslatedBackstoriesOutputFileName)
			 .As('o', "output")
			 .Required()
			 .WithDescription("Translated backstories output file path");

			ICommandLineParserResult result = p.Parse(args);

			if (result.HasErrors)
			{
				Console.WriteLine(result.ErrorText);
				return;
			}

			XDocument oldOrigStoriesDoc = XDocument.Load(p.Object.OldOrigBackstoriesFileName);
			XDocument newOrigStoriesDoc = XDocument.Load(p.Object.NewOrigBackstoriesFileName);
			XDocument oldTransStoriesDoc = XDocument.Load(p.Object.TranslatedBackstoriesInputFileName, LoadOptions.PreserveWhitespace);

			List<Story> oldStories = GetStories(oldOrigStoriesDoc).ToList();
			List<Story> newStories = GetStories(newOrigStoriesDoc).ToList();

			Dictionary<string, string> tagMap = new Dictionary<string, string>();

			StoryComparer comparer = new StoryComparer();

			HashSet<string> processedOld = new HashSet<string>();
			HashSet<string> processedNew = new HashSet<string>();

			Console.WriteLine("Renamed stories:");
			foreach (Story oldStory in oldStories)
			{
				if (tagMap.ContainsKey(oldStory.Name))
				{
					Console.WriteLine("OLD {0} story already exists in map. It corresponds to NEW {1}", oldStory.Name, tagMap[oldStory.Name]);
					continue;
				}

				foreach (Story newStory in newStories)
				{
					if (comparer.Equals(oldStory, newStory))
					{
						if (tagMap.ContainsValue(newStory.Name))
						{
							Console.WriteLine("NEW {0} story already exists in map", newStory.Name);
							continue;
						}

						if (oldStory.Name != newStory.Name)
						{
							Console.WriteLine("{0} -> {1}", oldStory.Name, newStory.Name);
							tagMap[oldStory.Name] = newStory.Name;
						}

						processedOld.Add(oldStory.Name);
						processedNew.Add(newStory.Name);
					}
				}

			}

			Console.WriteLine();

			Console.WriteLine("Unmapped old stories:");
			foreach (Story oldStory in oldStories.Where(s => !processedOld.Contains(s.Name)))
			{
				Console.WriteLine(oldStory.Name);
			}

			Console.WriteLine();

			Console.WriteLine("Unmapped new stories:");
			foreach (Story newStory in newStories.Where(s => !processedNew.Contains(s.Name)))
			{
				Console.WriteLine(newStory.Name);
			}

			RenameStories(oldTransStoriesDoc, tagMap);
			AddNewStories(oldTransStoriesDoc, newStories.Where(s => !processedNew.Contains(s.Name)));

			oldTransStoriesDoc.Save(p.Object.TranslatedBackstoriesOutputFileName, SaveOptions.None);
		}

		private static void AddNewStories(XDocument translatedStories, IEnumerable<Story> stories)
		{
			XElement root = translatedStories.Root;

			HashSet<string> existingStories = new HashSet<string>(root.Elements().Select(elem => elem.Name.LocalName));

			XElement last = root.Elements().Last();

			foreach (Story story in stories)
			{
				if (existingStories.Contains(story.Name))
					continue;

				XText newLine = new XText(Environment.NewLine);
				XText tab = new XText("\t");
				XText tab2 = new XText("\t\t");

				XElement newElement = new XElement(XName.Get(story.Name, last.Name.NamespaceName));
				XElement title = new XElement("title", story.Title);
				XElement titleFemale = new XElement("titleFemale", story.Title);
				XElement titleShort = new XElement("titleShort", story.TitleShort);
				XElement titleShortFemale = new XElement("titleShortFemale", story.TitleShort);
				XElement desc = new XElement("desc", story.Description);

				newElement.Add(tab2, title, newLine);
				newElement.Add(tab2, titleFemale, newLine);
				newElement.Add(tab2, titleShort, newLine);
				newElement.Add(tab2, titleShortFemale, newLine);
				newElement.Add(tab2, desc, newLine, tab);

				root.Add(tab, newElement, newLine, newLine);
			}
		}

		private static void RenameStories(XDocument translatedStories, Dictionary<string, string> tagMap)
		{
			XElement root = translatedStories.Root;

			foreach (XElement element in root.Elements().ToArray())
			{
				string newName;
				if (tagMap.TryGetValue(element.Name.LocalName, out newName))
				{
					XElement newElement = new XElement(XName.Get(newName, element.Name.NamespaceName));
					newElement.Add(element.Nodes());
					element.ReplaceWith(newElement);

					//RenameElement(storyElem, newName);
				}
			}
		}

		private static void RenameElement(XElement element, string newName)
		{
			XElement newElement = new XElement(XName.Get(newName, element.Name.NamespaceName));
			if (element.HasAttributes)
			{
				IEnumerable<XAttribute> attribs = element.Attributes();
				foreach (var attrib in attribs)
				{
					newElement.Add(new XAttribute(attrib));
				}
			}
			newElement.Add(element.Elements());
			element.ReplaceWith(newElement);
		}

		private static IEnumerable<Story> GetStories(XDocument storiesDoc)
		{
			XElement root = storiesDoc.Root;

			foreach (XElement storyElem in root.Elements())
			{
				yield return ReadStory(storyElem);
			}
		}


		private static Story ReadStory(XElement storyElem)
		{
			return new Story()
			{
				Name = storyElem.Name.LocalName,
				Title = storyElem.Element("title").Value,
				TitleShort = storyElem.Element("titleShort").Value,
				Description = storyElem.Element("desc").Value
			};
		}
	}
}

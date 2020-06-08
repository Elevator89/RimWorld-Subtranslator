using System;
using Fclp;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Elevator.Subtranslator.BackstorySolidAnalyzer
{
	class Program
	{
		public class ApplicationArguments
		{
			public string CreationsFileName { get; set; }
			public string BackstoriesInputFileName { get; set; }
			public string TranslatedBackstoriesInputFileName { get; set; }
			public string BackstoriesOutputFileName { get; set; }
		}

		static void Main(string[] args)
		{
			// create a generic parser for the ApplicationArguments type
			var p = new FluentCommandLineParser<ApplicationArguments>();

			// specify which property the value will be assigned too.
			p.Setup(arg => arg.CreationsFileName)
			 .As('c', "creations") // define the short and long option name
			 .Required() // using the standard fluent Api to declare this Option as required.
			 .WithDescription("Rimworld creations file path");		   // specify which property the value will be assigned too.

			p.Setup(arg => arg.BackstoriesInputFileName)
			 .As('b', "backstories") // define the short and long option name
			 .Required() // using the standard fluent Api to declare this Option as required.
			 .WithDescription("Backstories input file path");

			p.Setup(arg => arg.TranslatedBackstoriesInputFileName)
			 .As('t', "translated") // define the short and long option name
			 .Required() // using the standard fluent Api to declare this Option as required.
			 .WithDescription("Translated backstories input file path");

			p.Setup(arg => arg.BackstoriesOutputFileName)
			 .As('o', "output")
			 .Required()
			 .WithDescription("Backstories output file path");

			ICommandLineParserResult result = p.Parse(args);

			if (result.HasErrors)
			{
				Console.WriteLine(result.ErrorText);
				return;
			}

			XDocument solidPawnsDoc = XDocument.Load(p.Object.CreationsFileName, LoadOptions.PreserveWhitespace);
			XDocument origStoriesDoc = XDocument.Load(p.Object.BackstoriesInputFileName, LoadOptions.PreserveWhitespace);
			XDocument transStoriesDoc = XDocument.Load(p.Object.TranslatedBackstoriesInputFileName, LoadOptions.PreserveWhitespace);

			Analyzer analyzer = new Analyzer();
			SolidStoryHiglighter highlighter = new SolidStoryHiglighter();

			List<SolidPawn> solidPawns = analyzer.GetSolidPawns(solidPawnsDoc).ToList();
			solidPawns = analyzer.FillSolidPawnTags(origStoriesDoc, solidPawns);

			XDocument outputStories = highlighter.HighlightSolidStories(transStoriesDoc, solidPawns);
			outputStories.Save(p.Object.BackstoriesOutputFileName, SaveOptions.None);
		}

		static void FillWithNewElements(XDocument recipient, XDocument donor)
		{
			HashSet<string> tagsDonor = new HashSet<string>();
			HashSet<string> tagsRecipient = new HashSet<string>();

			foreach (XElement story in donor.Root.Elements())
			{
				tagsDonor.Add(story.Name.LocalName);
			}

			foreach (XElement story in recipient.Root.Elements())
			{
				tagsRecipient.Add(story.Name.LocalName);
			}

			tagsRecipient.IntersectWith(tagsDonor);
			tagsDonor.ExceptWith(tagsRecipient);

			foreach (string newTag in tagsDonor)
			{
				XElement newElement = new XElement(donor.Root.Element(newTag));
				recipient.Root.Add(newElement);
				recipient.Root.Add(Environment.NewLine);
				recipient.Root.Add(Environment.NewLine);
			}
		}

		static void RemoveOldElements(XDocument recipient, XDocument donor)
		{
			HashSet<string> tagsDonor = new HashSet<string>();
			HashSet<string> tagsRecipient = new HashSet<string>();

			foreach (XElement story in donor.Root.Elements())
			{
				tagsDonor.Add(story.Name.LocalName);
			}

			foreach (XElement story in recipient.Root.Elements())
			{
				tagsRecipient.Add(story.Name.LocalName);
			}

			tagsDonor.IntersectWith(tagsRecipient);
			tagsRecipient.ExceptWith(tagsDonor);

			foreach (string oldTag in tagsRecipient)
			{
				recipient.Root.Element(oldTag).Remove();
			}
		}
	}
}

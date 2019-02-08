using Fclp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Elevator.Subtranslator.BackstoryIssueGenerator
{
	class Program
	{
		public class ApplicationArguments
		{
			public string BackstoriesInputFileName { get; set; }
			public int StoriesPerIssue { get; set; }
			public string IssuesOutputFileName { get; set; }
		}

		static void Main(string[] args)
		{
			// create a generic parser for the ApplicationArguments type
			var p = new FluentCommandLineParser<ApplicationArguments>();

			p.Setup(arg => arg.BackstoriesInputFileName)
			 .As('b', "backstories") // define the short and long option name
			 .Required() // using the standard fluent Api to declare this Option as required.
			 .WithDescription("Backstories input file path");

			p.Setup(arg => arg.StoriesPerIssue)
			 .As('n', "number") // define the short and long option name
			 .Required() // using the standard fluent Api to declare this Option as required.
			 .WithDescription("Number of stories per issue");

			p.Setup(arg => arg.IssuesOutputFileName)
			 .As('o', "output")
			 .Required()
			 .WithDescription("Issue headers output file path");

			ICommandLineParserResult result = p.Parse(args);

			if (result.HasErrors)
			{
				Console.WriteLine(result.ErrorText);
				return;
			}

			XDocument document = XDocument.Parse(File.ReadAllText(p.Object.BackstoriesInputFileName));
			string[] issueHeaders = GenerateIssueHeaders(document, p.Object.StoriesPerIssue);
			File.WriteAllLines(p.Object.IssuesOutputFileName, issueHeaders);
		}

		private static string[] GenerateIssueHeaders(XDocument backstories, int storiesPerIssue)
		{
			List<string> issueHeaders = new List<string>();

			XElement backstoryTranslations = backstories.Root;
			int totalCounter = 0;

			foreach (XComment comment in backstoryTranslations.Nodes().Where(node => node.NodeType == XmlNodeType.Comment))
			{
				string commentXml = "<Commented>" + comment.Value + "</Commented>"; //Create fake root element for paser
				try
				{
					XElement commentedElement = XElement.Parse(commentXml, LoadOptions.PreserveWhitespace);

					string firstStory = null;
					string currentStory = null;

					int counter = 0;

					foreach (XElement backstory in commentedElement.Elements())
					{
						currentStory = backstory.Name.LocalName;
						if (counter == 0)
						{
							firstStory = currentStory;
						}
						if (counter == storiesPerIssue - 1)
						{
							issueHeaders.Add(string.Format("Backstories: {0}..{1}", firstStory, currentStory));
							counter = 0;
						}
						else
						{
							counter++;
						}
						totalCounter++;
					}

					if (counter > 0)
					{
						issueHeaders.Add(string.Format("Backstories: {0}..{1}", firstStory, currentStory));
					}
				}
				catch
				{
					continue;
				}
			}

			return issueHeaders.ToArray();
		}

	}
}

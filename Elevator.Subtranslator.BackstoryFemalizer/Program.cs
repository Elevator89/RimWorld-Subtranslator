using CommandLine;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Elevator.Subtranslator.BackstoryFemalizer
{
	class Options
	{
		[Option('t', "target", Required = true, HelpText = "Path to the original backstories file.")]
		public string TargetFile { get; set; }

		[Option('b', "base", Required = false, DefaultValue = "", HelpText = "Backstory to start from.")]
		public string Base { get; set; }

		[Option('c', "count", Required = false, DefaultValue = -1, HelpText = "Count of processed backstories.")]
		public int Count { get; set; }

		[Option('o', "output", Required = true, HelpText = "Path to the output file.")]
		public string OutputFile { get; set; }
	}

	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any())
				return;

			Options options = parseResult.Value;

			XDocument backstoriesDoc = XDocument.Load(options.TargetFile, LoadOptions.PreserveWhitespace);

			if (string.IsNullOrEmpty(options.Base))
			{
				foreach (XElement backstory in backstoriesDoc.Root.Elements())
				{
					FemalizeBackstory(backstory);
				}
			}
			else
			{
				XElement first = backstoriesDoc.Root.Element(options.Base);
				FemalizeBackstory(first);

				IEnumerable<XElement> elementsAfterFirst = first.ElementsAfterSelf();

				if (options.Count == -1)
				{
					foreach (XElement backstory in elementsAfterFirst)
					{
						FemalizeBackstory(backstory);
					}
				}
				else
				{
					int count = 1;
					IEnumerator<XElement> elementsAfterFirstEnum = elementsAfterFirst.GetEnumerator();
					while (elementsAfterFirstEnum.MoveNext() && count < options.Count)
					{
						FemalizeBackstory(elementsAfterFirstEnum.Current);
						count++;
					}
				}
			}

			backstoriesDoc.Save(options.OutputFile);
		}

		private static void FemalizeBackstory(XElement backstory)
		{
			if (backstory.Element("titleFemale") == null)
			{
				XElement title = backstory.Element("title");
				title.AddAfterSelf(new XElement("titleFemale", title.Value));
				title.AddAfterSelf(new XText(title.PreviousNode as XText));
			}
			if (backstory.Element("titleShortFemale") == null)
			{
				XElement titleShort = backstory.Element("titleShort");
				titleShort.AddAfterSelf(new XElement("titleShortFemale", titleShort.Value));
				titleShort.AddAfterSelf(new XText(titleShort.PreviousNode as XText));
			}
		}
	}
}

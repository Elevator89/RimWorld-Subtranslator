using CommandLine;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Elevator.Subtranslator.ListInjectionCompiler
{
	class Options
	{
		[Option('d', "draft", Required = true, HelpText = "Path to draft file.")]
		public string DraftFile { get; set; }

		[Option('x', "xml", Required = true, HelpText = "Path to output XML file.")]
		public string XmlFile { get; set; }
	}

	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any()) return;

			Options options = parseResult.Value;

			XDocument output = new XDocument();

			using (StreamWriter writer = File.CreateText(options.XmlFile))
			{
				string[] lines = File.ReadAllLines(options.DraftFile);
				string blockHeaderName = null;
				int lineInBlock = -1;

				for (int lineIndex = 0; lineIndex < lines.Length; ++lineIndex)
				{
					string line = lines[lineIndex];

					if (string.IsNullOrWhiteSpace(line))
					{
						writer.WriteLine();
					}
					else if (IsTag(line))
					{
						writer.WriteLine(line);
					}
					else if (IsBlockHeader(line))
					{
						blockHeaderName = line.Substring(0, line.LastIndexOf(':')).Trim();
						lineInBlock = 0;
					}
					else
					{
						writer.WriteLine("\t<{0}.{1}>{2}</{0}.{1}>", blockHeaderName, lineInBlock, line.Trim());
						lineInBlock++;
					}
				}
			}
		}

		private static bool IsBlockHeader(string line)
		{
			return line.TrimEnd().EndsWith(":");
		}

		private static bool IsTag(string line)
		{
			string trimmed = line.Trim();
			return trimmed.StartsWith("<") && trimmed.EndsWith(">");
		}

		private static int GetLineNesting(string line)
		{
			for (int i = 0; i < line.Length; ++i)
			{
				if (line[i] != 9) return i;
			}
			return -1;
		}
	}
}

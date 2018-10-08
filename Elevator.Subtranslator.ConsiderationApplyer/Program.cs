using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Elevator.Subtranslator.ConsiderationApplyer
{
	class Options
	{
		[Option('r', "report", Required = true, HelpText = "Path to translation report file.")]
		public string ReportPath { get; set; }

		[Option('t', "target", Required = true, HelpText = "Path to folder of target localization.")]
		public string TargetPath { get; set; }

		[Option('o', "output", Required = true, HelpText = "Path to output localization folder.")]
		public string OutputPath { get; set; }
	}

	class Consideration
	{
		public string FileName;
		public string TargetLine;
		public string Replacement;
	}

	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any())
				return;

			Options options = parseResult.Value;

			string report = File.ReadAllText(options.ReportPath);

			List<Consideration> considerations = new List<Consideration>();

			//Consider using PrisonCell.stages.wondrously_impressive_prison_cell.description instead of PrisonCell.stages.9.description (Thoughts_Situation_RoomStats.xml)
			Regex considerationRegex = new Regex(@"Consider using (.*?) instead of (.*?) \((.*?)\)");

			MatchCollection matches = considerationRegex.Matches(report);
			foreach (Match match in matches)
			{
				Consideration consideration = new Consideration
				{
					FileName = match.Groups[3].Value,
					TargetLine = match.Groups[2].Value,
					Replacement = match.Groups[1].Value,
				};

				considerations.Add(consideration);
			}

			Dictionary<string, IGrouping<string, Consideration>> considerationMap = considerations.GroupBy(c => c.FileName).ToDictionary(cg => cg.Key);

			DirectoryInfo targetDir = new DirectoryInfo(options.TargetPath);
			foreach (FileInfo file in targetDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
			{
				IGrouping<string, Consideration> fileConsiderations;
				if (considerationMap.TryGetValue(file.Name, out fileConsiderations))
				{
					string fileContents = File.ReadAllText(file.FullName);

					foreach (Consideration consideration in fileConsiderations)
					{
						fileContents = fileContents.Replace(consideration.TargetLine, consideration.Replacement);
					}

					File.WriteAllText(file.FullName, fileContents, Encoding.UTF8);
				}
			}
		}
	}
}

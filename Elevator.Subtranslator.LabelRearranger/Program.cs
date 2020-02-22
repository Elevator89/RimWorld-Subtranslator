using CommandLine;
using Elevator.Subtranslator.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elevator.Subtranslator.LabelRearranger
{
	class Options
	{
		[Option('d', "defs", Required = true, HelpText = "Path to translation DefInjected folder.")]
		public string DefsPath { get; set; }

		[Option('t', "def types", DefaultValue = "", Required = false, HelpText = "Use only these def types.")]
		public string DefsTypes { get; set; }

		[Option('m', "male", Required = true, HelpText = "Path to the file with male-gendered nouns.")]
		public string MaleGenderFile { get; set; }

		[Option('f', "female", Required = true, HelpText = "Path to the file with female-gendered nouns.")]
		public string FemaleGenderFile { get; set; }

		[Option('n', "neuter", Required = true, HelpText = "Path to the file with neuter-gendered nouns.")]
		public string NeuterGenderFile { get; set; }

		[Option('p', "plural", Required = true, HelpText = "Path to the file with plural-only nouns.")]
		public string PluralGenderFile { get; set; }

		[Option('i', "ignored", Required = true, HelpText = "Path to the file with nouns, for which gender is ignored.")]
		public string IgnoreFile { get; set; }

		[Option('o', "output", Required = true, HelpText = "Fule for output.")]
		public string OutputFile { get; set; }
	}

    /// <summary>
    /// Reads files categorized by gender, analyzes which Def category they belong, and writes them back
    /// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any())
				return;

			Options options = parseResult.Value;

			InjectionAnalyzer analyzer = new InjectionAnalyzer();

			HashSet<string> defTypes = new HashSet<string>(options.DefsTypes.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));

			Injection[] injections = analyzer
				.ReadInjections(options.DefsPath)
				.Where(inj => defTypes.Contains(inj.DefType))
				.Where(inj => GetLastPart(inj).ToLowerInvariant().Contains("label"))
				//.Where(inj => !ignoredLabels.Contains(inj.Translation))
				.ToArray();

			Dictionary<string, int> categoryStats = MergeDictionarites(
				GetCategoriesDict(File.ReadAllLines(options.MaleGenderFile), injections),
				GetCategoriesDict(File.ReadAllLines(options.FemaleGenderFile), injections),
				GetCategoriesDict(File.ReadAllLines(options.NeuterGenderFile), injections),
				GetCategoriesDict(File.ReadAllLines(options.PluralGenderFile), injections));

			//IEnumerable<string> categories =
			//	GetCategories(File.ReadAllLines(options.MaleGenderFile), allLabels)
			//	.Union(GetCategories(File.ReadAllLines(options.FemaleGenderFile), allLabels))
			//	.Union(GetCategories(File.ReadAllLines(options.NeuterGenderFile), allLabels))
			//	.Union(GetCategories(File.ReadAllLines(options.PluralGenderFile), allLabels))
			//	.Distinct();

			File.WriteAllLines(options.OutputFile, categoryStats.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

			File.WriteAllLines(options.IgnoreFile, WriteGroups(GroupByCategory(File.ReadAllLines(options.IgnoreFile), injections)));

			File.WriteAllLines(options.MaleGenderFile, WriteGroups(GroupByCategory(File.ReadAllLines(options.MaleGenderFile), injections)));
			File.WriteAllLines(options.FemaleGenderFile, WriteGroups(GroupByCategory(File.ReadAllLines(options.FemaleGenderFile), injections)));
			File.WriteAllLines(options.NeuterGenderFile, WriteGroups(GroupByCategory(File.ReadAllLines(options.NeuterGenderFile), injections)));
			File.WriteAllLines(options.PluralGenderFile, WriteGroups(GroupByCategory(File.ReadAllLines(options.PluralGenderFile), injections)));
		}

		private static Dictionary<string, int> MergeDictionarites(params Dictionary<string, int>[] dictionaries)
		{
			Dictionary<string, int> result = new Dictionary<string, int>();
			foreach (Dictionary<string, int> dictionary in dictionaries)
			{
				foreach (KeyValuePair<string, int> kvp in dictionary)
				{
					if (!result.ContainsKey(kvp.Key))
					{
						result[kvp.Key] = kvp.Value;
					}
					else
					{
						result[kvp.Key] += kvp.Value;
					}

				}
			}
			return result;
		}

		private static IEnumerable<string> GetCategories(IEnumerable<string> lines, Injection[] injections)
		{
			return GroupByCategory(lines, injections).Select(group => group.Key);
		}

		private static Dictionary<string, int> GetCategoriesDict(IEnumerable<string> lines, Injection[] injections)
		{
			return GroupByCategory(lines, injections).ToDictionary(g => g.Key, g => g.Count());
		}

		private static IEnumerable<string> WriteGroupsSort(IEnumerable<IGrouping<string, string>> groups)
		{
			foreach (IGrouping<string, string> group in groups)
			{
				if (group.Count() == 0)
					continue;

				yield return string.Empty;
				yield return $"// {group.Key}";

				foreach (string line in group.OrderBy(line => line))
				{
					yield return line;
				}
			}
		}

		private static IEnumerable<string> WriteGroups(IEnumerable<IGrouping<string, string>> groups)
		{
			foreach (IGrouping<string, string> group in groups)
			{
				if (group.Count() == 0)
					continue;

				yield return string.Empty;
				yield return $"// {group.Key}";

				foreach (string line in group)
				{
					yield return line;
				}
			}
		}

		private static IEnumerable<IGrouping<string, string>> GroupByCategory(IEnumerable<string> lines, Injection[] injections)
		{
			return lines
				.Distinct()
				.Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
				.GroupBy(line => GetLineCategory(line, injections));

		}

		private static IEnumerable<string> ArrangeLines(IEnumerable<string> lines, Injection[] injections)
		{
			string prevCategory = "";

			foreach (string line in lines)
			{
				if (string.IsNullOrWhiteSpace(line))
					continue;

				string category = GetLineCategory(line, injections);

				if (string.IsNullOrEmpty(category))
				{
					Console.WriteLine("No category for line {0}", line);
					continue;
				}

				if (category != prevCategory)
				{
					yield return string.Empty;
					yield return $"// {category}";

					prevCategory = category;
				}

				yield return line;
			}
		}

		private static string GetLineCategory(string line, Injection[] injections)
		{
			Injection foundInjection = injections.FirstOrDefault(inj => inj.Translation.ToLowerInvariant() == line.ToLowerInvariant());

			if (foundInjection == null)
			{
				return string.Empty;
			}

			return foundInjection.DefType;
		}

		private static string GetLastPart(Injection injection)
		{
			return injection.DefPathParts[injection.DefPathParts.Length - 1];
		}
	}
}

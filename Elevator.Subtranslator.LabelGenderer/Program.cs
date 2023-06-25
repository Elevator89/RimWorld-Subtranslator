using CommandLine;
using Elevator.Subtranslator.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elevator.Subtranslator.LabelGenderer
{
	enum Option { Unknown, Male = 1, Female = 2, Neuter = 3, Plural = 4, Ignore }

	class Options
	{
		[Option('d', "defsPath", Required = true, HelpText = "Path to translation DefInjected folder.")]
		public string DefsPath { get; set; }

		[Option('t', "defTypes", DefaultValue = "", Required = false, HelpText = "Use only these def types.")]
		public string DefsTypes { get; set; }

		[Option('m', "maleFile", Required = true, HelpText = "Path to the file with male-gendered nouns.")]
		public string MaleGenderFile { get; set; }

		[Option('f', "femaleFile", Required = true, HelpText = "Path to the file with female-gendered nouns.")]
		public string FemaleGenderFile { get; set; }

		[Option('n', "neuterFile", Required = true, HelpText = "Path to the file with neuter-gendered nouns.")]
		public string NeuterGenderFile { get; set; }

		[Option('p', "pluralFile", Required = true, HelpText = "Path to the file with plural-only nouns.")]
		public string PluralGenderFile { get; set; }

		[Option('i', "ignoreFile", Required = true, HelpText = "Path to the file with nouns, for which gender is ignored.")]
		public string IgnoreFile { get; set; }
	}

	/// <summary>
	/// Reads def injections' labels and allows user to specify which gender each of them is
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

			HashSet<string> maleLabels = File.Exists(options.MaleGenderFile) ? new HashSet<string>(File.ReadAllLines(options.MaleGenderFile)) : new HashSet<string>();
			HashSet<string> femaleLabels = File.Exists(options.FemaleGenderFile) ? new HashSet<string>(File.ReadAllLines(options.FemaleGenderFile)) : new HashSet<string>();
			HashSet<string> neuterLabels = File.Exists(options.NeuterGenderFile) ? new HashSet<string>(File.ReadAllLines(options.NeuterGenderFile)) : new HashSet<string>();
			HashSet<string> pluralLabels = File.Exists(options.PluralGenderFile) ? new HashSet<string>(File.ReadAllLines(options.PluralGenderFile)) : new HashSet<string>();
			HashSet<string> ignoredInjections = File.Exists(options.IgnoreFile) ? new HashSet<string>(File.ReadAllLines(options.IgnoreFile)) : new HashSet<string>();

			Injection[] allLabels = analyzer
				.ReadInjections(options.DefsPath)
				.Where(inj => defTypes.Contains(inj.DefType))
				.Where(inj => GetLastPart(inj).ToLowerInvariant().Contains("label"))
				.Distinct(new InjectionTypeTranslationComparer())
				.Where(inj =>
					   !ignoredInjections.Contains(FormInjectionLine(inj))
					&& !maleLabels.Contains(inj.Translation)
					&& !femaleLabels.Contains(inj.Translation)
					&& !neuterLabels.Contains(inj.Translation)
					&& !pluralLabels.Contains(inj.Translation))
				.ToArray();

			Console.WriteLine($"Enter genders for {allLabels.Length} labels.");
			Console.WriteLine("1 - Male, 2 - Female, 3 - Neuter, 4 - Plural, 0 - ignore label, <ENTER> - skip");

			List<Option> history = new List<Option>();

			string prevDefType = "";

			bool[] newDefFlags = new bool[6] { true, true, true, true, true, true };

			for (int labelIndex = 0; labelIndex < allLabels.Length; ++labelIndex)
			{
				Injection injection = allLabels[labelIndex];
				string label = injection.Translation;

				Console.WriteLine();
				Console.Write($"{labelIndex + 1}/{allLabels.Length} {injection.DefType} <{injection.DefPath}> \"{label}\": ");

				ConsoleKeyInfo keyInfo = Console.ReadKey();

				if (injection.DefType != prevDefType)
				{
					for (int i = (int)Option.Male; i <= (int)Option.Plural; ++i)
						newDefFlags[i] = true;

					prevDefType = injection.DefType;
				}

				switch (keyInfo.Key)
				{
					case ConsoleKey.Escape:
						return;

					case ConsoleKey.Backspace:
						string prevFileName = GetFileForOption(options, history[labelIndex - 1]);
						FileUtil.DeleteLine(prevFileName);
						history.RemoveAt(labelIndex - 1);
						labelIndex -= 2;
						break;

					case ConsoleKey.Enter:
						break;

					default:
						Option option = ParseInput(keyInfo.KeyChar);

						if (option == Option.Unknown)
						{
							--labelIndex;
						}
						else
						{
							string fileName = GetFileForOption(options, option);

							if (option == Option.Ignore)
							{
								FileUtil.PushLine(fileName, FormInjectionLine(injection));
							}
							else
							{
								if (newDefFlags[(int)option])
								{
									FileUtil.PushLine(fileName, string.Empty);
									FileUtil.PushLine(fileName, "// " + injection.DefType);
									newDefFlags[(int)option] = false;
								}
								FileUtil.PushLine(fileName, label);
							}

							history.Add(option);
						}
						break;
				}
			}
		}

		private static string FormInjectionLine(Injection inj)
		{
			return $"{inj.DefType} {inj.DefPath}: {inj.Translation}";
		}

		private static Option ParseInput(char input)
		{
			switch (input)
			{
				case '1':
					return Option.Male;
				case '2':
					return Option.Female;
				case '3':
					return Option.Neuter;
				case '4':
					return Option.Plural;
				case '0':
					return Option.Ignore;
				default:
					return Option.Unknown;
			}
		}

		private static string GetFileForOption(Options options, Option option)
		{
			switch (option)
			{
				case Option.Male:
					return options.MaleGenderFile;
				case Option.Female:
					return options.FemaleGenderFile;
				case Option.Neuter:
					return options.NeuterGenderFile;
				case Option.Plural:
					return options.PluralGenderFile;
				case Option.Ignore:
					return options.IgnoreFile;
				default:
					return null;
			}
		}

		private static void SmartDistinctLines(string filePath)
		{
			string[] allLines = File.ReadAllLines(filePath);

			HashSet<string> usedLines = new HashSet<string>();

			List<string> distinctLines = new List<string>();

			foreach (string line in allLines)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					distinctLines.Add(string.Empty);
					continue;
				}

				if (usedLines.Contains(line))
					continue;

				distinctLines.Add(line);
				usedLines.Add(line);
			}

			File.WriteAllLines(filePath, distinctLines);
		}

		private static string GetLastPart(Injection injection)
		{
			return injection.DefPathParts[injection.DefPathParts.Length - 1];
		}
	}
}

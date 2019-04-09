using CommandLine;
using Cyriller;
using Cyriller.Model;
using Elevator.Subtranslator.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elevator.Subtranslator.LabelDecliner
{
	enum Option { Unknown, Accept, Ignore }

	class Options
	{
		[Option('d', "defs", Required = true, HelpText = "Path to translation DefInjected folder.")]
		public string DefsPath { get; set; }

		[Option('t', "def types", DefaultValue = "", Required = false, HelpText = "Use only these def types.")]
		public string DefsTypes { get; set; }

		[Option('o', "output", Required = true, HelpText = "Path to the cased nouns table file.")]
		public string Output { get; set; }

		[Option('i', "ignored", Required = true, HelpText = "Path to the file with nouns, for which case is ignored.")]
		public string IgnoreFile { get; set; }
	}

	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any())
				return;

			Options options = parseResult.Value;

			// Создаем коллекцию всех существительных.
			CyrNounCollection nouns = new CyrNounCollection();

			// Создаем коллекцию всех прилагательных.
			CyrAdjectiveCollection adjectives = new CyrAdjectiveCollection();

			// Создаем фразу с использование созданных коллекций.
			CyrPhrase phrase = new CyrPhrase(nouns, adjectives);

			InjectionAnalyzer analyzer = new InjectionAnalyzer();

			HashSet<string> defTypes = new HashSet<string>(options.DefsTypes.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));

			HashSet<string> ignoredInjections = new HashSet<string>(File.ReadAllLines(options.IgnoreFile));

			HashSet<string> casedLabels = new HashSet<string>(File.ReadAllLines(options.Output).Select(line => new string(line.TakeWhile(c => c != ';').ToArray())));

			Injection[] allLabels = analyzer
				.ReadInjections(options.DefsPath)
				.Where(inj => defTypes.Contains(inj.DefType))
				.Where(inj => GetLastPart(inj).ToLowerInvariant().Contains("label"))
				.Distinct(new InjectionTypeTranslationComparer())
				.Where(inj =>
					   !ignoredInjections.Contains(FormInjectionLine(inj))
					&& !casedLabels.Contains(inj.Translation))
				.ToArray();

			Console.WriteLine($"Chack case forms for {allLabels.Length} labels.");

			List<Option> history = new List<Option>();

			string prevDefType = "";

			for (int labelIndex = 0; labelIndex < allLabels.Length;)
			{
				Injection injection = allLabels[labelIndex];
				string label = injection.Translation;

				Console.WriteLine();
				Console.WriteLine($"{labelIndex + 1}/{allLabels.Length} {injection.DefType} <{injection.DefPath}> \"{label}\":");

				CyrResult declinationResult = null;
				try
				{
					declinationResult = phrase.Decline(label, GetConditionsEnum.Strict);
					foreach (CasesEnum labelCase in Enum.GetValues(typeof(CasesEnum)))
					{
						Console.WriteLine($" {labelCase,15}: {declinationResult.Get(labelCase)}");
					}
				}
				catch
				{
					Console.WriteLine($"    Failed to decline");
				}

				Console.Write("<Enter> - accept; <Space> - edit; <Backspace> - back; <Delete> - ignore");
				ConsoleKey key = Console.ReadKey().Key;
				Console.WriteLine();

				switch (key)
				{
					case ConsoleKey.Escape:
						return;

					case ConsoleKey.Spacebar:
						if (declinationResult == null)
							declinationResult = new CyrResult(label);

						foreach (CasesEnum labelCase in Enum.GetValues(typeof(CasesEnum)))
						{
							Console.Write($" {labelCase,15}: ");
							declinationResult.Set(labelCase, ConsoleTools.ReadLine(label));
						}
						if (injection.DefType != prevDefType)
						{
							FileUtil.AppendLine(options.Output, string.Empty);
							FileUtil.AppendLine(options.Output, "// " + injection.DefType);
						}
						FileUtil.AppendLine(options.Output, Serialize(declinationResult));
						history.Add(Option.Accept);
						labelIndex++;
						break;

					case ConsoleKey.Enter:
						if (declinationResult != null)
						{
							if (injection.DefType != prevDefType)
							{
								FileUtil.AppendLine(options.Output, string.Empty);
								FileUtil.AppendLine(options.Output, "// " + injection.DefType);
							}
							FileUtil.AppendLine(options.Output, Serialize(declinationResult));
							history.Add(Option.Accept);
							labelIndex++;
						}
						break;

					case ConsoleKey.Delete:
						FileUtil.AppendLine(options.IgnoreFile, FormInjectionLine(injection));
						history.Add(Option.Ignore);
						labelIndex++;
						break;

					case ConsoleKey.Backspace:
						string prevFileName = GetFileForOption(options, history[labelIndex - 1]);
						FileUtil.DeleteLine(prevFileName);
						history.RemoveAt(labelIndex - 1);
						labelIndex--;
						break;

					default:
						break;
				}

				prevDefType = injection.DefType;
			}


		}

		private static string Serialize(CyrResult singular)
		{
			return $"{singular.Nominative}; {singular.Genitive}; {singular.Dative}; {singular.Accusative}; {singular.Instrumental}; {singular.Prepositional}";
		}

		private static string GetFileForOption(Options options, Option option)
		{
			switch (option)
			{
				case Option.Accept:
					return options.Output;
				case Option.Ignore:
					return options.IgnoreFile;
				default:
					return null;
			}
		}

		private static string FormInjectionLine(Injection inj)
		{
			return $"{inj.DefType} {inj.DefPath}: {inj.Translation}";
		}

		private static string GetLastPart(Injection injection)
		{
			return injection.DefPathParts[injection.DefPathParts.Length - 1];
		}
	}
}

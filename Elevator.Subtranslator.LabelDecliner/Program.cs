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

	class Arguments
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
			ParserResult<Arguments> parseResult = Parser.Default.ParseArguments<Arguments>(args);
			if (parseResult.Errors.Any())
				return;

			Arguments arguments = parseResult.Value;

			// Создаем коллекцию всех существительных.
			CyrNounCollection nouns = new CyrNounCollection();

			// Создаем коллекцию всех прилагательных.
			CyrAdjectiveCollection adjectives = new CyrAdjectiveCollection();

			// Создаем фразу с использование созданных коллекций.
			CyrPhrase phrase = new CyrPhrase(nouns, adjectives);

			InjectionAnalyzer analyzer = new InjectionAnalyzer();

			HashSet<string> defTypes = new HashSet<string>(arguments.DefsTypes.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));

			HashSet<string> ignoredInjections = new HashSet<string>(File.ReadAllLines(arguments.IgnoreFile));

			HashSet<string> casedLabels = new HashSet<string>(File.ReadAllLines(arguments.Output).Select(line => new string(line.TakeWhile(c => c != ';').ToArray())));

			Injection[] allLabels = analyzer
				.ReadInjections(arguments.DefsPath)
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

			CyrResult currentDeclination = null;

			for (int labelIndex = 0; labelIndex < allLabels.Length;)
			{
				Injection injection = allLabels[labelIndex];
				string label = injection.Translation;

				Console.WriteLine();
				Console.WriteLine($"{labelIndex + 1}/{allLabels.Length} {injection.DefType} <{injection.DefPath}> \"{label}\":");

				if (currentDeclination == null)
					currentDeclination = DeclineIgnoreSuffix(phrase, label, " (", " из ", " для ", " с ");

				if (currentDeclination == null)
				{
					Console.WriteLine($"    Failed to decline");
				}
				else
				{
					WriteDeclination(currentDeclination);
				}

				Console.Write("<Enter> - accept; <Space> - edit; <Backspace> - back; <Delete> - ignore");
				ConsoleKey key = Console.ReadKey().Key;
				Console.WriteLine();

				switch (key)
				{
					case ConsoleKey.Escape:
						return;

					case ConsoleKey.Spacebar:
						if (currentDeclination == null)
							currentDeclination = new CyrResult(label, label, label, label, label, label);

						ReadDeclination(currentDeclination);

						if (injection.DefType != prevDefType)
						{
							FileUtil.PushLine(arguments.Output, string.Empty);
							FileUtil.PushLine(arguments.Output, "// " + injection.DefType);
						}
						FileUtil.PushLine(arguments.Output, DeclinationTools.Serialize(currentDeclination));
						currentDeclination = null;
						history.Add(Option.Accept);
						labelIndex++;
						break;

					case ConsoleKey.Enter:
						if (currentDeclination != null)
						{
							if (injection.DefType != prevDefType)
							{
								FileUtil.PushLine(arguments.Output, string.Empty);
								FileUtil.PushLine(arguments.Output, "// " + injection.DefType);
							}
							FileUtil.PushLine(arguments.Output, DeclinationTools.Serialize(currentDeclination));
							currentDeclination = null;
							history.Add(Option.Accept);
							labelIndex++;
						}
						break;

					case ConsoleKey.Delete:
						FileUtil.PushLine(arguments.IgnoreFile, FormInjectionLine(injection));
						currentDeclination = null;
						history.Add(Option.Ignore);
						labelIndex++;
						break;

					case ConsoleKey.Backspace:
						Option prevOption = history[labelIndex - 1];
						history.RemoveAt(labelIndex - 1);
						labelIndex--;

						if (prevOption == Option.Accept)
						{
							string prevDeclinationLine = FileUtil.PopLine(arguments.Output);
							currentDeclination = DeclinationTools.Deserialize(prevDeclinationLine);
						}
						else if (prevOption == Option.Ignore)
						{
							FileUtil.PopLine(arguments.IgnoreFile);
							currentDeclination = null;
						}
						break;

					default:
						break;
				}

				prevDefType = injection.DefType;
			}
		}

		private static CyrResult DeclineIgnoreSuffix(CyrPhrase decliner, string phrase, params string[] ignoreSuffixesStart)
		{
			int openSymbolIndex = phrase.IndexOfAny(ignoreSuffixesStart);
			if (openSymbolIndex == -1)
				return DeclinationTools.Decline(decliner, phrase);

			string parenthesisExpr = phrase.Substring(openSymbolIndex, phrase.Length - openSymbolIndex);
			string parenthesisLessPhrase = phrase.Remove(openSymbolIndex, phrase.Length - openSymbolIndex);

			CyrResult result = DeclinationTools.Decline(decliner, parenthesisLessPhrase);
			if (result == null)
				return null;

			foreach (CasesEnum labelCase in Enum.GetValues(typeof(CasesEnum)))
			{
				result.Set(labelCase, result.Get(labelCase) + parenthesisExpr);
			}

			return result;
		}

		private static void ReadDeclination(CyrResult declinationResult)
		{
			foreach (CasesEnum labelCase in Enum.GetValues(typeof(CasesEnum)))
			{
				Console.Write($" {labelCase,15}: ");
				declinationResult.Set(labelCase, ConsoleTools.ReadLine(declinationResult.Get(labelCase)));
			}
		}

		private static void WriteDeclination(CyrResult declinationResult)
		{
			foreach (CasesEnum labelCase in Enum.GetValues(typeof(CasesEnum)))
			{
				Console.WriteLine($" {labelCase,15}: {declinationResult.Get(labelCase)}");
			}
		}

		private static string GetFileForOption(Arguments arguments, Option option)
		{
			switch (option)
			{
				case Option.Accept:
					return arguments.Output;
				case Option.Ignore:
					return arguments.IgnoreFile;
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

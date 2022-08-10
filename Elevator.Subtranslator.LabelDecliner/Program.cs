using CommandLine;
using Cyriller;
using Cyriller.Model;
using Elevator.Subtranslator.Common;
using Elevator.Subtranslator.ConsoleTools;
using Elevator.Subtranslator.DeclinationTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elevator.Subtranslator.LabelDecliner
{
	enum Option { Unknown, Accept, Ignore }

	class Arguments
	{
		[Option('d', "defsPath", Required = true, HelpText = "Path to translation DefInjected folder.")]
		public string DefsPath { get; set; }

		[Option('t', "defTypes", DefaultValue = "", Required = false, HelpText = "Use only these def types.")]
		public string DefsTypes { get; set; }

		[Option('o', "outputFile", Required = true, HelpText = "Path to the cased nouns table file.")]
		public string Output { get; set; }

		[Option('i', "ignoreFile", Required = true, HelpText = "Path to the file with nouns, for which case is ignored.")]
		public string IgnoreFile { get; set; }
	}

	/// <summary>
	/// Reads def injections' labels and allows user to specify all cases for each label, then outputs them to a specified file
	/// </summary>
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
					currentDeclination = DeclineIgnoreSuffix(phrase, label, " (", " из ", " для ", " с ", " в ");

				if (currentDeclination == null)
				{
					Console.WriteLine($"	Failed to decline");
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
						FileUtil.PushLine(arguments.Output, ToLine(currentDeclination));
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
							FileUtil.PushLine(arguments.Output, ToLine(currentDeclination));
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
							currentDeclination = FromLine(prevDeclinationLine);
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
				return CaseTools.Decline(decliner, phrase);

			string parenthesisExpr = phrase.Substring(openSymbolIndex, phrase.Length - openSymbolIndex);
			string parenthesisLessPhrase = phrase.Remove(openSymbolIndex, phrase.Length - openSymbolIndex);

			CyrResult result = CaseTools.Decline(decliner, parenthesisLessPhrase);
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
			int cursorPos = -1;
			foreach (CasesEnum labelCase in Enum.GetValues(typeof(CasesEnum)))
			{
				Console.Write($" {labelCase,15}: ");
				declinationResult.Set(labelCase, SmartConsole.EditLine(declinationResult.Get(labelCase), cursorPos, out cursorPos));
			}
		}

		private static void WriteDeclination(CyrResult declinationResult)
		{
			foreach (CasesEnum labelCase in Enum.GetValues(typeof(CasesEnum)))
			{
				Console.WriteLine($" {labelCase,15}: {declinationResult.Get(labelCase)}");
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

		public static string ToLine(CyrResult declination)
		{
			return
				$"{declination.Nominative}; " +
				$"{declination.Genitive}; " +
				$"{declination.Dative}; " +
				$"{declination.Accusative}; " +
				$"{declination.Instrumental}; " +
				$"{declination.Prepositional}";
		}

		public static CyrResult FromLine(string declinationStr)
		{
			string[] cases = declinationStr.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries);

			return new CyrResult(
				cases[0],
				cases[1],
				cases[2],
				cases[3],
				cases[4],
				cases[5]);
		}
	}
}

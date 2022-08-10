using CommandLine;
using Cyriller;
using Elevator.Subtranslator.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cyriller.Model;
using Elevator.Subtranslator.ConsoleTools;

namespace Elevator.Subtranslator.LabelPluralizer
{
	enum Option { Unknown, Accept, Ignore }

	class Arguments
	{
		[Option('d', "defsPath", Required = true, HelpText = "Path to translation DefInjected folder.")]
		public string DefsPath { get; set; }

		[Option('t', "defTypes", DefaultValue = "", Required = false, HelpText = "Use only these def types.")]
		public string DefsTypes { get; set; }

		[Option('o', "outputFile", Required = true, HelpText = "Path to the plural nouns table file.")]
		public string Output { get; set; }

		[Option('i', "ignoreFile", Required = true, HelpText = "Path to the file with nouns, for which plural form is ignored.")]
		public string IgnoreFile { get; set; }
	}

	internal class Program
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

			HashSet<string> ignoredInjections = File.Exists(arguments.Output)
				? new HashSet<string>(File.ReadAllLines(arguments.IgnoreFile))
				: new HashSet<string>();

			HashSet<string> pluralizedLabels = File.Exists(arguments.Output)
				? new HashSet<string>(File.ReadAllLines(arguments.Output).Select(line => new string(line.TakeWhile(c => c != ';').ToArray())))
				: new HashSet<string>();

			Injection[] allLabels = analyzer
				.ReadInjections(arguments.DefsPath)
				.Where(inj => defTypes.Contains(inj.DefType))
				.Where(inj => GetLastPart(inj).ToLowerInvariant().Contains("label"))
				.Distinct(new InjectionTypeTranslationComparer())
				.Where(inj =>
					   !ignoredInjections.Contains(FormInjectionLine(inj))
					&& !pluralizedLabels.Contains(inj.Translation))
				.ToArray();

			Console.WriteLine($"Check plural forms for {allLabels.Length} labels.");

			List<Option> history = new List<Option>();

			string prevDefType = "";

			PluralPair pluralPair = null;

			for (int labelIndex = 0; labelIndex < allLabels.Length;)
			{
				Injection injection = allLabels[labelIndex];
				string label = injection.Translation;

				Console.WriteLine();
				Console.WriteLine($"{labelIndex + 1}/{allLabels.Length} {injection.DefType} <{injection.DefPath}> \"{label}\":");

				if (pluralPair == null)
					pluralPair = PluralizeIgnoreSuffix(phrase, label, " (", " из ", " для ", " с ", " в ");

				if (pluralPair == null)
				{
					Console.WriteLine($"	Failed to pluralize");
				}
				else
				{
					WritePluralization(pluralPair);
				}

				Console.Write("<Enter> - accept; <Space> - edit; <Backspace> - back; <Delete> - ignore");
				ConsoleKey key = Console.ReadKey().Key;
				Console.WriteLine();

				switch (key)
				{
					case ConsoleKey.Escape:
						return;

					case ConsoleKey.Spacebar:
						if (pluralPair == null)
							pluralPair = new PluralPair(label, label);

						pluralPair = EditPluralization(pluralPair);

						if (injection.DefType != prevDefType)
						{
							FileUtil.PushLine(arguments.Output, string.Empty);
							FileUtil.PushLine(arguments.Output, "// " + injection.DefType);
						}
						FileUtil.PushLine(arguments.Output, ToLine(pluralPair));
						pluralPair = null;
						history.Add(Option.Accept);
						labelIndex++;
						break;

					case ConsoleKey.Enter:
						if (pluralPair != null)
						{
							if (injection.DefType != prevDefType)
							{
								FileUtil.PushLine(arguments.Output, string.Empty);
								FileUtil.PushLine(arguments.Output, "// " + injection.DefType);
							}
							FileUtil.PushLine(arguments.Output, ToLine(pluralPair));
							pluralPair = null;
							history.Add(Option.Accept);
							labelIndex++;
						}
						break;

					case ConsoleKey.Delete:
						FileUtil.PushLine(arguments.IgnoreFile, FormInjectionLine(injection));
						pluralPair = null;
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
							pluralPair = FromLine(prevDeclinationLine);
						}
						else if (prevOption == Option.Ignore)
						{
							FileUtil.PopLine(arguments.IgnoreFile);
							pluralPair = null;
						}
						break;

					default:
						break;
				}

				prevDefType = injection.DefType;
			}
		}

		private static PluralPair PluralizeIgnoreSuffix(CyrPhrase decliner, string phrase, params string[] ignoreSuffixesStart)
		{
			int ignoredTailStart = phrase.IndexOfAny(ignoreSuffixesStart);
			if (ignoredTailStart == -1)
			{
				CyrResult declinedPhrase = decliner.DeclinePlural(phrase, GetConditionsEnum.Strict);
				return new PluralPair(phrase, declinedPhrase.Nominative);
			}

			string ignoredSuffix = phrase.Substring(ignoredTailStart, phrase.Length - ignoredTailStart);
			string head = phrase.Remove(ignoredTailStart, phrase.Length - ignoredTailStart);

			CyrResult result = decliner.DeclinePlural(head, GetConditionsEnum.Strict);
			if (result == null)
				return null;

			string pluralPhrase = result.Nominative + ignoredSuffix;
			return new PluralPair(phrase, pluralPhrase);
		}

		private static PluralPair EditPluralization(PluralPair input)
		{
			int cursorPos = -1;

			Console.Write($"   Singular: ");
			string singular = SmartConsole.EditLine(input.Singular, cursorPos, out cursorPos);
			Console.Write($"     Plural: ");
			string plural = SmartConsole.EditLine(input.Plural, cursorPos, out cursorPos);

			return new PluralPair(singular, plural);
		}

		private static void WritePluralization(PluralPair input)
		{
			Console.WriteLine($"   Singular: {input.Singular}");
			Console.WriteLine($"     Plural: {input.Plural}");
		}

		private static string FormInjectionLine(Injection inj)
		{
			return $"{inj.DefType} {inj.DefPath}: {inj.Translation}";
		}

		private static string GetLastPart(Injection injection)
		{
			return injection.DefPathParts[injection.DefPathParts.Length - 1];
		}

		public static string ToLine(PluralPair pair)
		{
			return $"{pair.Singular}; {pair.Plural}";
		}

		public static PluralPair FromLine(string line)
		{
			string[] forms = line.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
			return new PluralPair(forms[0], forms[1]);
		}
	}
}

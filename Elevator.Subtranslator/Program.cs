using CommandLine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Elevator.Subtranslator.Common;

namespace Elevator.Subtranslator
{
	class Options
	{
		[Option('d', "defs", Required = false, HelpText = "Path to Def folder. If specified, the tool will use these values in output report. If not specified, etalon translation values will be used.")]
		public string DefsPath { get; set; }

		[Option('i', "injections", Required = true, HelpText = "Path to 'DefInjected' folder of target localization.")]
		public string InjectionsPath { get; set; }

		[Option('e', "etalon", Required = true, HelpText = "Path to 'DefInjected' folder of etalon localization.")]
		public string InjectionsEtalonPath { get; set; }

		[Option('a', "append", Required = false, DefaultValue = false, HelpText = "Append new translation lines to current localization or not")]
		public bool AppendTranslation { get; set; }

		[Option('r', "report", Required = false, HelpText = "Report file output path.")]
		public string ReportPath { get; set; }
	}

	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any()) return;

			Options options = parseResult.Value;

			InjectionAnalyzer analyzer = new InjectionAnalyzer();
			List<Injection> injections = analyzer.ReadInjections(options.InjectionsPath).ToList();
			List<Injection> etalonInjections = analyzer.ReadInjections(options.InjectionsEtalonPath).ToList();

			InjectionPathComparer injComparer = new InjectionPathComparer();
			DefTypeComparer defTypeComparer = new DefTypeComparer();

			List<Injection> itemsToTranslate = etalonInjections.Except(injections, injComparer).ToList();
			List<Injection> itemsToDelete = injections.Except(etalonInjections, injComparer).ToList();

			bool useEtalon = string.IsNullOrEmpty(options.DefsPath);

			if (!useEtalon)
			{
				XDocument mergedDoc = DefWorker.MergeDefs(options.DefsPath);
				itemsToTranslate = analyzer.FillOriginalValues(mergedDoc, itemsToTranslate).ToList();
				itemsToDelete = analyzer.FillOriginalValues(mergedDoc, itemsToDelete).ToList();
			}

			List<IGrouping<string, Injection>> groupedItemsToTranslate = itemsToTranslate.GroupBy(inj => inj.DefType).ToList();
			List<IGrouping<string, Injection>> groupedItemsToDelete = itemsToDelete.GroupBy(inj => inj.DefType).ToList();

			if (options.AppendTranslation)
			{
				DirectoryInfo injectionsDir = new DirectoryInfo(options.InjectionsPath);

				foreach (IGrouping<string, Injection> group in groupedItemsToTranslate)
				{
					string defType = group.Key;
					Injection injectionWithSameDefType = injections.FirstOrDefault(inj => defTypeComparer.Equals(inj.DefType, defType));

					DirectoryInfo defTypeDir =
						injectionWithSameDefType == null
						? Directory.CreateDirectory(Path.Combine(options.InjectionsPath, defType))
						: new DirectoryInfo(Path.Combine(injectionsDir.FullName, injectionWithSameDefType.DirectoryName));

					XDocument doc = new XDocument();

					XElement languageData = new XElement("LanguageData");
					doc.Add(languageData);

					foreach (Injection injection in group)
					{
						XElement injectionElement = new XElement(injection.DefPath, useEtalon ? injection.Translation : injection.Original);
						languageData.Add(injectionElement);
					}

					doc.Save(Path.Combine(defTypeDir.FullName, "Translate.xml"));
				}
			}

			using (StreamWriter sw = File.CreateText(options.ReportPath))
			{
				sw.WriteLine();
				sw.WriteLine("Items to translate:");
				foreach (IGrouping<string, Injection> group in groupedItemsToTranslate)
				{
					sw.WriteLine();
					sw.WriteLine(group.Key + ":");
					foreach (Injection injection in group)
					{
						sw.WriteLine("\t<{0}>{1}</{0}>", injection.DefPath, useEtalon ? injection.Translation : injection.Original);
					}
				}

				sw.WriteLine();
				sw.WriteLine();
				sw.WriteLine("Items to delete:");
				foreach (IGrouping<string, Injection> group in groupedItemsToDelete)
				{
					sw.WriteLine();
					sw.WriteLine(group.Key + ":");
					foreach (Injection injection in group)
					{
						sw.WriteLine("\t{0}:\t{1}\t{2}", injection.DefPath, injection.Original, injection.Translation);
					}
				}
				sw.Close();
			}
		}
	}
}

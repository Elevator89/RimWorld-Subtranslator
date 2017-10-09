﻿using System.Collections.Generic;
using System.Linq;
using CommandLine;
using System.IO;
using System.Xml.Linq;
using System;

namespace Elevator.Subtranslator
{
	class Options
	{
		[Option('d', "defs", Required = true, HelpText = "Definition folder location.")]
		public string DefsPath { get; set; }

		[Option('i', "injections", Required = true, HelpText = "Localized 'DefInjected' folder location.")]
		public string InjectionsPath { get; set; }

		[Option('e', "etalon", Required = true, HelpText = "Etalon 'DefInjected' folder location.")]
		public string InjectionsEtalonPath { get; set; }

		[Option('o', "output", Required = false, HelpText = "Output folder location.")]
		public string OutputPath { get; set; }
	}

	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any()) return;

			Options options = parseResult.Value;

			InjectionAnalyzer analyzer = new InjectionAnalyzer();
			IEnumerable<Injection> injections = analyzer.ReadInjections(options.InjectionsPath);
			IEnumerable<Injection> etalonInjections = analyzer.ReadInjections(options.InjectionsEtalonPath);

			InjectionPathComparer injComparer = new InjectionPathComparer();

			List<Injection> itemsToTranslate = etalonInjections.Except(injections, injComparer).ToList();
			List<Injection> itemsToDelete = injections.Except(etalonInjections, injComparer).ToList();

			XDocument mergedDoc = MergeDefs(options.DefsPath);
			itemsToTranslate = analyzer.FillOriginalValues(mergedDoc, itemsToTranslate).ToList();
			itemsToDelete = analyzer.FillOriginalValues(mergedDoc, itemsToDelete).ToList();

			List<Tuple<Injection, Injection>> moved = FindMovedInjections(itemsToDelete, itemsToTranslate).ToList();
			itemsToTranslate = itemsToTranslate.Except(moved.Select(item => item.Item2)).ToList();
			itemsToDelete = itemsToDelete.Except(moved.Select(item => item.Item1)).ToList();

			List<IGrouping<string, Injection>> groupedItemsToTranslate = itemsToTranslate.GroupBy(inj => inj.DefType).ToList();
			List<IGrouping<string, Injection>> groupedItemsToDelete = itemsToDelete.GroupBy(inj => inj.DefType).ToList();

			using (StreamWriter sw = File.CreateText(Path.Combine(options.OutputPath, "InjectionsReport.txt")))
			{
				sw.WriteLine("Items to translate:");
				foreach (IGrouping<string, Injection> group in groupedItemsToTranslate)
				{
					sw.WriteLine(group.Key + ":");
					foreach (Injection injection in group)
					{
						sw.WriteLine("\t<{0}>{1}</{0}>", injection.DefPath, injection.Original);
					}
				}

				sw.WriteLine();
				sw.WriteLine("Items to delete:");
				foreach (IGrouping<string, Injection> group in groupedItemsToDelete)
				{
					sw.WriteLine(group.Key + ":");
					foreach (Injection injection in group)
					{
						sw.WriteLine("\t<{0}>{1}</{0}>", injection.DefPath, injection.Original);
					}
				}

				sw.WriteLine();
				sw.WriteLine("Items to move:");
				foreach (Tuple<Injection, Injection> pair in moved)
				{
					sw.WriteLine("\t<{0}>{1}</{0}> -------->" + Environment.NewLine + "\t\t<{2}>{3}</{2}>", pair.Item1.DefPath, pair.Item1.Translation, pair.Item2.DefPath, pair.Item2.Translation);
				}
				sw.Close();
			}
		}

		static IEnumerable<Tuple<Injection, Injection>> FindMovedInjections(List<Injection> itemsToDelete, List<Injection> itemsToTranslate)
		{
			foreach (Injection toDelete in itemsToDelete)
			{
				if (string.IsNullOrEmpty(toDelete.Original))
					continue;

				foreach (Injection toTranslate in itemsToTranslate)
				{
					if (string.IsNullOrEmpty(toTranslate.Original))
						continue;

					if (StringsAreSimilar(toDelete.Original, toTranslate.Original))
						yield return new Tuple<Injection, Injection>(toDelete, toTranslate);
				}
			}
		}

		static bool StringsAreSimilar(string a, string b)
		{
			LevenshteinMeter meter = new LevenshteinMeter(1, 1, 1);
			return meter.GetNormedDistanceQ(a, b, 100f) < 0.1f;
		}

		static XDocument MergeDefs(string defsFullPath)
		{
			XDocument mergedXml = new XDocument();
			XElement mergedDefs = new XElement("Defs");
			mergedXml.Add(mergedDefs);

			foreach (string defFilePath in Directory.EnumerateFiles(defsFullPath, "*.xml", SearchOption.AllDirectories))
			{
				XDocument defXml = XDocument.Load(defFilePath);
				XElement defs = defXml.Root;

				foreach (XElement def in defs.Elements())
				{
					mergedDefs.Add(new XElement(def));
				}
			}
			return mergedXml;
		}
	}
}

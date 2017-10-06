using System.Collections.Generic;
using System.Linq;
using CommandLine;
using System.IO;
using System.Xml.Linq;

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

			IEnumerable<Injection> itemsToTranslate = etalonInjections.Except(injections, injComparer);
			IEnumerable<Injection> itemsToDelete = injections.Except(etalonInjections, injComparer);

			XDocument mergedDoc = MergeDefs(options.DefsPath);
			itemsToTranslate = analyzer.FillOriginalValues(mergedDoc, itemsToTranslate).Where(inj => !string.IsNullOrEmpty(inj.Original));
			itemsToDelete = analyzer.FillOriginalValues(mergedDoc, itemsToDelete).Where(inj => string.IsNullOrEmpty(inj.Original));

			File.WriteAllLines(Path.Combine(options.OutputPath, "InjectionsToTranslate.txt"), itemsToTranslate.Select(inj => string.Format("{2}: <{0}>{1}</{0}>", inj.DefPath, inj.Original, inj.DefType)));
			File.WriteAllLines(Path.Combine(options.OutputPath, "InjectionsToDelete.txt"), itemsToDelete.Select(inj => string.Format("{0}: {1}", inj.DefType, inj.DefPath)));

			//mergedDoc.Save(Path.Combine(options.OutputPath, "MergedDoc.xml"));
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

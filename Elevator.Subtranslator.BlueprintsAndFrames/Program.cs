using CommandLine;
using Elevator.Subtranslator.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Elevator.Subtranslator.BlueprintsAndFrames
{
	class Options
	{
		[Option('d', "defs", Required = false, HelpText = "Path to Def folder. If specified, the tool will use these values in output report. If not specified, etalon translation values will be used.")]
		public string DefsPath { get; set; }

		[Option('i', "injections", Required = true, HelpText = "Path to 'DefInjected' folder of target localization.")]
		public string InjectionsPath { get; set; }

		//[Option('f', "frames", Required = true, HelpText = "Path to '_ThingDef_BlueprintsAndFrames.xml' file of target localization.")]
		//public string FramesFilePath { get; set; }

		//[Option('a', "append", Required = false, DefaultValue = false, HelpText = "Append new translation lines to current localization or not")]
		//public bool AppendTranslation { get; set; }

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

			//InjectionAnalyzer analyzer = new InjectionAnalyzer();
			//List<Injection> injections = analyzer.ReadInjections(options.InjectionsPath).ToList();

			XDocument mergedDefs = DefWorker.MergeDefs(options.DefsPath);

			InjectionAnalyzer injAnalyzer = new InjectionAnalyzer();
			List<Injection> injections = injAnalyzer.ReadInjections(options.InjectionsPath).ToList();

			const string blueprintSuffix = " (проект)";
			const string frameSuffix = " (строится)";

			using (StreamWriter sw = File.CreateText(options.ReportPath))
			{
				foreach (XElement def in mergedDefs.Root.Elements())
				{
					string defname = DefWorker.GetDefName(def);
					string defType = def.Name.LocalName;

					if (DefWorker.IsDefAbstract(def)) continue;
					if (!DefWorker.DefHasParent(mergedDefs, def, "BuildingBase")) continue;
					if (!DefWorker.DefHasElement(mergedDefs, def, "designationCategory")) continue;

					XElement defLabelElement = def.Element("label");
					if (defLabelElement == null) continue;

					string defLabel = defLabelElement.Value;
					if (string.IsNullOrEmpty(defLabel)) continue;

					string defLabelLocalized = injections.FirstOrDefault(inj => inj.DefType == defType && inj.DefPath == defname + ".label").Translation; ;


					Injection blueprintLabel = injections.FirstOrDefault(inj => inj.DefType == defType && inj.DefPath == defname + "_Blueprint.label");
					string blueprintTag = string.Format("{0}_Blueprint.label", defname);
					if (blueprintLabel == null)
					{
						sw.WriteLine("\t<{0}>{1}{2}</{0}>", blueprintTag, defLabelLocalized, blueprintSuffix);
					}
					else if (blueprintLabel.Translation.Replace(blueprintSuffix, string.Empty) != defLabelLocalized)
					{
						sw.WriteLine("Replace: <{0}>", blueprintTag);
						sw.WriteLine("\t\t{0}", blueprintLabel.Translation);
						sw.WriteLine("\t\t{0}{1}", defLabelLocalized, blueprintSuffix);
					}


					Injection frameLabel = injections.FirstOrDefault(inj => inj.DefType == defType && inj.DefPath == defname + "_Frame.label");
					string frameTag = string.Format("{0}_Frame.label", defname);
					if (frameLabel == null)
					{
						sw.WriteLine("\t<{0}>{1}{2}</{0}>", frameTag, defLabelLocalized, frameSuffix);
					}
					else if (frameLabel.Translation.Replace(frameSuffix, string.Empty) != defLabelLocalized)
					{
						sw.WriteLine("Replace: <{0}>", frameTag);
						sw.WriteLine("\t\t{0}", frameLabel.Translation);
						sw.WriteLine("\t\t{0}{1}", defLabelLocalized, frameSuffix);
					}

					string defDescriptionLocalized = injections.FirstOrDefault(inj => inj.DefType == defType && inj.DefPath == defname + ".description").Translation;
					Injection frameDescription = injections.FirstOrDefault(inj => inj.DefType == defType && inj.DefPath == defname + "_Frame.description");
					string descriptionTag = string.Format("{0}_Frame.description", defname);
					if (frameDescription == null)
					{
						sw.WriteLine("\t<{0}>{1}</{0}>", descriptionTag, defDescriptionLocalized);
					}else if(frameDescription.Translation != defDescriptionLocalized)
					{
						sw.WriteLine("Replace: <{0}>", descriptionTag);
						sw.WriteLine("\t\t{0}", frameDescription.Translation);
						sw.WriteLine("\t\t{0}", defDescriptionLocalized);
					}

					//if (injections.FirstOrDefault(inj => inj.DefType == defType && inj.DefPath == defname + "_Blueprint_Install.label") == null)
					//{
					//	string tag = string.Format("{0}_Blueprint_Install.label", defname);
					//	sw.WriteLine("\t<{0}>{1} (проект)</{0}>", tag, defLabelLocalized);
					//}
				}
				sw.Close();
			}
		}
	}
}

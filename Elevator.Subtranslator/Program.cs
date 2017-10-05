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

	class DefDirectoryNameComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			string clearX = x.Replace("Defs", "").Replace("Def", "").TrimEnd('s');
			string clearY = y.Replace("Defs", "").Replace("Def", "").TrimEnd('s');

			return clearX == clearY;
		}

		public int GetHashCode(string obj)
		{
			return obj.GetHashCode();
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any()) return;

			Options options = parseResult.Value;

			XDocument mergedInjectionsEtalon = MergeInjections(options.InjectionsEtalonPath);
			XDocument mergedInjections = MergeInjections(options.InjectionsPath);

			List<string> mergedInjectionsEtalonTags = GetChildrenNames(mergedInjectionsEtalon.Root).ToList();
			List<string> mergedInjectionsTags = GetChildrenNames(mergedInjections.Root).ToList();

			IEnumerable<string> itemsToTranslate = mergedInjectionsEtalonTags.Except(mergedInjectionsTags);
			IEnumerable<string> itemsToDelete = mergedInjectionsTags.Except(mergedInjectionsEtalonTags);

			File.WriteAllLines(Path.Combine(options.OutputPath, "ItemsToTranslate.txt"), itemsToTranslate);
			File.WriteAllLines(Path.Combine(options.OutputPath, "ItemsToDelete.txt"), itemsToDelete);

			//XDocument mergedDoc = MergeDefs(options.DefsPath);
			//string[] excludeElementSuffixes = new string[] { "depth", "width", "height", "class", "texture", "category", "type", "path", "tags", "techLevel", "timeOfDay", "mode" };

			//XDocument filteredDoc = FilterStringDefs(mergedDoc, excludeElementSuffixes);
			//filteredDoc.Save(Path.Combine(options.OutputPath, "FilteredDefs.xml"));
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

		static XDocument MergeInjections(string injectionsFullPath)
		{
			XDocument mergedXml = new XDocument();
			XElement mergedDefs = new XElement("LanguageData");
			mergedXml.Add(mergedDefs);

			foreach (string injectionsFilePath in Directory.EnumerateFiles(injectionsFullPath, "*.xml", SearchOption.AllDirectories))
			{
				XDocument injectionsDoc = XDocument.Load(injectionsFilePath);
				XElement languageData = injectionsDoc.Root;

				foreach (XElement injection in languageData.Elements())
				{
					mergedDefs.Add(new XElement(injection));
				}
			}
			return mergedXml;
		}

		static IEnumerable<string> GetChildrenNames(XElement element)
		{
			foreach (XElement child in element.Elements())
			{
				yield return child.Name.LocalName;
			}
		}

		static HashSet<string> GetDefNames(XDocument doc)
		{
			XElement defs = doc.Root;

			HashSet<string> defNames = new HashSet<string>();

			foreach (XElement def in defs.Elements())
			{
				XElement defNameElem = def.Element("defName");
				if (defNameElem != null)
				{
					defNames.Add(defNameElem.Value);
				}
				else
				{
					XAttribute defNameAttrib = def.Attribute("Name");
					if (defNameAttrib != null)
					{
						defNames.Add(defNameAttrib.Value);
					}
				}
			}

			defNames.Distinct();

			return defNames;
		}
	}
}

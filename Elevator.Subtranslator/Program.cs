using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.IO;
using System.Xml.Linq;
using System.Globalization;

namespace Elevator.Subtranslator
{
	class Options
	{
		[Option('d', "defs", Required = true, HelpText = "Definition folder location.")]
		public string DefsLocation { get; set; }

		[Option('i', "injections", Required = true, HelpText = "Localized 'DefInjected' folder location.")]
		public string InjectionsLocation { get; set; }

		[Option('o', "output", Required = false, HelpText = "Output folder location.")]
		public string OutputLocation { get; set; }
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

			XDocument mergedDoc = MergeDefs(options.DefsLocation);
			mergedDoc.Save(Path.Combine(options.OutputLocation, "MergedDefs.xml"));

			string[] excludeElementSuffixes = new string[] { "depth", "width", "height", "class", "texture", "category", "type", "path", "tags", "techLevel", "timeOfDay", "mode" };

			XDocument filteredDoc = FilterStringDefs(mergedDoc, excludeElementSuffixes);
			filteredDoc.Save(Path.Combine(options.OutputLocation, "FilteredDefs.xml"));
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

		static XDocument FilterStringDefs(XDocument doc, string[] excludeElementSuffixes)
		{
			XDocument filteredDoc = new XDocument();
			XElement filteredDefs = new XElement("Defs");
			filteredDoc.Add(filteredDefs);

			HashSet<string> defNames = GetDefNames(doc);

			foreach (XElement def in doc.Root.Elements())
			{
				XElement filteredDef = FilterStringValues(def, defNames, excludeElementSuffixes);
				if (filteredDef != null)
				{
					filteredDefs.Add(filteredDef);
				}
			}

			return filteredDoc;
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

		static XElement FilterStringValues(XElement element, HashSet<string> defNames, string[] excludeSuffixes)
		{
			if (element == null)
				return null;

			if (HasOneOfSuffixes(element.Name.LocalName, excludeSuffixes))
				return null;

			XElement filteredElement = new XElement(element.Name);

			if (element.HasElements)
			{
				foreach (XElement child in element.Elements())
				{
					XElement filteredChild = FilterStringValues(child, defNames, excludeSuffixes);
					if (filteredChild != null)
					{
						filteredElement.Add(filteredChild);
					}
				}

				return filteredElement.HasElements ? filteredElement : null;
			}

			if (!string.IsNullOrEmpty(element.Value))
			{
				if (SeemsHavingStringValue(element, defNames, excludeSuffixes))
				{
					filteredElement.Value = element.Value;
					return filteredElement;
				}

				return null;
			}

			return null;
		}

		static bool SeemsHavingStringValue(XElement element, HashSet<string> defNames, string[] excludeSuffixes)
		{
			if (HasOneOfSuffixes(element.Name.LocalName, excludeSuffixes)) return false;

			string value = element.Value;

			bool boolValue;
			if (bool.TryParse(value, out boolValue)) return false;

			int intValue;
			if (int.TryParse(value, out intValue)) return false;

			float floatValue;
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue)) return false;

			if (value.StartsWith("(") && value.EndsWith(")"))
				return false;

			if (value.StartsWith("RGBA"))
				return false;

			if (defNames.Contains(value)) return false;

			return true;
		}

		static bool HasOneOfSuffixes(string value, string[] suffixes)
		{
			foreach (string suffix in suffixes)
			{
				if (value.EndsWith(suffix, true, CultureInfo.InvariantCulture))
					return true;
			}
			return false;
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Elevator.Subtranslator
{
	public class InjectionAnalyzer
	{
		public IEnumerable<Injection> ReadInjections(string injectionsDirPath)
		{
			DirectoryInfo injectionsDir = new DirectoryInfo(Path.Combine(injectionsDirPath));
			foreach (DirectoryInfo injectionTypeDir in injectionsDir.EnumerateDirectories())
			{
				string defType = injectionTypeDir.Name.Replace("Defs", "Def");
				foreach (FileInfo file in injectionTypeDir.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
				{
					XDocument injectionsDoc = XDocument.Load(file.FullName);
					XElement languageData = injectionsDoc.Root;

					foreach (XElement injection in languageData.Elements())
					{
						yield return new Injection(defType, injection.Name.LocalName) { Translation = injection.Value };
					}
				}
			}
		}

		public IEnumerable<Injection> FillOriginalValues(XDocument mergedDefDoc, IEnumerable<Injection> injections)
		{
			return injections.Select(inj => FillOriginalText(mergedDefDoc, inj));
		}

		private Injection FillOriginalText(XDocument mergedDefDoc, Injection injection)
		{
			string originalValue = GetDefFieldValue(mergedDefDoc, injection.DefType, injection.DefPath);
			injection.Original = originalValue;
			return injection;
		}

		private string GetDefFieldValue(XDocument mergedDefDoc, string defType, string path)
		{
			XElement allDefs = mergedDefDoc.Root;

			string[] pathItems = path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

			string defName = pathItems.First();
			IEnumerable<string> tail = pathItems.Skip(1);

			XElement def = GetDef(allDefs, defType, defName);
			if (def == null)
			{
				return null;
			}

			string defFieldValue = GetFieldValue(def, tail);

			while (string.IsNullOrEmpty(defFieldValue))
			{
				XAttribute parentNameAttrib = def.Attribute("ParentName");

				if (parentNameAttrib == null) return null;

				defName = parentNameAttrib.Value;
				def = GetDef(allDefs, defType, defName);

				if (def == null) return null;

				defFieldValue = GetFieldValue(def, tail);
			}

			return defFieldValue;
		}

		private XElement GetDef(XElement defsRoot, string defType, string defName)
		{
			string defPath = string.Format("{0}[defName = \"{1}\"]", defType, defName);

			XElement def = defsRoot.XPathSelectElement(defPath);
			if (def == null)
			{
				defPath = string.Format("{0}[@Name = \"{1}\"]", defType, defName);
				def = defsRoot.XPathSelectElement(defPath);
			}
			return def;
		}

		private string GetFieldValue(XElement element, IEnumerable<string> path)
		{
			if (element == null)
			{
				return null;
			}

			if (!path.Any())
			{
				return element.Value;
			}

			string head = path.First();
			IEnumerable<string> tail = path.Skip(1);

			int index;
			if (int.TryParse(head, out index))
			{
				string xPath = string.Format("li[{0}]", index + 1);
				XElement listItem = element.XPathSelectElement(xPath);
				return GetFieldValue(listItem, tail);
			}
			else
			{
				XElement child = element.Element(head);
				return GetFieldValue(child, tail);
			}
		}
	}
}

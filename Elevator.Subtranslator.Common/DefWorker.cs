using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Elevator.Subtranslator.Common
{
	public static class DefWorker
	{
		public static XDocument MergeDefs(string defsFullPath)
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

		public static bool DefHasParent(XDocument mergedDefDoc, string defType, string path, string parentName)
		{
			XElement allDefs = mergedDefDoc.Root;
			string[] pathItems = path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

			string defName = pathItems.First();
			XElement def = GetDef(allDefs, defType, defName);

			while (def != null)
			{
				if (defName == parentName)
					return true;

				XAttribute parentNameAttrib = def.Attribute("ParentName");
				if (parentNameAttrib == null)
					return false;

				defName = parentNameAttrib.Value;
				def = GetDef(allDefs, defType, defName);
			}
			throw new KeyNotFoundException("Def " + defName + " not found");
		}

		public static bool DefHasParent(XDocument mergedDefDoc, XElement def, string parentName)
		{
			if (def == null)
				throw new ArgumentException("Def is null");

			XElement allDefs = mergedDefDoc.Root;

			while (def != null)
			{
				string defName = GetDefName(def);
				if (defName == parentName)
					return true;

				XAttribute parentNameAttrib = def.Attribute("ParentName");
				if (parentNameAttrib == null)
					return false;

				string parentDefName = parentNameAttrib.Value;
				def = GetDef(allDefs, def.Name.LocalName, parentDefName);
			}
			throw new KeyNotFoundException("Def " + parentName + " not found");
		}

		public static XElement FindDefParent(XDocument mergedDefDoc, XElement def, Func<XElement, bool> predicate)
		{
			if (def == null)
				throw new ArgumentException("Def is null");

			XElement allDefs = mergedDefDoc.Root;

			while (def != null)
			{
				if (predicate(def))
					return def;

				XAttribute parentNameAttrib = def.Attribute("ParentName");
				if (parentNameAttrib == null)
					return null;

				string parentDefName = parentNameAttrib.Value;
				def = GetDef(allDefs, def.Name.LocalName, parentDefName);
			}
			return null;
		}

		public static bool DefHasElement(XDocument mergedDefDoc, XElement def, string elementName)
		{
			if (def == null)
				throw new ArgumentException("Def is null");

			XElement allDefs = mergedDefDoc.Root;

			while (def != null)
			{
				XElement element = def.Element(elementName);
				if (element != null)
					return true;

				XAttribute parentNameAttrib = def.Attribute("ParentName");
				if (parentNameAttrib == null)
					return false;

				string parentDefName = parentNameAttrib.Value;
				def = GetDef(allDefs, def.Name.LocalName, parentDefName);
			}
			return false;
		}

		public static string GetDefFieldValue(XDocument mergedDefDoc, string defType, string[] pathItems)
		{
			XElement allDefs = mergedDefDoc.Root;

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

				if (parentNameAttrib == null)
					return null;

				defName = parentNameAttrib.Value;
				def = GetDef(allDefs, defType, defName);

				if (def == null)
					return null;

				defFieldValue = GetFieldValue(def, tail);
			}

			return defFieldValue;
		}

		public static XElement GetDef(XElement defsRoot, string defType, string defName)
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

		public static string GetDefName(XElement def)
		{
			XElement defNameElement = def.Element("defName");
			if (defNameElement != null)
			{
				return defNameElement.Value;
			}

			XAttribute defNameAttribute = def.Attribute("Name");
			if (defNameAttribute != null)
			{
				return defNameAttribute.Value;
			}

			return null;
		}

		public static bool IsDefAbstract(XElement def)
		{
			XAttribute defAbstractAttribute = def.Attribute("Abstract");
			if (defAbstractAttribute != null)
			{
				return string.Equals(defAbstractAttribute.Value, "True", StringComparison.InvariantCultureIgnoreCase);
			}

			return false;
		}

		private static string GetFieldValue(XElement element, IEnumerable<string> path)
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

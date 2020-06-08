using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Elevator.Subtranslator.Common
{
	public class InjectionAnalyzer
	{
		public IEnumerable<Injection> ReadInjections(string injectionsDirPath)
		{
			List<Injection> injections = new List<Injection>();

			DirectoryInfo injectionsDir = new DirectoryInfo(injectionsDirPath);
			foreach (DirectoryInfo injectionTypeDir in injectionsDir.EnumerateDirectories())
			{
				string defType = injectionTypeDir.Name.Replace("Defs", "Def");
				foreach (FileInfo file in injectionTypeDir.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
				{
					try
					{
						XDocument injectionsDoc = XDocument.Load(file.FullName);
						XElement languageData = injectionsDoc.Root;

						foreach (XElement injection in languageData.Elements())
						{
							injections.Add(new Injection(injectionTypeDir.Name, defType, injection.Name.LocalName) { Translation = injection.Value });
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
						Console.WriteLine("FIle {0} was impossible to read", file.FullName);
						continue;
					}

				}
			}
			return injections;
		}

		public IEnumerable<Injection> GenerateByLeafParts(XDocument mergedDefDoc, HashSet<string> leafParts)
		{
			foreach (XElement def in mergedDefDoc.Root.Elements())
			{
				string defType = def.Name.LocalName;
				string defName = DefWorker.GetDefName(def);

				foreach (Injection inj in Traverse(def, leafParts, defType, defName))
				{
					yield return inj;
				}
			}
		}

		private IEnumerable<Injection> Traverse(XElement element, HashSet<string> leafParts, string defType, string defPath)
		{
			bool isLeaf = leafParts.Contains(element.Name.LocalName);

			XElement[] listItems = element.Elements("li").ToArray();
			bool isList = listItems.Length > 0;

			if (isLeaf && !isList)
			{
				yield return new Injection(defType, defType, defPath) { Original = element.Value };
			}

			if (isLeaf && isList)
			{
				for (int index = 0; index < listItems.Length; ++index)
				{
					string xPath = string.Format("li[{0}]", index + 1);
					XElement listItem = element.XPathSelectElement(xPath);

					yield return new Injection(defType, defType, defPath + "." + index) { Original = listItem.Value };
				}
			}

			if (!isLeaf && !isList)
			{
				foreach (XElement child in element.Elements())
				{
					foreach (Injection inj in Traverse(child, leafParts, defType, defPath + "." + child.Name.LocalName))
					{
						yield return inj;
					}
				}

			}

			if (!isLeaf && isList)
			{
				for (int index = 0; index < listItems.Length; ++index)
				{
					foreach (Injection inj in Traverse(listItems[index], leafParts, defType, defPath + "." + index))
					{
						yield return inj;
					}
				}

			}
		}

		public HashSet<string> GetInjectionLeafParts(IEnumerable<Injection> injections)
		{
			HashSet<string> leafs = new HashSet<string>();

			foreach (Injection injection in injections)
			{
				string leafPart = GetLeafPart(injection.DefPathParts);
				if (leafPart != null && !leafs.Contains(leafPart))
				{
					leafs.Add(leafPart);
				}
			}

			return leafs;
		}

		private static string GetLeafPart(string[] parts)
		{
			if (parts.Length == 0)
				return null;

			for (int i = parts.Length - 1; i >= 0; --i)
			{
				int listItemIndex;
				string part = parts[i];
				if (!int.TryParse(part, out listItemIndex))
				{
					return part;
				}
			}

			return null;
		}



		public IEnumerable<Injection> FillOriginalValues(XDocument mergedDefDoc, IEnumerable<Injection> injections)
		{
			return injections.Select(inj => FillOriginalText(mergedDefDoc, inj));
		}

		private Injection FillOriginalText(XDocument mergedDefDoc, Injection injection)
		{
			string originalValue = DefWorker.GetDefFieldValue(mergedDefDoc, injection.DefType, injection.DefPathParts);
			injection.Original = originalValue;
			return injection;
		}

	}
}

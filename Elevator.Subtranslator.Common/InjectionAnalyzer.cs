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

		public IEnumerable<Injection> FillOriginalValues(XDocument mergedDefDoc, IEnumerable<Injection> injections)
		{
			return injections.Select(inj => FillOriginalText(mergedDefDoc, inj));
		}

		private Injection FillOriginalText(XDocument mergedDefDoc, Injection injection)
		{
			string originalValue = DefWorker.GetDefFieldValue(mergedDefDoc, injection.DefType, injection.DefPath);
			injection.Original = originalValue;
			return injection;
		}

	}
}

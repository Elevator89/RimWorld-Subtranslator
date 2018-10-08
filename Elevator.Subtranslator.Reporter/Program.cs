using CommandLine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Elevator.Subtranslator.Common;
using Elevator.Subtranslator.Common.JoinExtensions;

namespace Elevator.Subtranslator.Reporter
{

	class Options
	{
		[Option('t', "target", Required = true, HelpText = "Path to folder of target localization.")]
		public string TargetPath { get; set; }

		[Option('e', "etalon", Required = true, HelpText = "Path to folder of etalon localization.")]
		public string EtalonPath { get; set; }

		[Option('x', "except", Required = false, HelpText = "DefType exceptions")]
		public string DefTypeExceptions { get; set; }

		[Option('o', "output", Required = true, HelpText = "CSV file output path.")]
		public string ReportPath { get; set; }
	}

	class GroupedResult
	{
		public string GroupName { get; private set; }
		public string Key { get; private set; }
		public string EtalonValue { get; private set; }
		public string TargetValue { get; private set; }

		public GroupedResult(string groupName, string key, string etalon, string target)
		{
			GroupName = groupName;
			Key = key;
			EtalonValue = etalon;
			TargetValue = target;
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			ParserResult<Options> parseResult = Parser.Default.ParseArguments<Options>(args);
			if (parseResult.Errors.Any())
				return;

			Options options = parseResult.Value;

			InjectionAnalyzer injectionAnaluzer = new InjectionAnalyzer();

			HashSet<string> exceptions = new HashSet<string>(options.DefTypeExceptions.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));

			InjectionPathComparer injComparer = new InjectionPathComparer();
			DefTypeComparer defTypeComparer = new DefTypeComparer();

			List<Injection> targetInjections = injectionAnaluzer.ReadInjections(Path.Combine(options.TargetPath, "DefInjected")).Where(inj => !exceptions.Contains(inj.DefType, defTypeComparer)).ToList();
			List<Injection> etalonInjections = injectionAnaluzer.ReadInjections(Path.Combine(options.EtalonPath, "DefInjected")).Where(inj => !exceptions.Contains(inj.DefType, defTypeComparer)).ToList();
			List<GroupedResult> injectionsJoinResult = etalonInjections.FullOuterJoin<Injection, Injection, Injection, GroupedResult>(targetInjections, tgt => tgt, et => et, MergeInjections, injComparer).ToList();
			List<IGrouping<string, GroupedResult>> injectionGroupedResult = injectionsJoinResult.GroupBy(inj => inj.GroupName).ToList();

			List<XmlEntry> targedKeyedEntries = ReadKeyedEntries(Path.Combine(options.TargetPath, "Keyed")).ToList();
			List<XmlEntry> etalonKeyedEntries = ReadKeyedEntries(Path.Combine(options.EtalonPath, "Keyed")).ToList();
			List<GroupedResult> keyedJoinResult = etalonKeyedEntries.FullOuterJoin<XmlEntry, XmlEntry, string, GroupedResult>(targedKeyedEntries, tgt => tgt.Key, et => et.Key, MergeKeyedEntries, StringComparer.InvariantCultureIgnoreCase).ToList();
			List<IGrouping<string, GroupedResult>> keyedGroupedResult = keyedJoinResult.GroupBy(inj => inj.GroupName).ToList();

			using (StreamWriter sw = File.CreateText(options.ReportPath))
			{
				sw.WriteLine();
				sw.WriteLine("Items to translate:");

				foreach (IGrouping<string, GroupedResult> group in keyedGroupedResult)
				{
					sw.WriteLine();
					sw.WriteLine(group.Key);
					foreach (GroupedResult result in group)
					{
						sw.WriteLine("{0}\t{1}\t{2}", result.Key, result.EtalonValue, result.TargetValue);
					}
				}
				sw.Close();
			}
		}

		static GroupedResult MergeInjections(Injection etalon, Injection target)
		{
			return new GroupedResult(
				etalon == null ? target.DefType : etalon.DefType,
				etalon == null ? target.DefPath : etalon.DefPath,
				etalon == null ? null : etalon.Translation,
				target == null ? null : target.Translation);
		}

		static GroupedResult MergeKeyedEntries(XmlEntry etalon, XmlEntry target)
		{
			return new GroupedResult(
				etalon == null ? target.Group : etalon.Group,
				etalon == null ? target.Key : etalon.Key,
				etalon == null ? null : etalon.Value,
				target == null ? null : target.Value);
		}

		static IEnumerable<XmlEntry> ReadKeyedEntries(string keyedPath)
		{
			DirectoryInfo keyedDir = new DirectoryInfo(keyedPath);
			foreach (FileInfo file in keyedDir.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
			{
				XDocument injectionsDoc;
				try
				{
					injectionsDoc = XDocument.Load(file.FullName);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					Console.WriteLine("FIle {0} was impossible to read", file.FullName);
					continue;
				}
				XElement languageData = injectionsDoc.Root;

				foreach (XElement keyedElement in languageData.Elements())
				{
					yield return new XmlEntry(file.Name, keyedElement.Name.LocalName, keyedElement.Value);
				}

			}

		}

	}
}

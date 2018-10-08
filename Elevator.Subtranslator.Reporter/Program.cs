using CommandLine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Elevator.Subtranslator.Common;
using Elevator.Subtranslator.Common.JoinExtensions;
using System.Xml;

namespace Elevator.Subtranslator.Reporter
{

	class Options
	{
		[Option('t', "target", Required = true, HelpText = "Path to folder of target localization.")]
		public string TargetPath { get; set; }

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

			List<GroupedResult> keyedEntries = ReadCommentedKeyedValues(Path.Combine(options.TargetPath, "Keyed")).ToList();
			List<GroupedResult> injectedEntries = ReadCommentedInjections(Path.Combine(options.TargetPath, "DefInjected")).Where(inj => !exceptions.Contains(inj.GroupName, defTypeComparer)).ToList();

            List<IGrouping<string, GroupedResult>> keyedGroupedResult = keyedEntries.GroupBy(inj => inj.GroupName).ToList();
            List<IGrouping<string, GroupedResult>> injectedGroupedResult = injectedEntries.GroupBy(inj => inj.GroupName).ToList();

            using (StreamWriter sw = File.CreateText(options.ReportPath))
			{
                sw.WriteLine();
                sw.WriteLine("Injected items:");

                foreach (IGrouping<string, GroupedResult> group in injectedGroupedResult)
                {
                    sw.WriteLine();
                    sw.WriteLine(group.Key);
                    foreach (GroupedResult result in group)
                    {
                        sw.WriteLine("{0}\t{1}\t{2}", result.Key, result.EtalonValue, result.TargetValue);
                    }
                }
                sw.WriteLine();
                sw.WriteLine("Keyed items:");

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

		static IEnumerable<GroupedResult> ReadCommentedKeyedValues(string path)
		{
			DirectoryInfo keyedDir = new DirectoryInfo(path);
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

                languageData.Nodes().Where(node => node.NodeType == XmlNodeType.Comment);


                foreach (XElement keyedElement in languageData.Elements())
				{
                    XComment comment = keyedElement.PreviousNode as XComment;
                    string etalon = comment == null ? string.Empty : comment.Value.Replace(" EN: ", string.Empty);

                    yield return new GroupedResult(file.Name, keyedElement.Name.LocalName, etalon, keyedElement.Value);
				}
			}
		}

        static IEnumerable<GroupedResult> ReadCommentedInjections(string injectionsDirPath)
        {
            List<GroupedResult> results = new List<GroupedResult>();

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
                            XComment comment = injection.PreviousNode as XComment;
                            string etalon = comment == null ? string.Empty : comment.Value.Replace(" EN: ", string.Empty);
                            results.Add(new GroupedResult(defType, injection.Name.LocalName, etalon, injection.Value));
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
            return results;
        }


    }
}

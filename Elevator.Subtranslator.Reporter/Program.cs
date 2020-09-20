using CommandLine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Elevator.Subtranslator.Common;
using System.Xml;
using System.Text.RegularExpressions;

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

	class Entry
	{
		public string Category { get; private set; }
		public string Key { get; private set; }
		public string EtalonValue { get; private set; }
		public string TargetValue { get; private set; }

		public Entry(string category, string key, string etalon, string target)
		{
			Category = category;
			Key = key;
			EtalonValue = etalon;
			TargetValue = target;
		}
	}

	/// <summary>
	/// Creates a .tsv table with english and russian enries for overall translation readouts
	/// </summary>
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

			List<Entry> keyedEntries = ReadCommentedKeyedValues(Path.Combine(options.TargetPath, "Keyed")).ToList();
			List<Entry> injectedEntries = ReadCommentedInjections(Path.Combine(options.TargetPath, "DefInjected")).Where(inj => !exceptions.Contains(inj.Category, defTypeComparer)).ToList();

			using (StreamWriter sw = File.CreateText(options.ReportPath))
			{
				WriteEntriesGrouped(injectedEntries.Concat(keyedEntries), sw);
				sw.Close();
			}
		}

		static void WriteEntriesGrouped(IEnumerable<Entry> entries, StreamWriter sw)
		{
			List<IGrouping<string, Entry>> groupedEntries = entries.GroupBy(inj => inj.Category).ToList();

			foreach (IGrouping<string, Entry> group in groupedEntries)
			{
				sw.WriteLine(group.Key);
				foreach (Entry entry in group)
				{
					sw.WriteLine($"{entry.Key}\t{entry.EtalonValue}\t{entry.TargetValue}");
				}
			}
		}

		static void WriteEntriesDirect(IEnumerable<Entry> entries, StreamWriter sw)
		{
			foreach (Entry entry in entries)
			{
				sw.WriteLine($"{entry.Category}\t{entry.Key}\t{entry.EtalonValue}\t{entry.TargetValue}");
			}
		}

		private static readonly Regex _englishComment = new Regex(@"^ EN: (.*?) $", RegexOptions.Compiled);

		static IEnumerable<Entry> ReadCommentedKeyedValues(string path)
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

					string etalon = comment == null ? string.Empty : _englishComment.Match(comment.Value).Groups[1].Value;
					yield return new Entry(file.Name, keyedElement.Name.LocalName, etalon, keyedElement.Value);
				}
			}
		}

		static IEnumerable<Entry> ReadCommentedInjections(string injectionsDirPath)
		{
			List<Entry> results = new List<Entry>();

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

							string etalon = comment == null ? string.Empty : _englishComment.Match(comment.Value).Groups[1].Value;
							results.Add(new Entry(defType, injection.Name.LocalName, etalon, injection.Value));
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

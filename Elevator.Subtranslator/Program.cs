using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.IO;
using System.Xml.Linq;

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

			SaveXml(mergedDoc, Path.Combine(options.OutputLocation, "Defs.xml"));
		}

		static XDocument MergeDefs(string defsFullPath)
		{
			XDocument mergedXml = new XDocument();
			XElement mergedDefs = new XElement("Defs");
			mergedXml.Add(mergedDefs);

			foreach (string defFilePath in Directory.EnumerateFiles(defsFullPath, "*.xml", SearchOption.AllDirectories))
			{
				XDocument defXml = LoadXml(defFilePath);
				XElement defs = defXml.Root;

				foreach (XElement def in defs.Elements())
				{
					mergedDefs.Add(new XElement(def));
				}
			}
			return mergedXml;
		}

		static XDocument LoadXml(string filename)
		{
			using (StreamReader reader = File.OpenText(filename))
			{
				return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
			}
		}

		static void SaveXml(XDocument doc, string filename)
		{

			string output = doc.ToString(SaveOptions.None);
			File.WriteAllText(filename, output);
		}
	}
}

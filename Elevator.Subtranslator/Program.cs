using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.IO;

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

			IEqualityComparer<string> defDirNameComparer = new DefDirectoryNameComparer();
			Dictionary<string, string> injectionDirNameMap = new Dictionary<string, string>(defDirNameComparer);
			foreach (string injDefDirFullPath in Directory.EnumerateDirectories(options.InjectionsLocation))
			{
				injectionDirNameMap[Path.GetFileName(injDefDirFullPath)] = injDefDirFullPath;
			}

			foreach (string defDirFullPath in Directory.EnumerateDirectories(options.DefsLocation))
			{
				string defDir = Path.GetFileName(defDirFullPath);

				string injDir = injectionDirNameMap.Keys.FirstOrDefault(key => defDirNameComparer.Equals(defDir, key));
				if (string.IsNullOrEmpty(injDir))
				{
					Console.WriteLine("{0} -> ---", defDir);
				}
				else
				{
					Console.WriteLine("{0} -> {1}", defDir, injDir);
				}
			}
		}
	}
}

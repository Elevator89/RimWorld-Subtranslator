using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elevator.Subtranslator.Common
{
	public static class FileUtil
	{
		public static void PushLine(string filePath, string line)
		{
			File.AppendAllLines(filePath, new string[] { line });
		}

		public static string PopLine(string filePath)
		{
			List<string> lines = File.ReadAllLines(filePath).ToList();
			string line = lines[lines.Count - 1];
			lines.RemoveAt(lines.Count - 1);
			File.WriteAllLines(filePath, lines);
			return line;
		}

		public static void DeleteLine(string filePath)
		{
			List<string> lines = File.ReadAllLines(filePath).ToList();
			lines.RemoveAt(lines.Count - 1);
			File.WriteAllLines(filePath, lines);
		}
	}
}

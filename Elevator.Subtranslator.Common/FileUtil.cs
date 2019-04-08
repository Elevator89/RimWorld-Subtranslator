using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elevator.Subtranslator.Common
{
	public static class FileUtil
	{
		public static void AppendLine(string filePath, string line)
		{
			File.AppendAllLines(filePath, new string[] { line });
		}

		public static void DeleteLine(string filePath)
		{
			List<string> lines = File.ReadAllLines(filePath).ToList();
			lines.RemoveAt(lines.Count - 1);
			File.WriteAllLines(filePath, lines);
		}
	}
}

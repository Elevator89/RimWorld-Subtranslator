using System;

namespace Elevator.Subtranslator.Common
{
	public class Injection
	{
		public string DirectoryName { get; private set; }
		public string DefType { get; private set; }
		public string DefPath { get; private set; }
		public string[] DefPathParts { get; private set; }
		public string Original { get; set; }
		public string Translation { get; set; }

		public Injection(string directoryName, string defType, string defPath)
		{
			DirectoryName = directoryName;
			DefType = defType;
			DefPath = defPath;
			DefPathParts = DefPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			Original = null;
			Translation = null;
		}

		public Injection(string directoryName, string defType, string[] defPathParts)
		{
			DirectoryName = directoryName;
			DefType = defType;
			DefPath = string.Join(",", defPathParts);
			DefPathParts = defPathParts;
			Original = null;
			Translation = null;
		}
	}
}

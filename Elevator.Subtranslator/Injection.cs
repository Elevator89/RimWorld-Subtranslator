namespace Elevator.Subtranslator
{
	public class Injection
	{
		public string DirectoryName { get; private set; }
		public string DefType { get; private set; }
		public string DefPath { get; private set; }
		public string Original { get; set; }
		public string Translation { get; set; }

		public Injection(string directoryName, string defType, string defPath)
		{
			DirectoryName = directoryName;
			DefType = defType;
			DefPath = defPath;
			Original = null;
			Translation = null;
		}
	}
}

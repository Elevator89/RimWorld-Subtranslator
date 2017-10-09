namespace Elevator.Subtranslator
{
	public class Injection
	{
		public string DefType { get; private set; }
		public string DefPath { get; private set; }
		public string Original { get; set; }
		public string Translation { get; set; }

		public Injection(string defType, string defPath)
		{
			DefType = defType;
			DefPath = defPath;
			Original = null;
			Translation = null;
		}
	}
}

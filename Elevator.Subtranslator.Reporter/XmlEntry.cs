namespace Elevator.Subtranslator.Reporter
{
	public class XmlEntry
	{
		public string Group { get; private set; }
		public string Key { get; private set; }
		public string Value { get; private set; }

		public XmlEntry(string path, string name, string value)
		{
			Group = path;
			Key = name;
			Value = value;
		}
	}
}

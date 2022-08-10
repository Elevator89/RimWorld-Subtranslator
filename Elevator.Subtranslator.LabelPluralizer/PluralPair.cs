namespace Elevator.Subtranslator.LabelPluralizer
{
	public class PluralPair
	{
		public string Singular { get; }
		public string Plural { get; }

		public PluralPair(string singular, string plural)
		{
			Singular = singular;
			Plural = plural;
		}
	}
}

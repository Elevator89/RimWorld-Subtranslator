using System;
using Cyriller;
using Cyriller.Model;

namespace Elevator.Subtranslator.DeclinationTools
{
	public class CaseTools
	{
		public static CyrResult Decline(CyrPhrase decliner, string phrase)
		{
			CyrResult result = DeclineSingular(decliner, phrase);
			return result ?? DeclinePlural(decliner, phrase);
		}

		public static bool TryDecline(CyrPhrase decliner, string phrase, out CyrResult result)
		{
			return
				TryDeclineSingular(decliner, phrase, out result) ||
				TryDeclinePlural(decliner, phrase, out result);
		}

		public static bool TryDeclineSingular(CyrPhrase decliner, string phrase, out CyrResult result)
		{
			result = DeclineSingular(decliner, phrase);
			return result != null;
		}

		public static CyrResult DeclineSingular(CyrPhrase decliner, string phrase)
		{
			try
			{
				return decliner.Decline(phrase, GetConditionsEnum.Strict);
			}
			catch
			{
				return null;
			}
		}

		public static bool TryDeclinePlural(CyrPhrase decliner, string phrase, out CyrResult result)
		{
			result = DeclinePlural(decliner, phrase);
			return result != null;
		}

		public static CyrResult DeclinePlural(CyrPhrase decliner, string phrase)
		{
			try
			{
				return decliner.DeclinePlural(phrase, GetConditionsEnum.Strict);
			}
			catch
			{
				return null;
			}
		}
	}
}

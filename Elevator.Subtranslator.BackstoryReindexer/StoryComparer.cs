using System.Collections.Generic;

namespace Elevator.Subtranslator.BackstoryReindexer
{
    public class StoryComparer : IEqualityComparer<Story>
    {
		public bool Equals(Story x, Story y)
		{
			return
				x.Title.ToLower() == y.Title.ToLower() &&
				x.TitleShort.ToLower() == y.TitleShort.ToLower() &&
				ReplaceVars(x.Description.ToUpper()) == ReplaceVars(y.Description.ToUpper());
		}

		//public bool Equals(Story x, Story y)
		//{
		//	string descX = ReplaceVars(x.Description).ToUpper();
		//	string descY = ReplaceVars(y.Description).ToUpper();

		//	string titleX = x.Title.ToLower();
		//	string titleY = y.Title.ToLower();

		//	string titleShortX = x.TitleShort.ToLower();
		//	string titleShortY = y.TitleShort.ToLower();

		//	return
		//		titleX == titleY &&
		//		titleShortX == titleShortY &&
		//		descX == descY;
		//}

		private static string ReplaceVars(string input)
		{
			return input
				.Replace("NAME", "[PAWN_nameDef]")
				.Replace("HECAP", "[PAWN_pronoun]")
				.Replace("HE", "[PAWN_pronoun]")
				.Replace("HISCAP", "[PAWN_possessive]")
				.Replace("HIS", "[PAWN_possessive]")
				.Replace("HIMCAP", "[PAWN_objective]")
				.Replace("HIM", "[PAWN_objective]")
				//.Replace("hecap", "[PAWN_pronoun]")
				//.Replace("he", "[PAWN_pronoun]")
				//.Replace("hiscap", "[PAWN_possessive]")
				//.Replace("his", "[PAWN_possessive]")
				//.Replace("himcap", "[PAWN_objective]")
				//.Replace("him", "[PAWN_objective]")
				;
		}

        public int GetHashCode(Story obj)
        {
            return (obj.Title.ToLower() + obj.TitleShort.ToLower() + ReplaceVars(obj.Description.ToUpper())).GetHashCode();
        }
    }
}

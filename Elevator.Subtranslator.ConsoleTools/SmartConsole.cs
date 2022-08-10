using System;

namespace Elevator.Subtranslator.ConsoleTools
{
	public class SmartConsole
	{
		public static string EditLine(string defaultValue, int cursorRelativePosBefore, out int cursorRelativePosAfter)
		{
			int cursorBase = Console.CursorLeft;
			string value = defaultValue;
			int cursorRelative = cursorRelativePosBefore == -1 ? value.Length : Math.Min(cursorRelativePosBefore, value.Length);

			while (true)
			{
				Write(value, cursorBase, cursorRelative);

				ConsoleKeyInfo info = Console.ReadKey(true);
				char c = info.KeyChar;

				if (info.Key == ConsoleKey.Enter)
				{
					Console.Write(Environment.NewLine);
					cursorRelativePosAfter = cursorRelative;
					return value;
				}

				if (info.Key == ConsoleKey.Backspace && cursorRelative > 0)
				{
					cursorRelative--;
					value = value.Remove(cursorRelative, 1);
				}
				else if (info.Key == ConsoleKey.Delete && cursorRelative < value.Length)
				{
					value = value.Remove(cursorRelative, 1);
				}
				else if (info.Key == ConsoleKey.LeftArrow && cursorRelative > 0)
				{
					if (info.Modifiers == ConsoleModifiers.Control)
					{
						// Skip nearby whitespaces
						int startIndex = cursorRelative - 1;
						while (value[startIndex] == ' ' && startIndex > 0)
							startIndex--;

						int prevWordStart = value.LastIndexOfAny(new[] { ' ', '-' }, startIndex);
						cursorRelative = prevWordStart == -1 ? 0 : prevWordStart;
					}
					else
					{
						cursorRelative--;
					}
				}
				else if (info.Key == ConsoleKey.Home)
				{
					cursorRelative = 0;
				}
				else if (info.Key == ConsoleKey.End)
				{
					cursorRelative = value.Length;
				}
				else if (info.Key == ConsoleKey.RightArrow && cursorRelative < value.Length)
				{
					if (info.Modifiers == ConsoleModifiers.Control)
					{
						// Skip nearby whitespaces
						int startIndex = cursorRelative;
						while (value[startIndex] == ' ' && startIndex > 0)
							startIndex++;

						int nextWordStart = value.IndexOfAny(new[] { ' ', '-' }, startIndex);
						cursorRelative = nextWordStart == -1 ? value.Length : nextWordStart;
					}
					else
					{
						cursorRelative++;
					}
				}
				else if (char.IsLetterOrDigit(c) || char.IsSeparator(c) || char.IsSymbol(c) || char.IsPunctuation(c))
				{
					if (cursorRelative < value.Length)
					{
						value = value.Insert(cursorRelative, $"{c}");
						cursorRelative++;
					}
					else
					{
						value += c;
						cursorRelative++;
					}
				}
			}
		}

		private static void Write(string value, int cursorBase, int cursorRelative)
		{
			Console.CursorLeft = cursorBase;
			Console.Write(value + " ");
			Console.CursorLeft = cursorBase + cursorRelative;
		}
	}
}

﻿using System;
using System.Text.RegularExpressions;

namespace Elevator.Subtranslator.LabelDecliner
{
	public static class ConsoleTools
	{
		private static readonly Regex _wordStartRegex = new Regex(@"\s\w", RegexOptions.Compiled);

		public static string ReadLine(string defaultValue)
		{
			int cursorBase = Console.CursorLeft;
			string value = defaultValue;
			int cursorRelative = value.Length;

			while (true)
			{
				Write(value, cursorBase, cursorRelative);

				ConsoleKeyInfo info = Console.ReadKey(true);
				char c = info.KeyChar;

				if (info.Key == ConsoleKey.Enter)
				{
					Console.Write(Environment.NewLine);
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

						int prevWordStart = value.LastIndexOf(' ', startIndex);
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

						int nextWordStart = value.IndexOf(' ', startIndex);
						cursorRelative = nextWordStart == -1 ? value.Length : nextWordStart;
					}
					else
					{
						cursorRelative++;
					}

					//cursorRelative = info.Modifiers == ConsoleModifiers.Control
					//	? value.IndexOf(' ', cursorRelative)
					//	: cursorRelative + 1;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Fclp;
using System.Xml.Linq;
using System.IO;

namespace Elevator.Subtranslator.BackstoryTitleWidthChecker
{
	class Program
	{
		public class ApplicationArguments
		{
			public string BackstoriesFileName { get; set; }
			public int MaxWidth { get; set; }
			public string ReportOutputFileName { get; set; }
		}

		public class BackstoryLine
		{
			public string Title { get; set; }
			public float Width { get; set; }
		}

		/// <summary>
		/// Very useful utility to check if width of backstory titles in pixels. Check Verse.Text class for more correct work. Width of field is 160px
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			// create a generic parser for the ApplicationArguments type
			var p = new FluentCommandLineParser<ApplicationArguments>();

			p.Setup(arg => arg.BackstoriesFileName)
				.As('b', "backstories")
				.Required()
				.WithDescription("Backstories original file path");

			p.Setup(arg => arg.MaxWidth)
				.As('w', "width")
				.Required()
				.WithDescription("Max line width");

			p.Setup(arg => arg.ReportOutputFileName)
				.As('o', "output")
				.Required()
				.WithDescription("Report file path");

			ICommandLineParserResult result = p.Parse(args);

			if (result.HasErrors)
			{
				Console.WriteLine(result.ErrorText);
				return;
			}

			XDocument backstoriesDoc = XDocument.Load(p.Object.BackstoriesFileName);

			Font font = new Font("Arial", 10.5f);
			Bitmap bmp = new Bitmap(500, 100, PixelFormat.Format32bppPArgb);

			float maxWidth = p.Object.MaxWidth;

			List<BackstoryLine> oversizedBackstories = new List<BackstoryLine>();

			using (Graphics graphics = Graphics.FromImage(bmp))
			{
				Brush background = new SolidBrush(Color.FromArgb(255, Color.Black));
				Brush foreground = new SolidBrush(Color.White);

				graphics.FillRectangle(background, new RectangleF(0, 0, bmp.Width, bmp.Height));

				//string iii = "I i i i i i i i i i i i i i i i i i i i i i i i i";
				//SizeF stringSize = graphics.MeasureString(iii, font);
				//graphics.DrawRectangle(new Pen(Color.Red, 1), 0f, 0f, stringSize.Width, stringSize.Height);
				//graphics.DrawString(iii, font, foreground, new PointF(0, 0));

				foreach (XElement storyElem in backstoriesDoc.Root.Elements())
				{
					string title = CapitalizeFirst(storyElem.Element("title").Value);
					float titleWidth = graphics.MeasureString(title, font).Width;

					if (titleWidth > maxWidth)
					{
						oversizedBackstories.Add(
							new BackstoryLine()
							{
								Title = title,
								Width = titleWidth,
							});
					}

					string titleFemale = storyElem.Element("titleFemale")?.Value;
					if (titleFemale != null)
					{
						titleFemale = CapitalizeFirst(titleFemale);
						float titleFemaleWidth = graphics.MeasureString(titleFemale, font).Width;

						if (titleFemaleWidth > maxWidth)
						{
							oversizedBackstories.Add(
								new BackstoryLine()
								{
									Title = titleFemale,
									Width = titleFemaleWidth,
								});
						}

					}
				}
			}

			oversizedBackstories = oversizedBackstories.OrderByDescending(bs => bs.Width).ToList();

			using (TextWriter writer = File.CreateText(p.Object.ReportOutputFileName))
			{
				writer.WriteLine("Width > {0}:", maxWidth);

				foreach (BackstoryLine line in oversizedBackstories)
				{
					writer.WriteLine("{0:0.0}\t{1}", line.Width, line.Title);
				}

				writer.Close();
			}
		}

		static string CapitalizeFirst(string input)
		{
			char[] chars = input.ToCharArray();
			chars[0] = char.ToUpper(chars[0]);

			return new string(chars);
		}
	}
}

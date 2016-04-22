using System;
using SharpFont;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Crow
{
	public class glFont
	{
		Dictionary<int, glGlyph> glyphs = new Dictionary<int,glGlyph>();

		public static void dumpFontsDirectories(string _path)
		{
			List<string> stylesList = new List<string> ();
			List<string> namesList = new List<string> ();
			List<string> attribsList = new List<string> ();

			DirectoryInfo dir = new DirectoryInfo (_path);
			try
			{
				using (Library lib = new Library())
				{
					Console.WriteLine("FreeType version: " + lib.Version + "\n");

					foreach (FileInfo f in dir.GetFiles("*.ttf",SearchOption.AllDirectories)) {
						try {
							
							Face face = new Face(lib,f.FullName, 0);

							Console.Write(System.IO.Path.GetFileNameWithoutExtension(f.Name) + " => ");
							Console.WriteLine("{0} Faces:{1} Flags:{2}\nStyle:{3} StyleFlags:{4}\n",
								face.FamilyName,
								face.FaceCount,
								face.FaceFlags,
								face.StyleName,
								face.StyleFlags);
							string[] styles = face.StyleName.Split(' ');
							foreach (string s in styles) {
								if (stylesList.Contains(s))
									continue;
								stylesList.Add(s);
							}

							string[] names = face.FamilyName.Split(' ');
							if (!namesList.Contains(names[0]))
								namesList.Add(names[0]);
							for (int i = 1; i < names.Length; i++) {
								if (attribsList.Contains(names[i]))
									continue;
								attribsList.Add(names[i]);
							}

							face.Dispose();
						}
						catch (FreeTypeException ee)
						{
							Console.Write(ee.Error.ToString() + ": " + f.FullName);
						}
					}
				}
			}
			catch (FreeTypeException e)
			{
				Console.Write(e.Error.ToString());
			}
			Console.Write ("Names => ");
			foreach (string s in namesList) {
				Console.Write (s + " ");
			}
			Console.Write ("\n\nAttrib => ");
			foreach (string s in attribsList) {
				Console.Write (s + " ");
			}

			Console.Write ("\n\nStyles => ");
			foreach (string s in stylesList) {
				Console.Write (s + " ");
			}
		}

		public glFont (string _fontPath)
		{

		}

	}
}


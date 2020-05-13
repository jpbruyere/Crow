// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using Crow;

namespace PerfTests
{
	class TestInterface : Interface
	{
		public TestInterface (int width = 800, int height = 600)
			: base (width, height, false, false)
		{
			surf = new Crow.Cairo.ImageSurface (Crow.Cairo.Format.Argb32, ClientRectangle.Width, ClientRectangle.Height);
			Init ();
		}

		long Test (string path, out long min, out long max, int count = 10)
		{
			min = long.MaxValue;
			max = long.MinValue;

			long total = 0;

			Stopwatch sw = new Stopwatch ();

			for (int i = 0; i < count; i++) {
				sw.Restart ();


				Load (path);
				while (LayoutingQueue.Count > 0)
					Update ();

				sw.Stop ();

				if (sw.ElapsedTicks < min)
					min = sw.ElapsedMilliseconds;
				if (sw.ElapsedTicks > max)
					max = sw.ElapsedMilliseconds;
				total += sw.ElapsedMilliseconds;

			}

			return total / count;
		}

		void testDir (string dirPath, int level = 0)
		{
			Console.WriteLine ($"{new string (' ', level * 4)}-{dirPath}");
			level++;

			foreach (string d in Directory.GetDirectories (dirPath)) 
				testDir (d, level);

			foreach (string f in Directory.GetFiles (dirPath)) {
				try {
					long mean = Test (f, out long min, out long max);
					Console.WriteLine ($"{new string (' ', level * 4)}{ Path.GetFileName (f),-30}: {min,5} |{mean,5} |{max,5}");
				} catch (Exception ex) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ($"{ex.Message}");
					Console.ResetColor ();
				}
			}
		}


		public static void Main (string [] args)
		{
			//IndentedTextWriter w = new IndentedTextWriter()
			using (TestInterface iface = new TestInterface ()) {
				iface.testDir ("Interfaces");
			}
		}
	}
}

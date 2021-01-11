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
		readonly int count;
		readonly bool miliseconds = false;
		readonly bool resetStylesAndTemplates = true;
		readonly string outDirectory;

		Stream outStream;
		TextWriter writer;

		bool logToDisk => writer != null;

		void writeHeader (TextWriter txtWriter) {
			txtWriter.WriteLine ($"Crow perf test ({clientRectangle.Width} X {clientRectangle.Height}), repeat count = {count}, reset style and templates = {resetStylesAndTemplates}");
			txtWriter.WriteLine ($"git:{ThisAssembly.Git.Commit} {ThisAssembly.Git.Branch} {ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch} {ThisAssembly.Git.SemVer.Label}");
		}
		public TestInterface (int count, string _outDirectory = "", int width = 800, int height = 600)
			: base (width, height, false, false)
		{
			this.count = count;

			if (string.IsNullOrEmpty (_outDirectory)) {
				writeHeader (Console.Out);
				Console.WriteLine (new string ('-', 100));
				Console.WriteLine ($"{" Path",-50}|   Min    |   Mean   |   Max    | Alloc(kb)| Lost(Kb) |");
			} else {
				string dirName = Path.IsPathRooted (_outDirectory) ? _outDirectory :
					Path.Combine (Directory.GetCurrentDirectory (), _outDirectory);
				if (!Directory.Exists (dirName))
					Directory.CreateDirectory (dirName);

				string filename = "crow-" + DateTime.Now.ToString ("yyyy-MM-dd-HH-mm") +".perflog";
				outStream = new FileStream (Path.Combine (dirName, filename), FileMode.Create);
				writer = new StreamWriter (outStream);
				writeHeader (writer);
				writer.WriteLine ("Path;Min;Mean;Max;Allocated;LostMem");
			}

			surf = new Crow.Cairo.ImageSurface (Crow.Cairo.Format.Argb32, ClientRectangle.Width, ClientRectangle.Height);
			Init ();
		}

        protected override void Dispose (bool disposing) {
            base.Dispose (disposing);

			if (logToDisk) {
				writer.Dispose ();
				outStream.Dispose ();
            }
        }

        long Test (string path, out long min, out long max)
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

				ClearInterface ();
				if (resetStylesAndTemplates) {
					this.Styling.Clear ();
					this.StylingConstants.Clear ();
					this.Instantiators.Clear ();
					this.Templates.Clear ();
					this.DefaultTemplates.Clear ();
					this.DefaultValuesLoader.Clear ();
					GC.Collect ();
                }

				if (miliseconds) {
					if (sw.ElapsedMilliseconds < min)
						min = sw.ElapsedMilliseconds;
					if (sw.ElapsedMilliseconds > max)
						max = sw.ElapsedMilliseconds;
					total += sw.ElapsedMilliseconds;
                } else {
					if (sw.ElapsedTicks < min)
						min = sw.ElapsedTicks;
					if (sw.ElapsedTicks > max)
						max = sw.ElapsedTicks;
					total += sw.ElapsedTicks;
				}

			}

			return total / count;
		}

		void testDir (string dirPath, int level = 0)
		{
			string label = $"{new string (' ', level * 4)}- {dirPath}";
			if (!logToDisk)
				Console.WriteLine (label, -50);
			level++;

			foreach (string d in Directory.GetDirectories (dirPath)) 
				testDir (d, level);

			foreach (string f in Directory.GetFiles (dirPath, "*.crow")) {
				label = $"{new string (' ', level * 4)}{ Path.GetFileName (f)}";
				try {
					long totMemBefore = GC.GetTotalMemory (true);
					long allocBefore = GC.GetTotalAllocatedBytes (true);
					long mean = Test (f, out long min, out long max);
					long allocAfter = GC.GetTotalAllocatedBytes (true);
					long totMemAfter = GC.GetTotalMemory (true);
					double allocated = (double)(allocAfter - allocBefore) / 1024.0;
					double lostMem = (double)(totMemAfter - totMemBefore) / 1024.0;
					if (logToDisk) {
						writer.WriteLine ($"{f};{min};{mean};{max};{allocAfter - allocBefore};{totMemAfter - totMemBefore}");
                    } else {
						if (miliseconds)
							Console.WriteLine ($"{label,-50}|{min,10}|{mean,10}|{max,10}| {allocated,8:0.0} | {lostMem,8:0.0} |");
						else
							Console.WriteLine ($"{label,-50}|{0.001 * min,10:0.00}|{0.001 * mean,10:0.00}|{0.001 * max,10:0.00}| {allocated,8:0.0} | {lostMem,8:0.0} |");
					}
				} catch (Exception ex) {
					if (logToDisk) {
						writer.WriteLine ($"{f};{ex.Message}");
					} else {
						Console.WriteLine ($"{label,-50}");
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine ($"\t\t{ex.Message}");
						Console.ResetColor ();
					}
				}
			}
		}
		

		public static void Main (string [] args)
		{
			int count = 10;
			string outDir = "";

			if (args.Length > 0 && int.TryParse (args[0], out int c))
				count = c;
			if (args.Length > 1)
				outDir = args[1];

			using (TestInterface iface = new TestInterface (count, outDir)) {
				iface.testDir (@"Interfaces");
			}
		}
	}
}

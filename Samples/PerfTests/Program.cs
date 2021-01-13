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
		readonly int count = 1, updateCycles = 0;
		readonly bool miliseconds = false;
		readonly bool resetItors = false;
		readonly bool screenOutput = false;
		readonly string inDirectory = "Interfaces";//directory to test


        Stream outStream;
		TextWriter writer;

		bool logToDisk => writer != null;

		void writeHeader (TextWriter txtWriter) {
			txtWriter.WriteLine ($"Crow perf test ({clientRectangle.Width} X {clientRectangle.Height}), repeat count = {count}, reset style and templates = {resetItors}");
			txtWriter.WriteLine ($"git:{ThisAssembly.Git.Commit} {ThisAssembly.Git.Branch} {ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch} {ThisAssembly.Git.SemVer.Label}");
		}

		void printHelp () {
			Console.WriteLine ("Usage: PerfTests.exe [options]\n");
			Console.WriteLine ("-o,--output:\n\tWrite results to output directory, if omited, results are printed to screen.");
			Console.WriteLine ("-i,--input:\n\tInput directory to search recursively for '.crow' file to test.");
			Console.WriteLine ("-w,--width:\n\toutput surface width, not displayed on screen.");
			Console.WriteLine ("-h,--height:\n\toutput surface height, not displayed on screen.");
			Console.WriteLine ("-c,--count:\n\trepeat each test 'c' times.");
			Console.WriteLine ("-m,--millisec:\n\tenable measure time in milisecond, if omitted measure in ticks.");
			Console.WriteLine ("-r,--reset:\n\tenable clear iterators after each test file.");
			Console.WriteLine ("-u,--update:\n\tmeasure 'n' update cycle with DateTime.Now string notified.");
			Console.WriteLine ("-s,--screen:\n\tenable output to screen.");
		}

		public TestInterface (string[] args, int width = 800, int height = 600)
			: base (width, height, false, false)
		{
			string outDir = null;

			int i = 0;
			try {
				while (i < args.Length) {
					switch (args[i++]) {
					case "-o":
					case "--output":
						outDir = args[i++];
						break;
					case "-i":
					case "--input":
						inDirectory = args[i++];
						break;
					case "-c":
					case "--count":
						count = int.Parse (args[i++]);
						break;
					case "-w":
					case "--width":
						clientRectangle.Width = int.Parse (args[i++]);
						break;
					case "-h":
					case "--height":
						clientRectangle.Height = int.Parse (args[i++]);
						break;
					case "-m":
					case "--millisec":
						miliseconds = true;
						break;
					case "-r":
					case "--reset":
						resetItors = true;
						break;
					case "-u":
					case "--update":
						updateCycles = int.Parse (args[i++]);
						break;
					case "-s":
					case "--screen":
						screenOutput = true;
						break;

					default:						
						throw new Exception ();
					}
				}
			} catch (Exception) {
				printHelp ();
				throw;
			}

			if (string.IsNullOrEmpty (outDir)) {
				writeHeader (Console.Out);
				Console.WriteLine (new string ('-', 100));
				Console.WriteLine ($"{" Path",-50}|   Min    |   Mean   |   Max    | Alloc(kb)| Lost(Kb) |");
			} else {
				string dirName = Path.IsPathRooted (outDir) ? outDir :
					Path.Combine (Directory.GetCurrentDirectory (), outDir);
				if (!Directory.Exists (dirName))
					Directory.CreateDirectory (dirName);

				string filename = "crow-" + DateTime.Now.ToString ("yyyy-MM-dd-HH-mm") +".perflog";
				outStream = new FileStream (Path.Combine (dirName, filename), FileMode.Create);
				writer = new StreamWriter (outStream);
				writeHeader (writer);
				writer.WriteLine ("Path;Min;Mean;Max;Allocated;LostMem");
			}

			if (screenOutput)
				initSurface ();
			else
				surf = new Crow.Cairo.ImageSurface (Crow.Cairo.Format.Argb32, ClientRectangle.Width, ClientRectangle.Height);
			Init ();
		}

		public void PerformTests () {
			string dirName = Path.IsPathRooted (inDirectory) ? inDirectory :
				Path.Combine (Directory.GetCurrentDirectory (), inDirectory);
			if (!Directory.Exists (dirName))
				throw new FileNotFoundException ("Input directory not found: " + dirName);

			testDir (dirName);
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

				if (updateCycles == 0) {
					sw.Restart ();//dont measure load time when measuring updates
					Load (path);
				}else
					Load (path).DataSource = this;

				Update ();
				while (LayoutingQueue.Count > 0)
					Update ();

				if (updateCycles > 0) {
					sw.Restart ();
                    for (int j = 0; j < updateCycles; j++) {
						NotifyValueChanged ("elapsed", sw.ElapsedTicks);				
						Update ();
						while (LayoutingQueue.Count > 0)
							Update ();
					}
				}

				sw.Stop ();

				ClearInterface ();
				if (resetItors) {
					//this.Styling.Clear ();
					//this.StylingConstants.Clear ();
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
			string label = $"{new string (' ', level * 4)}- {Path.GetFileName (dirPath)}";
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
						Console.WriteLine ($"\t\t{ex.ToString()}");
						Console.ResetColor ();
					}
				}
			}
		}
		
		public static void Main (string [] args)
		{
            try {
				using (TestInterface iface = new TestInterface (args)) {
					iface.PerformTests ();
				}
			} catch (Exception) {
            }
		}
	}
}

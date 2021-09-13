// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using Crow;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Buffers;

namespace PerfTests
{
	class TestInterface : Interface
	{
#if NETCOREAPP
		static IntPtr resolveUnmanaged (Assembly assembly, String libraryName) {

			switch (libraryName)
			{
				case "glfw3":
					return  NativeLibrary.Load("glfw", assembly, null);
				case "rsvg-2.40":
					return  NativeLibrary.Load("rsvg-2", assembly, null);
			}
			Console.WriteLine ($"[UNRESOLVE] {assembly} {libraryName}");
			return IntPtr.Zero;
		}

		static TestInterface () {
			System.Runtime.Loader.AssemblyLoadContext.Default.ResolvingUnmanagedDll+=resolveUnmanaged;
		}
#endif
		readonly int count = 10, updateCycles = 0;
		readonly bool screenOutput = false;
		readonly string inDirectory = null;//directory to test
		readonly string outFilePath;


        Stream outStream;
		TextWriter writer;

		bool logToDisk => writer != null;

		enum Stage
        {
			ITor = 0,
			Instantiation,
			Add,
			Datasource,
			FirstUpdate,
			Update,
			Delete
        }
		Stage StartStage = Stage.ITor;
		Stage EndStage = Stage.Update;

		void writeHeader (TextWriter txtWriter) {
			txtWriter.WriteLine ($"Crow perf test ({clientRectangle.Width} X {clientRectangle.Height}), output to screen = { screenOutput }");
			txtWriter.WriteLine ($"repeat = {count}, {StartStage} -> {EndStage},  update cycles = {updateCycles}");
			txtWriter.WriteLine ($"git:{ThisAssembly.Git.Commit} {ThisAssembly.Git.Branch} {ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch} {ThisAssembly.Git.SemVer.Label}");
		}

		void printHelp () {
			Console.WriteLine ("Usage: PerfTests.exe [options]\n");
			Console.WriteLine ("-o,--output:\n\tWrite results to output directory, if omited, results are printed to screen.");
			Console.WriteLine ("-i,--input:\n\tInput directory to search recursively for '.crow' file to test. If ommitted, builtin unit tests are performs");
			Console.WriteLine ("-w,--width:\n\toutput surface width, not displayed on screen.");
			Console.WriteLine ("-h,--height:\n\toutput surface height, not displayed on screen.");
			Console.WriteLine ("-c,--count:\n\trepeat each test 'c' times. (default = 10, minimum = 5");

			Console.WriteLine ("-b,--begin:\n\tStarting stage for measures, may be the stage name or stage index");
            foreach (Stage s in Enum.GetValues(typeof(Stage))) {
				Console.WriteLine ($"\t\t\t- [{(int)s}] {s}");
			}
			Console.WriteLine ("-e,--end:\n\tEnding stage for measures, may be the stage name or stage index");
			Console.WriteLine ("-r,--reset:\n\tenable clear iterators after each test file.");
			Console.WriteLine ("-u,--update:\n\tmeasure 'n' update cycle with elapsed ticks string notified. (default = 0)");
			Console.WriteLine ("-s,--screen:\n\tenable output to screen.");
			Console.WriteLine ("--help:\n\tthis help message.");
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
						count = Math.Max(5, int.Parse (args[i++]));
						break;
					case "-w":
					case "--width":
						clientRectangle.Width = int.Parse (args[i++]);
						break;
					case "-h":
					case "--height":
						clientRectangle.Height = int.Parse (args[i++]);
						break;
					case "-b":
					case "--begin":
						if (!Enum.TryParse<Stage> (args[i++], out StartStage))
							StartStage = (Stage)int.Parse (args[i++]);
						break;
					case "-e":
					case "--end":
						if (!Enum.TryParse<Stage> (args[i++], out EndStage))
							EndStage = (Stage)int.Parse (args[i++]);
						break;
					case "-u":
					case "--update":
						updateCycles = int.Parse (args[i++]);
						break;
					case "-s":
					case "--screen":
						screenOutput = true;
						break;
					case "--help":
					default:
						throw new Exception ();
					}
				}
				if (EndStage < StartStage) {
					Console.WriteLine ($"Ending stage (){EndStage} is before Starting stage ({StartStage})");
					throw new Exception ();
				}

			} catch (Exception) {
				printHelp ();
				throw;
			}

			if (string.IsNullOrEmpty (outDir)) {
				writeHeader (Console.Out);
				Console.WriteLine (new string ('-', 100));
				Console.WriteLine ($"{" Path",-50}| Time(ms) | AllocKB  | MemKB  |  σ time  | σ Alloc| σ Mem  |");
			} else {
				string dirName = Path.IsPathRooted (outDir) ? outDir :
					Path.Combine (Directory.GetCurrentDirectory (), outDir);
				if (!Directory.Exists (dirName))
					Directory.CreateDirectory (dirName);

				string filename = "crow-" + DateTime.Now.ToString ("yyyy-MM-dd-HH-mm") +".perflog";
				outFilePath = Path.Combine (dirName, filename);
				outStream = new FileStream (outFilePath, FileMode.Create);
				writer = new StreamWriter (outStream);
				writeHeader (writer);
				writer.WriteLine ("Path;MinEllapsed;MaxEllapsed;MeanEllapsed;MedianEllapsed;sigmaEllapsed;MinMem;MaxMem;MeanMem;MedianMem;sigmaMem;MinAlloc;MaxAlloc;MeanAlloc;MedianAlloc;sigmaAlloc");
			}

			if (screenOutput) {
				initBackend ();
				initSurface ();
			} else {
				SolidBackground = false;
				initBackend (true);

				CreateMainSurface (ref clientRectangle);
			}

			initDictionaries ();
			loadStyling ();
		}

		protected override void Dispose (bool disposing) {
            base.Dispose (disposing);

			if (logToDisk) {
				writer.Dispose ();
				outStream.Dispose ();
            }
        }

		struct Measures
        {
			public double Elapsed;
			public double AllocatedKB;
			public double UsedKB;
		}
		struct Result
        {
			public double MinElapsed;
			public double MaxElapsed;
			public double MeanElapsed;
			public double MedianElapsed;
			public double sigmaElapsed;

			public double MinMem;
			public double MaxMem;
			public double MeanMem;
			public double MedianMem;
			public double sigmaMem;

			public double MinAlloc;
			public double MaxAlloc;
			public double MeanAlloc;
			public double MedianAlloc;
			public double sigmaAlloc;

			public override string ToString () =>
				$"{MinElapsed};{MaxElapsed};{MeanElapsed};{MedianElapsed};{sigmaElapsed};{MinMem};{MaxMem};{MeanMem};{MedianMem};{sigmaMem};{MinAlloc};{MaxAlloc};{MeanAlloc};{MedianAlloc};{sigmaAlloc}";
		}

		void getMemUsage (out long allocations, out long memory ) {
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			allocations = GC.GetTotalAllocatedBytes (true);
			memory = GC.GetTotalMemory (true);
			//memory = GC.GetGCMemoryInfo ().HeapSizeBytes;
		}

		Stopwatch chrono = new Stopwatch ();
		double freq = 0.001 * Stopwatch.Frequency;
		long totMemBefore = 0, allocBefore = 0, allocAfter = 0, totMemAfter = 0;
		double trimMinElapsed = double.MaxValue, extMinElapsed = double.MaxValue;
		double trimMaxElapsed = double.MinValue, extMaxElapsed = double.MinValue;
		double totElapsed = 0;
		double trimMinMem = double.MaxValue, extMinMem = double.MaxValue;
		double trimMaxMem = double.MinValue, extMaxMem = double.MinValue;
		double totMem = 0;
		double trimMinAlloc = double.MaxValue, extMinAlloc = double.MaxValue;
		double trimMaxAlloc = double.MinValue, extMaxAlloc = double.MinValue;
		double totAlloc = 0;

		Measures[] measures = null;
		Result result = default;

		void Test (string iml, bool isImlFragment = false)
		{

			for (int i = 0; i < count; i++) {


				if (StartStage == Stage.ITor) {
					getMemUsage (out allocBefore, out totMemBefore);
					chrono.Restart ();
				}

				Crow.IML.Instantiator iTor = isImlFragment ?
					Crow.IML.Instantiator.CreateFromImlFragment (this, iml) : new Crow.IML.Instantiator (this, iml);

				if (EndStage == Stage.ITor) {
					chrono.Stop ();
					getMemUsage (out allocAfter, out totMemAfter);
				} else {
					if (StartStage == Stage.Instantiation) {
						getMemUsage (out allocBefore, out totMemBefore);
						chrono.Restart ();
					}

					Widget w = iTor.CreateInstance ();

					if (EndStage == Stage.Instantiation) {
						chrono.Stop ();
						getMemUsage (out allocAfter, out totMemAfter);
					} else {
						if (StartStage == Stage.Add) {
							getMemUsage (out allocBefore, out totMemBefore);
							chrono.Restart ();
						}

						AddWidget (w);

						if (EndStage == Stage.Add) {
							chrono.Stop ();
							getMemUsage (out allocAfter, out totMemAfter);
						} else {
							if (StartStage == Stage.Datasource) {
								getMemUsage (out allocBefore, out totMemBefore);
								chrono.Restart ();
							}

							w.DataSource = this;

							if (EndStage == Stage.Datasource) {
								chrono.Stop ();
								getMemUsage (out allocAfter, out totMemAfter);
							} else {
								if (StartStage == Stage.FirstUpdate) {
									getMemUsage (out allocBefore, out totMemBefore);
									chrono.Restart ();
								}

								Update ();
								while (LayoutingQueue.Count > 0)
									Update ();

								if (EndStage == Stage.FirstUpdate) {
									chrono.Stop ();
									getMemUsage (out allocAfter, out totMemAfter);
								} else {
									if (StartStage == Stage.Update) {
										getMemUsage (out allocBefore, out totMemBefore);
										chrono.Restart ();
									}

									for (int j = 0; j < updateCycles; j++) {
										NotifyValueChanged ("elapsed", chrono.ElapsedTicks);
										Update ();
										while (LayoutingQueue.Count > 0)
											Update ();
									}

									if (EndStage == Stage.Update) {
										chrono.Stop ();
										getMemUsage (out allocAfter, out totMemAfter);
									} else if (StartStage == Stage.Delete) {
										getMemUsage (out allocBefore, out totMemBefore);
										chrono.Restart ();
									}
								}
							}
						}

						DeleteWidget (w);
						w = null;

						if (EndStage == Stage.Delete) {
							chrono.Stop ();
							iTor = null;
							getMemUsage (out allocAfter, out totMemAfter);
						}
					}

					w?.Dispose ();
				}

				LayoutingQueue.Clear ();
				ClippingQueue.Clear ();
				/*this.Instantiators.Clear ();
				this.Templates.Clear ();
				this.DefaultTemplates.Clear ();*/
				this.DefaultValuesLoader.Clear ();

				measures[i].AllocatedKB = (double)(allocAfter - allocBefore) / 1024.0;
				measures[i].UsedKB = (double)(totMemAfter - totMemBefore) / 1024.0;
				measures[i].Elapsed = (double)chrono.ElapsedTicks / freq;
			}

			trimMinElapsed = double.MaxValue; extMinElapsed = double.MaxValue;
			trimMaxElapsed = double.MinValue; extMaxElapsed = double.MinValue;
			totElapsed = 0;
			trimMinMem = double.MaxValue; extMinMem = double.MaxValue;
			trimMaxMem = double.MinValue; extMaxMem = double.MinValue;
			totMem = 0;
			trimMinAlloc = double.MaxValue; extMinAlloc = double.MaxValue;
			trimMaxAlloc = double.MinValue; extMaxAlloc = double.MinValue;
			totAlloc = 0;

			for (int i = 0; i < count; i++) {
				if (measures[i].Elapsed < extMinElapsed)
					extMinElapsed = measures[i].Elapsed;
				if (measures[i].Elapsed > extMaxElapsed)
					extMaxElapsed = measures[i].Elapsed;
				totElapsed += measures[i].Elapsed;

				if (measures[i].UsedKB < extMinMem)
					extMinMem = measures[i].UsedKB;
				if (measures[i].UsedKB > extMaxMem)
					extMaxMem = measures[i].UsedKB;
				totMem += measures[i].UsedKB;

				if (measures[i].AllocatedKB< extMinAlloc)
					extMinAlloc = measures[i].AllocatedKB;
				if (measures[i].AllocatedKB > extMaxAlloc)
					extMaxAlloc = measures[i].AllocatedKB;
				totAlloc += measures[i].AllocatedKB;
			}

			for (int i = 0; i < count; i++) {
				if (measures[i].Elapsed < trimMinElapsed && measures[i].Elapsed != extMinElapsed)
					trimMinElapsed = measures[i].Elapsed;
				if (measures[i].Elapsed > trimMaxElapsed && measures[i].Elapsed != extMaxElapsed)
					trimMaxElapsed = measures[i].Elapsed;

				if (measures[i].UsedKB < trimMinMem && measures[i].UsedKB != extMinMem)
					trimMinMem = measures[i].UsedKB;
				if (measures[i].UsedKB > trimMaxMem && measures[i].UsedKB != extMaxMem)
					trimMaxMem = measures[i].UsedKB;

				if (measures[i].AllocatedKB < trimMinAlloc && measures[i].AllocatedKB != extMinAlloc)
					trimMinAlloc = measures[i].AllocatedKB;
				if (measures[i].AllocatedKB > trimMaxAlloc && measures[i].AllocatedKB != extMaxAlloc)
					trimMaxAlloc = measures[i].AllocatedKB;
			}

			result.MinElapsed = trimMinElapsed;
			result.MaxElapsed = trimMaxElapsed;
			result.MeanElapsed = totElapsed / count;
			result.MedianElapsed = (totElapsed - extMaxElapsed - extMinElapsed) / (count - 2);

			result.MinMem = trimMinMem;
			result.MaxMem = trimMaxMem;
			result.MeanMem = totMem / count;
			result.MedianMem = (totMem - extMaxMem - extMinMem) / (count - 2);

			result.MinAlloc = trimMinAlloc;
			result.MaxAlloc = trimMaxAlloc;
			result.MeanAlloc = totAlloc / count;
			result.MedianAlloc = (totAlloc - extMaxAlloc - extMinAlloc) / (count - 2);

			for (int i = 0; i < count; i++) {
				if (measures[i].Elapsed != extMinElapsed && measures[i].Elapsed != extMaxElapsed)
					result.sigmaElapsed += Math.Pow (measures[i].Elapsed - result.MedianElapsed, 2);

				if (measures[i].UsedKB != extMinMem && measures[i].UsedKB!= extMaxMem)
					result.sigmaMem += Math.Pow (measures[i].UsedKB - result.MedianMem, 2);

				if (measures[i].AllocatedKB != extMinAlloc && measures[i].AllocatedKB != extMaxAlloc)
					result.sigmaAlloc += Math.Pow (measures[i].AllocatedKB - result.MedianAlloc, 2);
			}

			result.sigmaElapsed = Math.Sqrt (result.sigmaElapsed / (count - 2));
			result.sigmaMem = Math.Sqrt (result.sigmaMem / (count - 2));
			result.sigmaAlloc = Math.Sqrt (result.sigmaAlloc / (count - 2));
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

					Test (f);

					if (logToDisk) {
						writer.WriteLine ($"{f};{result}");
						Console.Write (".");
                    } else {
						Console.WriteLine ($"{label,-50}|{result.MedianElapsed,10:0.000}|{result.MedianAlloc,10:0.000}|{result.MedianMem,8:0.000}|{result.sigmaElapsed,10:0.000}|{result.sigmaAlloc,8:0.000}|{result.sigmaMem,8:0.000}|");
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

		static string w = @"<Widget Width='50' Height='50'/>";
		static string[] unitTests =
		{
			@"<Widget/>",
			@"<Widget Width='Stretched'/>",
			@"<Widget Background='Blue'/>",
			@"<Label />",
			@"<Label Text='{elapsed}'/>",
			@"<Label Width='Stretched'/>",
			@"<Label Text='{elapsed}' Width='Stretched'/>",
			@"<Container/>",
			@"<Group/>",
			@"<CheckBox/>",
			@"<Spinner/>",
			//@"<Expandable/>",
		};
		void PerformUnitTests () {
			measures = ArrayPool<Measures>.Shared.Rent (count);

			foreach (string unitTest in unitTests) {
                //for (int i = 0; i < 2; i++) {
					Test (unitTest, true);

					if (logToDisk) {
						writer.WriteLine ($"{unitTest};{result}");
						Console.Write (".");
					} else {
						Console.WriteLine ($"{unitTest,-50}|{result.MedianElapsed,10:0.000}|{result.MedianAlloc,10:0.000}|{result.MedianMem,8:0.000}|{result.sigmaElapsed,10:0.000}|{result.sigmaAlloc,8:0.000}|{result.sigmaMem,8:0.000}|");
					}
				//}

			}

			ArrayPool<Measures>.Shared.Return (measures);
		}
		public void PerformTests () {
			measures = ArrayPool<Measures>.Shared.Rent (count);

			string dirName = Path.IsPathRooted (inDirectory) ? inDirectory :
				Path.Combine (Directory.GetCurrentDirectory (), inDirectory);
			if (!Directory.Exists (dirName))
				throw new FileNotFoundException ("Input directory not found: " + dirName);

			testDir (dirName);

			if (logToDisk)
				Console.WriteLine ($"\ntest log written to: {outFilePath}");

			ArrayPool<Measures>.Shared.Return (measures);
		}


		public static void Main (string [] args)
		{

			try {
				using (TestInterface iface = new TestInterface (args)) {
					if (string.IsNullOrEmpty(iface.inDirectory))
						iface.PerformUnitTests ();
					else
						iface.PerformTests ();
				}
			} catch (Exception e) {
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine (e);
				Console.ResetColor ();
            }
		}
	}
}

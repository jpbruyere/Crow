using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crow;
using Glfw;

namespace Samples
{
	public class BasicTests : SampleBase
	{
		static void Main ()
		{
			DbgLogger.IncludeEvents = DbgEvtType.Widget;
			DbgLogger.DiscardEvents = DbgEvtType.None;
			Crow.DbgLogger.ConsoleOutput = false;

			using (BasicTests app = new BasicTests ()) {
				app.SolidBackground = false;
				app.Run ();
			}
		}

		protected override void OnInitialized ()
		{
			Commands = new CommandGroup (
				new Crow.Command("command1", new Action(() => Console.WriteLine ("command1 triggered"))),
				new Crow.Command("command2", new Action(() => Console.WriteLine ("command2 triggered"))),
				new Crow.Command("command3", new Action(() => Console.WriteLine ("command3 triggered"))),
				new Crow.Command("command4", new Action(() => Console.WriteLine ("command4 triggered")))
			);

			// += KeyboardKeyDown1;

			//testFiles = new string [] { @"Interfaces/Experimental/testDock.crow" };
			testFiles = new string [] { @"Interfaces/Divers/welcome.crow" };
			//testFiles = new string [] { @"Interfaces/TemplatedGroup/3.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/testShape.crow" };
			//testFiles = new string [] { @"Interfaces/TemplatedControl/testEnumSelector.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/all.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/gauge.crow" };
			//testFiles = new string [] { @"Interfaces/Stack/StretchedInFit4.crow" };
			//testFiles = new string [] { @"Interfaces/TemplatedGroup/1.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/colorPicker2.crow" };
			//testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/GraphicObject", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Container", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Group", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Stack", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedControl", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedContainer", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedGroup", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Wrapper", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Divers", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/DragAndDrop", "*.crow")).ToArray ();
			//testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Experimental", "*.crow")).ToArray ();

			Load (testFiles [idx]).DataSource = this;
		}

		int idx = 0;
		string [] testFiles;


		public override bool OnKeyDown (Key key)
		{
			try {
				switch (key) {
				case Key.Escape:
					Quit ();
					break;				
				case Key.F3:
					idx--;
					break;
				case Key.F4:
					idx++;
					break;
				case Key.F1:
					//TestList.Add ("new string");
					NotifyValueChanged ("TestList", TestList);
					break;
				default:
					return base.OnKeyDown (key);
				}

				ClearInterface ();

				if (idx == testFiles.Length)
					idx = 0;
				else if (idx < 0)
					idx = testFiles.Length - 1;

#if NETCOREAPP3_1
				Console.WriteLine ($"Loading {testFiles [idx]}. {AppDomain.CurrentDomain.MonitoringSurvivedMemorySize}");
#else
				Console.WriteLine ($"Loading {testFiles [idx]}. {GC.GetTotalMemory(true)}");
#endif
				Load (testFiles [idx]).DataSource = this;
			} catch (Exception ex) {
				//(LoadIMLFragment ($"<Label Background='Red' Foreground='White' Height='Fit' Width='Stretched' Multiline='true' VerticalAlignment='Bottom' Margin='5' />") as OldLabel).Text = ex.ToString();
				Console.WriteLine (ex);
																														 //MessageBox.Show (CurrentInterface, MessageBox.Type.Error, ex.Message + "\n" + ex.InnerException.Message).Modal = true;
			}
			return false;
		}
		
		void OnClear (object sender, MouseButtonEventArgs e) => TestList = null;

		void OnLoadList (object sender, MouseButtonEventArgs e) => TestList =
			null; //Color.ColorDic.Values.OrderBy (c => c.Hue).ToList ();

	}
}

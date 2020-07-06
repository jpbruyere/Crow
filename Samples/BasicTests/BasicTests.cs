using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Crow;
using Glfw;

namespace tests
{
	public class BasicTests : SampleBase
	{
		static void Main ()
		{
#if NETCOREAPP3_1
			DllMapCore.Resolve.Enable (true);
#endif

			using (BasicTests app = new BasicTests ()) {
				app.Run ();
			}
		}

		protected override void OnInitialized ()
		{
			Commands = new List<Crow.Command> (new Crow.Command [] {
				new Crow.Command(new Action(() => command1())) { Caption = "command1"},
				new Crow.Command(new Action(() => command2())) { Caption = "command2"},
				new Crow.Command(new Action(() => command3())) { Caption = "command3"},
				new Crow.Command(new Action(() => command4())) { Caption = "command4"},
			});

			// += KeyboardKeyDown1;

			//testFiles = new string [] { @"Interfaces/Experimental/testDock.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/welcome.crow" };
			//testFiles = new string [] { @"Interfaces/TemplatedGroup/3.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/testShape.crow" };
			//testFiles = new string [] { @"Interfaces/TemplatedControl/testEnumSelector.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/all.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/gauge.crow" };
			//testFiles = new string [] { @"Interfaces/Stack/StretchedInFit4.crow" };
			testFiles = new string [] { @"Interfaces/TemplatedGroup/1.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/colorPicker2.crow" };
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/GraphicObject", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Container", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Group", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Stack", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedControl", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedContainer", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedGroup", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Splitter", "*.crow")).ToArray ();
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
				case Key.F2:
					//if (IsKeyDown (Key.LeftShift))
						//DbgLogger.Reset ();
					//DbgLogger.save (this);
					return false;
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
				case Key.F5:
					Load ("Interfaces/Divers/testFileDialog.crow").DataSource = this;
					return false;
				case Key.F6:
					Load ("Interfaces/Divers/0.crow").DataSource = this;
					return false;
				case Key.F7:
					Load ("Interfaces/Divers/perfMeasures.crow").DataSource = this;
					return false;
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
				(LoadIMLFragment ($"<Label Background='Red' Foreground='White' Height='Fit' Width='Stretched' Multiline='true' VerticalAlignment='Bottom' Margin='5' />") as Label).Text = ex.ToString();
				Console.WriteLine (ex.Message + "\n" + ex.InnerException);
																														 //MessageBox.Show (CurrentInterface, MessageBox.Type.Error, ex.Message + "\n" + ex.InnerException.Message).Modal = true;
			}
			return false;
		}
		
		void OnClear (object sender, MouseButtonEventArgs e) => TestList = null;

		void OnLoadList (object sender, MouseButtonEventArgs e) => TestList =
			null; //Color.ColorDic.Values.OrderBy (c => c.Hue).ToList ();

		void command1 ()
		{
			Console.WriteLine ("command1 triggered");
		}
		void command2 ()
		{
			Console.WriteLine ("command2 triggered");
		}
		void command3 ()
		{
			Console.WriteLine ("command3 triggered");
		}
		void command4 ()
		{
			Console.WriteLine ("command4 triggered");
		}
	}
}

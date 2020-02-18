using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Crow;

namespace tests
{
	public class BasicTests : Interface
	{
		[STAThread]
		static void Main ()
		{
			using (BasicTests app = new BasicTests ()) {
				app.Run ();
			}
		}

		protected override void Startup ()
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
			//testFiles = new string [] { @"Interfaces/Divers/perfMeasures.crow" };
			testFiles = new string [] { @"Interfaces/Divers/colorPicker2.crow" };
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

		public Version CrowVersion {
			get {
				return System.Reflection.Assembly.GetAssembly (typeof (Widget)).GetName ().Version;
			}
		}

		public override bool OnKeyDown (Key key)
		{
			try {
				switch (key) {
				case Key.Escape:
					Quit ();
					break;
				case Key.F2:
					idx--;
					break;
				case Key.F3:
					idx++;
					break;
				case Key.F1:
					TestList.Add ("new string");
					NotifyValueChanged ("TestList", TestList);
					break;
				case Key.F4:
					Load ("Interfaces/TemplatedContainer/testWindow.goml").DataSource = this;
					return false;
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

				Console.WriteLine ($"Loading {testFiles [idx]}.");

				Load (testFiles [idx]).DataSource = this;
			} catch (Exception ex) {
				(LoadIMLFragment ($"<Label Background='Red' Foreground='White' Height='Fit' Width='Stretched' Multiline='true' VerticalAlignment='Bottom' Margin='5' />") as Label).Text = ex.ToString();
				Console.WriteLine (ex.Message + "\n" + ex.InnerException);
																														 //MessageBox.Show (CurrentInterface, MessageBox.Type.Error, ex.Message + "\n" + ex.InnerException.Message).Modal = true;
			}
			return false;
		}
		#region Test values for Binding
		public List<Crow.Command> Commands;
		public int intValue = 500;
		DirectoryInfo curDir = new DirectoryInfo (Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location));
		//DirectoryInfo curDir = new DirectoryInfo (@"/mnt/data/Images");
		public FileSystemInfo [] CurDirectory {
			get { return curDir.GetFileSystemInfos (); }
		}
		public int IntValue {
			get {
				return intValue;
			}
			set {
				intValue = value;
				NotifyValueChanged ("IntValue", intValue);
			}
		}
		void onSpinnerValueChange (object sender, ValueChangeEventArgs e)
		{
			if (e.MemberName != "Value")
				return;
			intValue = Convert.ToInt32 (e.NewValue);
		}
		void change_alignment (object sender, EventArgs e)
		{
			RadioButton rb = sender as RadioButton;
			if (rb == null)
				return;
			NotifyValueChanged ("alignment", Enum.Parse (typeof (Alignment), rb.Caption));
		}
		public IEnumerable<String> List2 = new List<string> (new string []
			{
				"string1",
				"string2",
				"string3",
				"string4",
				"string5",
				"string6",
				"string7",
				"string8",
				"string8",
				"string8",
				"string8",
				"string8",
				"string8",
				"string9"
			}
		);
		public IEnumerable<String> TestList2 {
			set {
				List2 = value;
				NotifyValueChanged ("TestList2", testList);
			}
			get { return List2; }
		}
		public class TestClass
		{
			public string Prop1 { get; set; }
			public string Prop2 { get; set; }

			public override string ToString ()
				=> $"{Prop1}, {Prop2}";

		}
		public IEnumerable<TestClass> List3 = new List<TestClass> (new TestClass []
			{
				new TestClass { Prop1 = "string1", Prop2="prop2-1" },
				new TestClass { Prop1 = "string2", Prop2="prop2-2" },
				new TestClass { Prop1 = "string3", Prop2="prop2-3" },
			}
		);
		public IEnumerable<string> TestList3Props1 => List3.Select (sc => sc.Prop1).ToList();
		public IEnumerable<TestClass> TestList3 {
			set {
				List3 = value;
				NotifyValueChanged ("TestList3", testList);
			}
			get { return List3; }
		}
		string prop1;
		public string TestList3SelProp1 {
			get => prop1;
			set {
				if (prop1 == value)
					return;
				prop1 = value;

				NotifyValueChanged ("TestList3SelProp1", prop1);
			}
		}

		string selString;
		public string TestList2SelectedString {
			get => selString;
			set {
				if (selString == value)
					return;
				selString = value;
				NotifyValueChanged ("TestList2SelectedString", selString);
			}
		}

		List<Color> testList = Color.ColorDic.Values//.OrderBy(c=>c.Hue)
													//.ThenBy(c=>c.Value).ThenBy(c=>c.Saturation)
			.ToList ();
		public List<Color> TestList {
			set {
				testList = value;
				NotifyValueChanged ("TestList", testList);
			}
			get { return testList; }
		}
		string curSources = "";
		public string CurSources {
			get { return curSources; }
			set {
				if (value == curSources)
					return;
				curSources = value;
				NotifyValueChanged ("CurSources", curSources);
			}
		}
		bool boolVal = true;
		public bool BoolVal {
			get { return boolVal; }
			set {
				if (boolVal == value)
					return;
				boolVal = value;
				NotifyValueChanged ("BoolVal", boolVal);
			}
		}

		#endregion

		void OnClear (object sender, MouseButtonEventArgs e) => TestList = null;

		void OnLoadList (object sender, MouseButtonEventArgs e) => TestList =
			Color.ColorDic.Values.OrderBy (c => c.Hue).ToList ();

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

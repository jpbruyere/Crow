using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Crow;
using Glfw;

namespace HelloWorld
{
	class Program : CrowVkWin
	{
		static void Main (string [] args)
		{
			using (Program vke = new Program ()) {
				vke.Run ();
			}
		}

		int idx = 0;
		string [] testFiles;

		public Version CrowVersion {
			get {
				return Assembly.GetAssembly (typeof (Widget)).GetName ().Version;
			}
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
		public IList<String> List2 = new List<string> (new string []
			{
				"string1",
				"string2",
				"string3",
//				"string4",
//				"string5",
//				"string6",
//				"string7",
//				"string8",
//				"string8",
//				"string8",
//				"string8",
//				"string8",
//				"string8",
//				"string9"
			}
		);
		public IList<String> TestList2 {
			set {
				List2 = value;
				NotifyValueChanged ("TestList2", testList);
			}
			get { return List2; }
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

		protected override void onLoad ()
		{
			Commands = new List<Crow.Command> (new Crow.Command [] {
				new Crow.Command(new Action(() => command1())) { Caption = "command1"},
				new Crow.Command(new Action(() => command2())) { Caption = "command2"},
				new Crow.Command(new Action(() => command3())) { Caption = "command3"},
				new Crow.Command(new Action(() => command4())) { Caption = "command4"},
			});

			//testFiles = new string [] { @"Interfaces/Experimental/testDock.crow" };
			testFiles = new string [] { @"Interfaces/Divers/welcome.crow" };
			//testFiles = new string [] { @"Interfaces/Divers/colorPicker.crow" };
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

			Widget w = crow.Load (testFiles [idx]);
			w.DataSource = this;
		}


		protected override void onKeyDown (Glfw.Key key, int scanCode, Modifier modifiers)
		{
			base.onKeyDown (key, scanCode, modifiers);

			switch (key) {
			case Glfw.Key.F2:
				idx--;
				break;
			case Glfw.Key.F3:
				idx++;
				break;
			}

			crow.ClearInterface ();

			if (idx == testFiles.Length)
				idx = 0;
			else if (idx < 0)
				idx = testFiles.Length - 1;

			try {
				Widget w = crow.Load (testFiles [idx]);
				w.DataSource = this;
			} catch (Exception ex) {
				MessageBox.Show (crow, MessageBox.Type.Error, ex.Message + "\n" + ex.InnerException.Message).Modal = true;
			}
		}
	}
}
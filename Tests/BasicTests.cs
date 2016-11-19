using System;
using Crow;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Diagnostics;


namespace Tests
{
	class BasicTests : OpenTKGameWindow
	{
		public BasicTests ()
			: base(800, 600,"test: press <F3> to toogle test files")
		{
		}

		int idx = 0;
		string[] testFiles;

		#region Test values for Binding
		public int intValue = 25;
		DirectoryInfo curDir = new DirectoryInfo (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
		public FileSystemInfo[] CurDirectory {
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
		void onSpinnerValueChange(object sender, ValueChangeEventArgs e){
			if (e.MemberName != "Value")
				return;
			intValue = Convert.ToInt32(e.NewValue);
		}
		void change_alignment(object sender, EventArgs e){
			RadioButton rb = sender as RadioButton;
			if (rb == null)
				return;
			NotifyValueChanged ("alignment", Enum.Parse(typeof(Alignment), rb.Caption));
		}
		public IList<String> List2 = new List<string>(new string[]
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
		IList<Color> testList = Color.ColorDic.ToList();
		public IList<Color> TestList {
			set{
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
		#endregion

		void OnClear (object sender, MouseButtonEventArgs e) => TestList = null;

		void OnLoadList (object sender, MouseButtonEventArgs e) => TestList = Color.ColorDic.ToList();

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			this.KeyDown += KeyboardKeyDown1;

			testFiles = new string [] { @"Interfaces/Divers/welcome.crow" };
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/GraphicObject", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Container", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Group", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Stack", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Wrapper", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Divers", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/Splitter", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedControl", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedContainer", "*.crow")).ToArray ();
			testFiles = testFiles.Concat (Directory.GetFiles (@"Interfaces/TemplatedGroup", "*.crow")).ToArray ();

			this.Title = testFiles [idx] + ". Press <F3> to switch example.";
			CrowInterface.LoadInterface(testFiles[idx]).DataSource = this;
		}
		void KeyboardKeyDown1 (object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			if (e.Key == OpenTK.Input.Key.Escape) {
				Quit (null, null);
				return;
			} else if (e.Key == OpenTK.Input.Key.F1) {
				TestList.Add ("new string");
				NotifyValueChanged ("TestList", TestList);
				return;
			} else if (e.Key == OpenTK.Input.Key.F4) {
				GraphicObject w = CrowInterface.LoadInterface ("Interfaces/TemplatedContainer/testWindow.goml");
				w.DataSource = this;
				return;
			} else if (e.Key == OpenTK.Input.Key.F5) {
				GraphicObject w = CrowInterface.LoadInterface ("Interfaces/TemplatedContainer/testWindow2.goml");
				w.DataSource = this;
				return;
			}else if (e.Key == OpenTK.Input.Key.F6) {
				GraphicObject w = CrowInterface.LoadInterface ("Interfaces/Divers/0.crow");
				w.DataSource = this;
				return;
			} else if (e.Key == OpenTK.Input.Key.F2)
				idx--;
			else if (e.Key == OpenTK.Input.Key.F3)
				idx++;
			else
				return;
		
			CrowInterface.ClearInterface ();

			if (idx == testFiles.Length)
				idx = 0;
			else if (idx < 0)
				idx = testFiles.Length - 1;
			
			this.Title = testFiles [idx] + ". Press <F3> to cycle examples.";
			try {
				GraphicObject obj = CrowInterface.LoadInterface(testFiles[idx]);
				obj.DataSource = this;
			} catch (Exception ex) {
				Debug.WriteLine (ex.Message + ex.InnerException);
			}
		}
		void Tv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			FileInfo fi = e.NewValue as FileInfo;
			if (fi == null)
				return;
			if (fi.Extension == ".crow" || fi.Extension == ".goml") {
				Instantiator i = new Instantiator(fi.FullName);
				lock (CrowInterface.UpdateMutex) {
					(CrowInterface.FindByName ("crowContainer") as Container).SetChild
					(i.CreateInstance(CrowInterface));
					//CurSources = i.GetImlSourcesCode();
				}
			}
		}
		void onImlSourceChanged(Object sender, TextChangeEventArgs e){
			Instantiator i;
			try {
				i = Instantiator.CreateFromImlFragment (e.Text);
			} catch (Exception ex) {
				Debug.WriteLine (ex);
				return;
			}
			lock (CrowInterface.UpdateMutex) {
				(CrowInterface.FindByName ("crowContainer") as Container).SetChild
				(i.CreateInstance(CrowInterface));
			}
		}
		void onButClick(object send, MouseButtonEventArgs e)
		{
			Console.WriteLine ("button clicked:" + send.ToString());
		}
		void onAddTabButClick(object sender, MouseButtonEventArgs e){

			TabView tv = CrowInterface.FindByName("tabview1") as TabView;
			if (tv == null)
				return;
			tv.AddChild (new TabItem () { Caption = "NewTab" });
		}
		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");
			BasicTests win = new BasicTests ();
			win.Run (30);
		}
		protected override void OnUpdateFrame (OpenTK.FrameEventArgs e)
		{
			base.OnUpdateFrame (e);
			string test = e.Time.ToString ();
			NotifyValueChanged ("PropertyLessBinding", test);
		}
		void onNew(object sender, EventArgs e){
			Debug.WriteLine ("menu new clicked");
		}
	}
}

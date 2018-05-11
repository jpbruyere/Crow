//
// BasicTests.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
	class BasicTests : CrowWindow
	{
		public BasicTests ()
			: base(800, 600,"test: press <F3> to toogle test files")
		{
		}

		int idx = 0;
		string[] testFiles;

		public Version CrowVersion {
			get {
				return System.Reflection.Assembly.GetAssembly(typeof(GraphicObject)).GetName().Version;
			}
		}

		#region Test values for Binding
		public List<Crow.Command> Commands;
		public int intValue = 500;
		DirectoryInfo curDir = new DirectoryInfo (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
		//DirectoryInfo curDir = new DirectoryInfo (@"/mnt/data/Images");
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
			set{
				List2 = value;
				NotifyValueChanged ("TestList2", testList);
			}
			get { return List2; }
		}
		List<Color> testList = Color.ColorDic.Values//.OrderBy(c=>c.Hue)
			//.ThenBy(c=>c.Value).ThenBy(c=>c.Saturation)
			.ToList();
		public List<Color> TestList {
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
			Color.ColorDic.Values.OrderBy(c=>c.Hue).ToList();

		void command1(){
			Console.WriteLine("command1 triggered");
		}
		void command2(){
			Console.WriteLine("command2 triggered");
		}
		void command3(){
			Console.WriteLine("command3 triggered");
		}
		void command4(){
			Console.WriteLine("command4 triggered");
		}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
//
//			foreach (Color c in Color.ColorDic.Values) {
//				if (string.IsNullOrEmpty(c.htmlCode))
//					Console.WriteLine ("no htmlcode for {0}", c.Name);
//				else if (c.htmlCode.Substring(1) != c.HtmlCode)
//					Console.WriteLine ("{2} orig: {0} comp: {1}",c.htmlCode, c.HtmlCode, c.Name);
//			}


			Commands = new List<Crow.Command> (new Crow.Command[] {
				new Crow.Command(new Action(() => command1())) { Caption = "command1"},
				new Crow.Command(new Action(() => command2())) { Caption = "command2"},
				new Crow.Command(new Action(() => command3())) { Caption = "command3"},
				new Crow.Command(new Action(() => command4())) { Caption = "command4"},
			});

			this.KeyDown += KeyboardKeyDown1;

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

			Load(testFiles[idx]).DataSource = this;

//			LoadIMLFragment (@"<DockWindow Width=""150"" Height=""150"" Background=""DarkRed"" />", 0);
//			LoadIMLFragment (@"<DockWindow Width=""200"" Height=""150"" Background=""DarkGreen"" />", 0);
//			LoadIMLFragment (@"<DockWindow Width=""250"" Height=""150"" Background=""Brown"" />", 0);
//			LoadIMLFragment (@"<DockWindow Width=""300"" Height=""150"" Background=""DarkBlue"" />", 0);


		}
		void KeyboardKeyDown1 (object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			try {
				
			if (e.Key == OpenTK.Input.Key.Escape) {
				Quit (null, null);
				return;
			} else if (e.Key == OpenTK.Input.Key.F1) {
				TestList.Add ("new string");
				NotifyValueChanged ("TestList", TestList);
				return;
			} else if (e.Key == OpenTK.Input.Key.F4) {
				GraphicObject w = Load ("Interfaces/TemplatedContainer/testWindow.goml");
				w.DataSource = this;
				return;
			} else if (e.Key == OpenTK.Input.Key.F5) {
				GraphicObject w = Load ("Interfaces/Divers/testFileDialog.crow");
				w.DataSource = this;
				return;
			}else if (e.Key == OpenTK.Input.Key.F6) {
				GraphicObject w = Load ("Interfaces/Divers/0.crow");
				w.DataSource = this;
				return;
			}else if (e.Key == OpenTK.Input.Key.F7) {
				GraphicObject w = Load ("Interfaces/Divers/perfMeasures.crow");
				w.DataSource = this.ifaceControl[0];
				return;
			} else if (e.Key == OpenTK.Input.Key.F2)
				idx--;
			else if (e.Key == OpenTK.Input.Key.F3)
				idx++;
			else
				return;

				ClearInterface ();

				if (idx == testFiles.Length)
					idx = 0;
				else if (idx < 0)
					idx = testFiles.Length - 1;

				this.Title = testFiles [idx] + ". Press <F3> to cycle examples.";

				GraphicObject obj = Load (testFiles[idx]);
				obj.DataSource = this;
			} catch (Exception ex) {				
				MessageBox.Show (CurrentInterface, MessageBox.Type.Error, ex.Message + "\n" + ex.InnerException.Message).Modal = true;
			}
		}
//		void Tv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
//		{
//			FileInfo fi = e.NewValue as FileInfo;
//			if (fi == null)
//				return;
//			if (fi.Extension == ".crow" || fi.Extension == ".goml") {
//				Instantiator i = new Instantiator(fi.FullName);
//				lock (ifaceControl.CrowInterface.UpdateMutex) {
//					(ifaceControl.CrowInterface.FindByName ("crowContainer") as Container).SetChild
//					(i.CreateInstance(ifaceControl.CrowInterface));
//					//CurSources = i.GetImlSourcesCode();
//				}
//			}
//		}
//		void onImlSourceChanged(Object sender, TextChangeEventArgs e){
//			Instantiator i;
//			try {
//				i = Instantiator.CreateFromImlFragment (e.Text);
//			} catch (Exception ex) {
//				Debug.WriteLine (ex);
//				return;
//			}
//			lock (ifaceControl.CrowInterface.UpdateMutex) {
//				(ifaceControl.CrowInterface.FindByName ("crowContainer") as Container).SetChild
//				(i.CreateInstance(ifaceControl.CrowInterface));
//			}
//		}
		void onButClick(object send, MouseButtonEventArgs e)
		{
			Console.WriteLine ("button clicked:" + send.ToString());
		}
		void onAddTabButClick(object sender, MouseButtonEventArgs e){

			TabView tv = FindByName("tabview1") as TabView;
			if (tv == null)
				return;
			//tv.AddChild (new TabItem (CurrentInterface) { Caption = "NewTab" });
			lock (CurrentInterface.UpdateMutex) {
				tv.AddChild (CurrentInterface.LoadIMLFragment
					(@"<TabItem Caption='New tab' Background='Blue'><Label/></TabItem>"));
			}
		}
		[STAThread]
		static void Main ()
		{
			#if DEBUG
			TextWriterTraceListener listener = new TextWriterTraceListener ("debug.log");
			Debug.Listeners.Add (listener);
			#endif

			Console.WriteLine ("starting example");
			BasicTests win = new BasicTests ();
			win.VSync = OpenTK.VSyncMode.Adaptive;
			win.Run (30);
			#if DEBUG
			listener.Dispose ();
			#endif
		}
		protected override void OnUpdateFrame (OpenTK.FrameEventArgs e)
		{
			base.OnUpdateFrame (e);
			string test = e.Time.ToString ();
			IntValue++;
			if (IntValue == 1000)
				IntValue = 0;
			NotifyValueChanged ("PropertyLessBinding", test);
		}
		void onNew(object sender, EventArgs e){
			Debug.WriteLine ("menu new clicked");
		}
	}
}

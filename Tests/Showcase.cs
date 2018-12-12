//
// UIEditor.cs
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
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using Crow.IML;

namespace tests
{
	class Showcase : Interface
	{
		public Container crowContainer;

		[STAThread]
		static void Main ()
		{
			using (Showcase app = new Showcase ()) {
				//app.Keyboard.KeyDown += App_KeyboardKeyDown;

				GraphicObject g = app.AddWidget ("#Tests.ui.showcase.crow");
				g.DataSource = app;
				app.crowContainer = g.FindByName ("CrowContainer") as Container;
				//I set an empty object as datasource at this level to force update when new
				//widgets are added to the interface
				app.crowContainer.DataSource = new object ();
				app.hideError ();                
                app.Run();

			}
		}

		static void App_KeyboardKeyDown (object sender, KeyEventArgs e)
		{
			#if DEBUG_LOG
			switch (e.Key) {
			case Key.F2:
				DebugLog.save (sender as Interface);
				break;
			}
			#endif
		}

		public Showcase ()
			: base(1024, 800)
		{
		}


		void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			FileSystemInfo fi = e.NewValue as FileSystemInfo;
			if (fi == null)
				return;
			if (fi is DirectoryInfo)
				return;
			hideError();
			lock (UpdateMutex) {
                try
                {
                    GraphicObject g = Load(fi.FullName);
                    crowContainer.SetChild(g);
                    g.DataSource = this;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    if (ex is InstantiatorException)
                        showError((InstantiatorException)ex);
                }
            }

            string source = "";
			using (Stream s = new FileStream (fi.FullName, FileMode.Open)) {
				using (StreamReader sr = new StreamReader (s)) {
					source = sr.ReadToEnd ();
				}
			}
			NotifyValueChanged ("source", source);
		}

		void showError(InstantiatorException ex) {
			NotifyValueChanged ("ErrorMessage", ex.Path + ": " + ex.InnerException.Message);
			NotifyValueChanged ("ShowError", true);
		}
		void hideError () {
			NotifyValueChanged ("ShowError", false);
		}

		void Tb_TextChanged (object sender, TextChangeEventArgs e)
		{
			hideError();
			GraphicObject g = null;
			try {
				lock (UpdateMutex) {
					Instantiator inst = null;
					using (MemoryStream ms = new MemoryStream (Encoding.UTF8.GetBytes (e.Text))){
						inst = new Instantiator (this, ms);
					}
					g = inst.CreateInstance ();
					crowContainer.SetChild (g);
					g.DataSource = this;
				}
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
                if (ex is InstantiatorException)
				    showError ((InstantiatorException)ex);
			}
        }


		#region Test values for Binding
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
		IList<Color> testList = Color.ColorDic.Values.ToList();
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

		void OnLoadList (object sender, MouseButtonEventArgs e) => TestList = Color.ColorDic.Values.ToList();

	}


}
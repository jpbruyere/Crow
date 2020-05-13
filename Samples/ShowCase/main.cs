// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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

				app.Run ();

			}
		}

		protected override void OnInitialized ()
		{
			Widget g = Load ("#ShowCase.showcase.crow");
			g.DataSource = this;
			crowContainer = g.FindByName ("CrowContainer") as Container;
			//I set an empty object as datasource at this level to force update when new
			//widgets are added to the interface
			crowContainer.DataSource = new object ();
			hideError ();
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
			: base (1024, 800)
		{
		}


		void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			FileSystemInfo fi = e.NewValue as FileSystemInfo;
			if (fi == null)
				return;
			if (fi is DirectoryInfo)
				return;
			hideError ();
			lock (UpdateMutex) {
				try {
					Widget g = CreateInstance (fi.FullName);
					crowContainer.SetChild (g);
					g.DataSource = this;
				} catch (Exception ex) {
					Console.WriteLine (ex.ToString ());
					showError (ex);
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

		void showError (Exception ex)
		{
			NotifyValueChanged ("ErrorMessage", ex.Message);
			NotifyValueChanged ("ShowError", true);
		}
		void hideError ()
		{
			NotifyValueChanged ("ShowError", false);
		}

		void Tb_TextChanged (object sender, TextChangeEventArgs e)
		{
			hideError ();
			Widget g = null;
			try {
				lock (UpdateMutex) {
					Instantiator inst = null;
					using (MemoryStream ms = new MemoryStream (Encoding.UTF8.GetBytes (e.Text))) {
						inst = new Instantiator (this, ms);
					}
					g = inst.CreateInstance ();
					crowContainer.SetChild (g);
					g.DataSource = this;
				}
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
				showError ((Exception)ex);
			}
		}


		#region Test values for Binding
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
		IList<Color> testList = null;// Enum.GetValues (typeof (Colors)).Cast<Color> ().ToList (); // Color.ColorDic.Values.OrderBy(c=>c.Hue).ThenBy(c=>c.Value).ToList ();
		public IList<Color> TestList {
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

		void OnLoadList (object sender, MouseButtonEventArgs e) => TestList = Enum.GetValues (typeof (Colors)).Cast<Color> ().ToList ();

	}

	public static class Extensions
	{
		public static Fill GetIcon (this Alignment align)
		{
			return (Crow.Fill)Fill.Parse ("#images.valign.svg");
		}
	}
}
// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using System.Text;
using Crow.IML;

namespace ShowCase
{
	class Showcase : SampleBase
	{
		static void Main ()
		{
#if NETCOREAPP3_1
			DllMapCore.Resolve.Enable (true);
#endif
			using (Showcase app = new Showcase ()) 
				app.Run ();
		}

		public Container crowContainer;

		public string CurrentDir {
			get { return Configuration.Global.Get<string> ("CurrentDir"); }
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set ("CurrentDir", value);
				NotifyValueChanged ("CurrentDir",CurrentDir);
			}
		}
		public void goUpDirClick (object sender, MouseButtonEventArgs e)
		{
			string root = Directory.GetDirectoryRoot (CurrentDir);
			if (CurrentDir == root)
				return;
			CurrentDir = Directory.GetParent (CurrentDir).FullName;
		}

		protected override void OnInitialized ()
		{
			if (string.IsNullOrEmpty (CurrentDir))
				CurrentDir = Path.Combine (Directory.GetCurrentDirectory (), "Interfaces");
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

		void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			FileSystemInfo fi = e.NewValue as FileSystemInfo;
			if (fi == null)
				return;
			if (fi is DirectoryInfo)
				return;

			string source = "";
			using (Stream s = new FileStream (fi.FullName, FileMode.Open)) {
				using (StreamReader sr = new StreamReader (s))
					source = sr.ReadToEnd ();
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
			} catch (InstantiatorException itorex) {
				Console.WriteLine (itorex.ToString ());
				showError (itorex.InnerException);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				showError (ex);
			}
		}


	}
}
// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using System.Text;
using Crow.IML;
using System.Runtime.CompilerServices;

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
			get { return Configuration.Global.Get<string> (nameof (CurrentDir)); }
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set (nameof (CurrentDir), value);
				NotifyValueChanged (CurrentDir);
			}
		}

		string source = @"<Label Text='Hello World' Background='MediumSeaGreen' Margin='10'/>";

		public string Source {
			get => Source;
			set {
				if (source == value)
					return;
				source = value;
				reloadFromSource ();
				NotifyValueChanged (source);
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
			base.OnInitialized ();

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
				
			using (Stream s = new FileStream (fi.FullName, FileMode.Open)) {
				using (StreamReader sr = new StreamReader (s))
					Source = sr.ReadToEnd ();
			}
		}

		void showError (Exception ex)
		{
			NotifyValueChanged ("ErrorMessage", (object)ex.Message);
			NotifyValueChanged ("ShowError", true);
		}
		void hideError ()
		{
			NotifyValueChanged ("ShowError", false);
		}

		void reloadFromSource ()
		{
			hideError ();
			Widget g = null;
			try {
				lock (UpdateMutex) {
					Instantiator inst = null;
					using (MemoryStream ms = new MemoryStream (Encoding.UTF8.GetBytes (source))) {
						inst = new Instantiator (this, ms);
					}
					g = inst.CreateInstance ();
					crowContainer.SetChild (g);
					g.DataSource = this;
				}
			} catch (InstantiatorException itorex) {
				Console.WriteLine (itorex);
				showError (itorex.InnerException);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				showError (ex);
			}
		}


	}
}
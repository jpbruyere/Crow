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


	}
}
// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using System.Text;
using Crow.IML;
using System.Runtime.CompilerServices;
using Glfw;
using System.Diagnostics;
using Crow.Text;
using System.Collections.Generic;
using Encoding = System.Text.Encoding;
using Samples;

namespace ShowCase
{
	class Showcase : SampleBaseForEditor
	{
		DbgEvtType[] logEvts = {
			DbgEvtType.MouseEnter,
			DbgEvtType.MouseLeave,
			DbgEvtType.WidgetMouseDown,
			DbgEvtType.WidgetMouseUp,
			DbgEvtType.WidgetMouseClick,
		};
		static void Main ()
		{
			initDebugLog ();

			Environment.SetEnvironmentVariable ("FONTCONFIG_PATH", @"C:\Users\Jean-Philippe\source\vcpkg\installed\x64-windows\tools\fontconfig\fonts");

			using (Showcase app = new Showcase ()) {
				app.WindowTitle = "C.R.O.W Showcase";
				app.SetWindowIcon ("#Crow.Icons.crow.png");
				//app.Theme = @"C:\Users\Jean-Philippe\source\Crow\Themes\TestTheme";
				CurrentProgramInstance = app;

				app.Run ();
			}
		}
		public Container crowContainer;

		Stopwatch reloadChrono = new Stopwatch ();

		public override string Source {
			get => source;
			set {
				if (source == value)
					return;
				source = value;
				CMDSave.CanExecute = IsDirty;
				if (!reloadChrono.IsRunning)
					reloadChrono.Restart ();
				NotifyValueChanged (source);
				NotifyValueChanged ("IsDirty", IsDirty);
			}
		}

		void reloadFromSource () {
			hideError ();
			Widget g = null;
			try {
				lock (UpdateMutex) {
					Instantiator inst = null;
					using (MemoryStream ms = new MemoryStream (Encoding.UTF8.GetBytes (source)))
						inst = new Instantiator (this, ms);
					g = inst.CreateInstance ();
					crowContainer.SetChild (g);
					g.DataSource = this;
				}
			} catch (InstantiatorException itorex) {
				showError (itorex);
			} catch (Exception ex) {
				showError (ex);
			}
		}

		void showError (Exception ex) {
			NotifyValueChanged ("ErrorMessage", ex);
			NotifyValueChanged ("ShowError", true);
		}
		void hideError () {
			NotifyValueChanged ("ShowError", false);
		}




		protected override void OnInitialized () {
			base.OnInitialized ();

			Load ("#ShowCase.showcase.crow").DataSource = this;
			crowContainer = FindByName ("CrowContainer") as Container;
			editor = FindByName ("tb") as TextBox;

			if (!File.Exists (CurrentFile))
				newFile ();
			//I set an empty object as datasource at this level to force update when new
			//widgets are added to the interface
			crowContainer.DataSource = new object ();
			hideError ();

			reloadFromFile ();
		}

		public override void UpdateFrame () {
            base.UpdateFrame ();
			if (reloadChrono.ElapsedMilliseconds < 200)
				return;
			reloadFromSource ();
			reloadChrono.Reset ();
		}


        public override bool OnKeyDown (KeyEventArgs e) {

            switch (e.Key) {
            case Key.F5:
                Load ("#ShowCase.DebugLog.crow").DataSource = this;
                return true;
            case Key.F6:
				if (DebugLogRecording) {
					DbgLogger.IncludedEvents.Clear();
					if (DebugLogToFile && !string.IsNullOrEmpty(DebugLogFilePath))
	                	DbgLogger.Save (this, DebugLogFilePath);
					DebugLogRecording = false;
 				} else {
					DbgLogger.Reset ();
					DbgLogger.IncludedEvents = new List<DbgEvtType> (logEvts);
					DebugLogRecording = true;
				}
                return true;
            }
            return base.OnKeyDown (e);
        }
    }
}
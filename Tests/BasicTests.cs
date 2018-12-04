using System;
using System.Runtime.InteropServices;
using Crow;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace tests
{
	class MainClass : Interface
	{
		static MainClass app;
		public Command CMDTest;
		public Measure TestWidth = 100;

		IList<Color> testList = Color.ColorDic.Values.ToList();
		public IList<Color> TestList {
			set{
				testList = value;
				NotifyValueChanged ("TestList", testList);
			}
			get { return testList; }
		}
		public List<KeyValuePair<DbgEvtType, Color>> ColorsKVPList {
			get {
				return DbgLogViewer.colors.ToList();
			}
		}

		protected override void InitBackend ()
		{
			base.InitBackend ();
			Keyboard.KeyDown += App_KeyboardKeyDown;
		}
		public static void Main(string[] args)
		{
			using (app = new MainClass ()) {
				//				XWindow win = new XWindow (app);
				//				win.Show ();
				//app.LoadIMLFragment (@"<SimpleGauge Level='40' Margin='5' Background='Jet' Foreground='Grey' Width='30' Height='50%'/>");

				app.CMDTest = new Command(new Action(() => Console.WriteLine("test cmd"))) { Caption = "Test", Icon = new SvgPicture("#Tests.image.blank-file.svg"), CanExecute = true};
				//app.AddWidget (@"Interfaces/Divers/testFocus.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Divers/testMenu.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Divers/0.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Splitter/1.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Container/0.crow").DataSource = app;


				//app.AddWidget (@"Interfaces/Divers/colorPicker.crow").DataSource = app;
				//app.AddWidget ("Interfaces/Divers/perfMeasures.crow").DataSource = app;

				/*app.AddWidget ("#Tests.ui.dbgLog.crow").DataSource = app;

				GraphicObject go = app.AddWidget ("#Tests.ui.dbgLogColors.crow");
				go.DataSource = app;

				(go.FindByName("combo") as ComboBox).SelectedItemChanged += combo_selectedItemChanged;
				(go.FindByName("kvpList") as ListBox).SelectedItemChanged += kvpList_selectedItemChanged;*/

				app.AddWidget (@"Interfaces/Experimental/testDock.crow").DataSource = app;

				/*app.LoadIMLFragment (@"<DockWindow Width='150' Height='150' Name='dock1'/>");
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150' Name='dock2'/>");
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150' Name='dock3'/>");

				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150'/>");
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150'/>");
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150'/>");*/

				long cpt = 0;
				Measure testWidth = 100;
				int increment = 1;

				while (true) {
					cpt++;
					/*
					testWidth += increment;

					if (increment > 0) {
						if (testWidth > 500)
							increment = -increment;
					} else if (testWidth < 100) 
						increment = -increment;					
					app.NotifyValueChanged ("TestWidth", testWidth);
*/

					/*app.NotifyValueChanged ("CPT", cpt);

					if (cpt % 2 == 0)
						app.NotifyValueChanged ("TestColor", Color.Red);
					else
						app.NotifyValueChanged ("TestColor", Color.Blue);*/
					
					/*#if MEASURE_TIME
					foreach (PerformanceMeasure m in app.PerfMeasures)
						m.NotifyChanges ();	
					#endif*/
					app.ProcessEvents ();
					//Thread.Sleep(1);
				}
			}
			/*using (Display disp = new Display())
            {
                Window win = new Window(disp);
                bool running = true;

                while (running) {
                    IntPtr evt = disp.NextEvent;

                    switch ((EventType)Marshal.ReadInt32(evt))
                    {
                        case EventType.KeyPress:
                            running = false;
                            break;
                    }
                }
            }*/
		}

		void onColorUpdate (object sender, MouseButtonEventArgs e)
		{
			DbgLogViewer.colorsConf.Set (selectedEvtType.ToString (), newColor);
			DbgLogViewer.colors [selectedEvtType] = newColor;
			NotifyValueChanged ("ColorsKVPList", ColorsKVPList);
		}
		static DbgEvtType selectedEvtType;
		static Color newColor;

		static void kvpList_selectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			if (e.NewValue == null)
				return;
			selectedEvtType = ((KeyValuePair<DbgEvtType, Color>)e.NewValue).Key;
		}
		static void combo_selectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			newColor = (Color)e.NewValue;

		}

		void App_KeyboardKeyDown (object sender, KeyEventArgs e)
		{
			Console.WriteLine((byte)e.Key);
			//#if DEBUG_LOG
			switch (e.Key) {
			case Key.F2:				
				DebugLog.save (app);
				break;
			case Key.F4:				
				app.NotifyValueChanged ("ColorsKVPList", app.ColorsKVPList);
				break;
			case Key.F6:				
				saveDocking ();
				break;
			case Key.F7:				
				reloadDocking ();
				break;
			case Key.F8:				
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150'/>");
				break;
			}
			//#endif
		}

		void saveDocking () {
			DockStack ds = FindByName ("mainDock") as DockStack;
			if (ds == null) {
				Console.WriteLine ("main dock not found in graphic tree");
				return;
			}
			string conf = ds.ExportConfig ();
			Console.WriteLine ("docking conf = " + conf);
			Configuration.Global.Set ("DockingTests", conf);
		}
		void reloadDocking () {
			DockStack ds = FindByName ("mainDock") as DockStack;
			if (ds == null) {
				Console.WriteLine ("main dock not found in graphic tree");
				return;
			}

			string conf = Configuration.Global.Get<string> ("DockingTests");
			if (string.IsNullOrEmpty (conf))
				return;


			ds.ImportConfig (conf);
		}
	}
}

using System;
using System.Runtime.InteropServices;
using Crow;
using System.Threading;

namespace tests
{
	class MainClass
	{
		static Interface app;

		public static void Main(string[] args)
		{
			using (app = new Interface ()) {
				//				XWindow win = new XWindow (app);
				//				win.Show ();
				//app.LoadIMLFragment (@"<SimpleGauge Level='40' Margin='5' Background='Jet' Foreground='Grey' Width='30' Height='50%'/>");

				app.Keyboard.KeyDown += App_KeyboardKeyDown;

				//app.AddWidget (@"Interfaces/Divers/0.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Splitter/1.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Container/0.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Divers/colorPicker.crow").DataSource = app;
				//app.AddWidget ("Interfaces/Divers/perfMeasures.crow").DataSource = app;
				//app.AddWidget ("#Tests.ui.dbgLog.crow").DataSource = app;
				app.AddWidget (@"Interfaces/Experimental/testDock.crow").DataSource = app;

				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150' Name='dock1'/>");
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150' Name='dock2'/>");
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150' Name='dock3'/>");
				/*app.LoadIMLFragment (@"<DockWindow Width='150' Height='150'/>");
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150'/>");
				app.LoadIMLFragment (@"<DockWindow Width='150' Height='150'/>");*/

		
				while (true) {
					#if MEASURE_TIME
					foreach (PerformanceMeasure m in app.PerfMeasures)
						m.NotifyChanges ();	
					#endif
					Thread.Sleep(10);
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

		static void App_KeyboardKeyDown (object sender, KeyEventArgs e)
		{
			Console.WriteLine((byte)e.Key);
			//#if DEBUG_LOG
			/*switch (e.Key) {
			case Key.F2:				
				DebugLog.save (app);
				break;
			}*/
			//#endif
		}
	}
}

using System;
using System.Runtime.InteropServices;
using Crow;
using System.Threading;

namespace tests
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			using (Interface app = new Interface ()) {
				//				XWindow win = new XWindow (app);
				//				win.Show ();
				//app.LoadIMLFragment (@"<SimpleGauge Level='40' Margin='5' Background='Jet' Foreground='Grey' Width='30' Height='50%'/>");

				app.KeyboardKeyDown += App_KeyboardKeyDown;

				//app.AddWidget (@"Interfaces/Divers/0.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Splitter/1.crow").DataSource = app;
				app.AddWidget (@"Interfaces/Container/0.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Divers/colorPicker.crow").DataSource = app;
				//app.AddWidget ("Interfaces/Divers/perfMeasures.crow").DataSource = app;

				while (true) {
					/*#if MEASURE_TIME
					foreach (PerformanceMeasure m in app.PerfMeasures)
						m.NotifyChanges ();	
					#endif*/
					//Thread.Sleep(10);
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

		static void App_KeyboardKeyDown (object sender, KeyboardKeyEventArgs e)
		{
			Console.WriteLine((byte)e.Key);
			switch (e.Key) {
			case Key.Keypad1:
				DebugLog.save (sender as Interface);
				break;
			}
		}
	}
}

using System;
using System.Runtime.InteropServices;
using Crow;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace tests
{
	class MainClass : Interface
	{
		[DllImport ("dl", CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr dlopen (string file, dlmode mode);
		[DllImport ("dl", CallingConvention=CallingConvention.Cdecl)]
		static extern int dlclose (IntPtr handle);
		//void *dlsym(void *restrict handle, const char *restrict name); [Option End]
		[DllImport ("dl", CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr dlsym (IntPtr libHnd, string funcName);
		[DllImport ("mono-2.0", CallingConvention=CallingConvention.Cdecl)]
		static extern void mono_add_internal_call (string signature, IntPtr funcPtr);

		[DllImport ("__Internal", EntryPoint="cairo_stroke", CallingConvention=CallingConvention.Cdecl)]
		public static extern void cairo_stroke_internal (IntPtr ctx);
		[DllImport ("__Internal", EntryPoint="cairo_rectangle", CallingConvention=CallingConvention.Cdecl)]
		public static extern void cairo_rect_internal (IntPtr ctx, double x, double y, double w, double h);
		[DllImport ("__Internal", EntryPoint="cairo_set_source_rgba", CallingConvention=CallingConvention.Cdecl)]
		public static extern void cairo_rgba_internal (IntPtr ctx, double r, double g, double b, double a);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void cairo_stroke (IntPtr ctx);

		[Flags]
		enum dlmode {
			RTLD_LOCAL = 0,
			RTLD_LAZY = 0x00001,        /* Lazy function call binding.  */
			RTLD_NOW = 0x00002,        /* Immediate function call binding.  */
			RTLD_BINDING_MASK = 0x3,        /* Mask of binding time value.  */
			RTLD_NOLOAD = 0x00004,        /* Do not load the object.  */
			RTLD_DEEPBIND = 0x00008,        /* Use deep binding.  */
			RTLD_GLOBAL = 0x00100,
		}

		const string cairoLibPath = @"/opt/cairo/lib/libcairo.so";
		//static const string cairoLibPath = @"/opt/cairo/lib/libcairo.so.2.11513.0";

		public delegate void Stroke(IntPtr ctx);
		public delegate void Cairo4Doubles (IntPtr ctx, double x, double y, double z, double w);

		public static Stroke cairo_stroke_func;
		public static Cairo4Doubles cairo_rect_func;
		public static Cairo4Doubles cairo_set_rgba_func;

		/*[DllImport (cairoLibPath, EntryPoint="cairo_stroke")]
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public static extern void cairo_stroke_icall (IntPtr cr);*/


		static MainClass app;
		public Command CMDTest;
		public Measure TestWidth = 100;

		public Crow.IML.Instantiator instFileDlg;

		public string CurrentDirectory {
			get { return Crow.Configuration.Global.Get<string>("CurrentDirectory");}
			set {
				Crow.Configuration.Global.Set ("CurrentDirectory", value);
			}
		}

		IList<Color> testList = Color.ColorDic.Values.ToList();
		public IList<Color> TestList {
			set{
				testList = value;
				NotifyValueChanged ("TestList", testList);
			}
			get { return testList; }
		}
		/*public List<KeyValuePair<DbgEvtType, Color>> ColorsKVPList {
			get {
				return DbgLogViewer.colors.ToList();
			}
		}*/

		protected override void InitBackend ()
		{
			base.InitBackend ();
			Keyboard.KeyDown += App_KeyboardKeyDown;
		}
		public static void Main(string[] args)
		{
			//IntPtr cairoLib = dlopen (cairoLibPath, dlmode.RTLD_LAZY);



			//mono_add_internal_call ("tests.MainClass:cairo_stroke_icall(IntPtr cr)", strokeFuncPtr);

			/*cairo_stroke_func = Marshal.GetDelegateForFunctionPointer<Stroke>(dlsym (cairoLib, "cairo_stroke"));
			cairo_rect_func = Marshal.GetDelegateForFunctionPointer<Cairo4Doubles>(dlsym (cairoLib, "cairo_rectangle"));
			cairo_set_rgba_func = Marshal.GetDelegateForFunctionPointer<Cairo4Doubles>(dlsym (cairoLib, "cairo_set_source_rgba"));
*/


			using (app = new MainClass ()) {
				//				XWindow win = new XWindow (app);
				//				win.Show ();
				//app.LoadIMLFragment (@"<SimpleGauge Level='40' Margin='5' Background='Jet' Foreground='Grey' Width='30' Height='50%'/>");

				app.CMDTest = new Command(new Action(() => app.AddWidget (app.instFileDlg.CreateInstance()).DataSource = app)) { Caption = "Test", Icon = new SvgPicture("#Tests.image.blank-file.svg"), CanExecute = true};
				//app.AddWidget (@"Interfaces/Divers/testFocus.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Divers/testMenu.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Divers/testVisibility.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Divers/0.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/Splitter/1.crow").DataSource = app;
				app.AddWidget (@"Interfaces/GraphicObject/0.crow").DataSource = app;
				//app.AddWidget (@"Interfaces/TemplatedContainer/test_Listbox.crow").DataSource = app;

				/*app.instFileDlg = Crow.IML.Instantiator.CreateFromImlFragment
					(app, "<FileDialog Caption='Open File' CurrentDirectory='{²CurrentDirectory}'/>");*/
				

				//app.AddWidget (@"Interfaces/Divers/colorPicker.crow").DataSource = app;
				//app.AddWidget ("Interfaces/Divers/perfMeasures.crow").DataSource = app;

				/*app.AddWidget ("#Tests.ui.dbgLog.crow").DataSource = app;

				GraphicObject go = app.AddWidget ("#Tests.ui.dbgLogColors.crow");
				go.DataSource = app;

				(go.FindByName("combo") as ComboBox).SelectedItemChanged += combo_selectedItemChanged;
				(go.FindByName("kvpList") as ListBox).SelectedItemChanged += kvpList_selectedItemChanged;*/

				//app.AddWidget (@"Interfaces/Experimental/testDock.crow").DataSource = app;

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


			//dlclose (cairoLib);
		}

		/*void onColorUpdate (object sender, MouseButtonEventArgs e)
		{
			DbgLogViewer.colorsConf.Set (selectedEvtType.ToString (), newColor);
			DbgLogViewer.colors [selectedEvtType] = newColor;
			NotifyValueChanged ("ColorsKVPList", ColorsKVPList);
		}
		//static DbgEvtType selectedEvtType;
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

		}*/

		void App_KeyboardKeyDown (object sender, KeyEventArgs e)
		{
			Console.WriteLine((byte)e.Key);
			//#if DEBUG_LOG
			switch (e.Key) {
			case Key.F2:				
				//DebugLog.save (app);
				break;
			case Key.F4:				
				//app.NotifyValueChanged ("ColorsKVPList", app.ColorsKVPList);
				break;
			case Key.F6:
				app.LoadIMLFragment (@"<FileDialog Caption='Open File'/>");
				//saveDocking ();
				break;
			case Key.F7:				
				//reloadDocking ();
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

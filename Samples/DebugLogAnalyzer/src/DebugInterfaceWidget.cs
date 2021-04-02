// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using Crow.Cairo;
using System.Diagnostics;
using CrowDbgShared;
using System.Collections.Generic;
using Crow.DebugLogger;

namespace Crow
{	
	public class DebugInterfaceWidget : Widget {
		
		string imlSource;
		List<DbgEvent> events;
		List<DbgWidgetRecord> widgets;
		Exception currentException;
		object dbgIFace;
		AssemblyLoadContext crowLoadCtx;
		Assembly crowAssembly, thisAssembly;
		Type dbgIfaceType;
		Action<int, int> delResize;
		Func<int, int, bool> delMouseMove;
		Func<MouseButton, bool> delMouseDown, delMouseUp;
		Action delResetDirtyState;
		Action delResetDebugger;
		Action<object, string> delSaveDebugLog;
		IntPtrGetterDelegate delGetSurfacePointer;
		Action<string> delSetSource;

		FieldInfo fiDbg_IncludeEvents, fiDbg_DiscardEvents, fiDbg_ConsoleOutput;
		
		bool initialized, recording;
		string crowDbgAssemblyLocation;
		DbgEvtType recordedEvents, discardedEvents;

		public CommandGroup LoggerCommands =>
			new CommandGroup(
				new Command("Get logs", () => getLog ()),
				new Command("Reset logs", () => delResetDebugger ())
			);
		public string IMLSource {
			get => imlSource;
			set {
				if (imlSource == value)
					return;
				imlSource = value;
				if (initialized)
					delSetSource (imlSource);				
				NotifyValueChangedAuto(imlSource);
				RegisterForRedraw();
			}
		}
		public Exception CurrentException {
			get => currentException;
			set {
				if (currentException == value)
					return;
				currentException = value;									
				NotifyValueChangedAuto(currentException);								
			}
		}
		public string CrowDbgAssemblyLocation {
			get => crowDbgAssemblyLocation;
			set {
				if (crowDbgAssemblyLocation == value)
					return;
				crowDbgAssemblyLocation = value;
				NotifyValueChangedAuto(CrowDbgAssemblyLocation);
				tryStartDebugInterface();
			}
		}

		public bool Recording {
			get => recording;
			set {
				if (recording == value)
					return;
				recording = value & initialized;
				if (recording) {
					fiDbg_DiscardEvents.SetValue (dbgIFace, DiscardedEvents);
					fiDbg_IncludeEvents.SetValue (dbgIFace, RecordedEvents);					
				} else {
					fiDbg_DiscardEvents.SetValue (dbgIFace, DiscardedEvents);
					fiDbg_IncludeEvents.SetValue (dbgIFace, RecordedEvents);
				}
				NotifyValueChangedAuto(recording);
			}
		}
		public DbgEvtType RecordedEvents {
			get => recordedEvents;
			set {
				if (recordedEvents == value)
					return;
				recordedEvents = value;
				if (Recording)
					fiDbg_IncludeEvents.SetValue (dbgIFace, value);
				NotifyValueChangedAuto (recordedEvents);
			}
		}
		public DbgEvtType DiscardedEvents {
			get => discardedEvents;
			set {
				if (discardedEvents == value)
					return;				
				discardedEvents = value;
				if (Recording)
					fiDbg_DiscardEvents.SetValue (dbgIFace, value);
				NotifyValueChangedAuto (discardedEvents);
			}
		}
		public bool DebugLogToFile {
			get => initialized ? !(bool)fiDbg_ConsoleOutput.GetValue (dbgIFace) : false;
			set {
				if (!initialized || DebugLogToFile == value)
					return;				
				fiDbg_ConsoleOutput.SetValue (dbgIFace, !value);
				NotifyValueChangedAuto (DebugLogToFile);
			}
		}


		protected override void onInitialized(object sender, EventArgs e)
		{
			base.onInitialized(sender, e);

			
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);			
		}

		public bool CrowDebuggerOK => initialized;
		public bool CrowDebuggerNOK => !initialized;
		void notifyCrowDebuggerState () {
			NotifyValueChanged("CrowDebuggerOK", CrowDebuggerOK);
			NotifyValueChanged("CrowDebuggerNOK", CrowDebuggerNOK);
		}

		void tryStartDebugInterface () {
			if (initialized)
				return;
			if (!File.Exists (crowDbgAssemblyLocation))	{
				notifyCrowDebuggerState();
				return;
			}
			
			crowLoadCtx = new AssemblyLoadContext("CrowDebuggerLoadContext");
		
			//using (crowLoadCtx.EnterContextualReflection()) {
				crowAssembly = crowLoadCtx.LoadFromAssemblyPath (crowDbgAssemblyLocation);
				thisAssembly = crowLoadCtx.LoadFromAssemblyPath (new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);				

				Type debuggerType = crowAssembly.GetType("Crow.DbgLogger");
				if (!(bool)debuggerType.GetField("IsEnabled").GetValue(null)) {
					notifyCrowDebuggerState();
					return;
				}				

				dbgIfaceType = thisAssembly.GetType("Crow.DebugInterface");
				
				dbgIFace = Activator.CreateInstance (dbgIfaceType, new object[] {IFace.WindowHandle});

				delResize = (Action<int, int>)Delegate.CreateDelegate(typeof(Action<int, int>),
											dbgIFace, dbgIfaceType.GetMethod("Resize"));

				delMouseMove = (Func<int, int, bool>)Delegate.CreateDelegate(typeof(Func<int, int, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseMove"));

				delMouseDown = (Func<MouseButton, bool>)Delegate.CreateDelegate(typeof(Func<MouseButton, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseButtonDown"));

				delMouseUp = (Func<MouseButton, bool>)Delegate.CreateDelegate(typeof(Func<MouseButton, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseButtonUp"));

				delGetSurfacePointer = (IntPtrGetterDelegate)Delegate.CreateDelegate(typeof(IntPtrGetterDelegate),
											dbgIFace, dbgIfaceType.GetProperty("SurfacePointer").GetGetMethod());
				delSetSource = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>),
											dbgIFace, dbgIfaceType.GetProperty("Source").GetSetMethod());	

				delResetDirtyState = (Action)Delegate.CreateDelegate(typeof(Action),
											dbgIFace, dbgIfaceType.GetMethod("ResetDirtyState"));

				fiDbg_IncludeEvents = debuggerType.GetField("IncludeEvents");
				fiDbg_DiscardEvents = debuggerType.GetField("DiscardEvents");
				fiDbg_ConsoleOutput = debuggerType.GetField("ConsoleOutput");
				delResetDebugger = (Action)Delegate.CreateDelegate(typeof(Action),
											null, debuggerType.GetMethod("Reset"));
				/*delSaveDebugLog = (Action<object, string>)Delegate.CreateDelegate(typeof(Action<object, string>),
											null, debuggerType.GetMethod("Save", new Type[] {dbgIfaceType, typeof(string)}));*/

				dbgIfaceType.GetMethod("RegisterDebugInterfaceCallback").Invoke (dbgIFace, new object[] {this} );				
				dbgIfaceType.GetMethod("Run").Invoke (dbgIFace, null);

				initialized = true;
										
				//Console.WriteLine($"DbgIFace: LoadCtx:{AssemblyLoadContext.GetLoadContext (dbgIFace.GetType().Assembly).Name})");
			//}
		}

		protected override void onDraw(Context gr)
		{
			Console.WriteLine("onDraw");
			gr.SetSource(Colors.RoyalBlue);
			gr.Paint();
		}
		public override bool CacheEnabled { get => true; set => base.CacheEnabled = true; }

		public override void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			if (initialized) {				
				Point m = ScreenPointToLocal (e.Position);			
				delMouseMove (m.X, m.Y);									
				e.Handled = true;
			}
			base.onMouseMove(sender, e);
		}
		public override void onMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (initialized) {				
				delMouseDown (e.Button);
				e.Handled=true;
			}
			base.onMouseDown (sender, e);			
		}
		public override void onMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (initialized) {				
				delMouseUp (e.Button);			
				e.Handled=true;
			}
			base.onMouseUp (sender, e);
		}

		protected override void RecreateCache()
		{
			bmp?.Dispose ();		
			
			if (initialized) {
				delResize (Slot.Width, Slot.Height);			
				bmp = Crow.Cairo.Surface.Lookup (delGetSurfacePointer(), false);				
			} else
				bmp = IFace.surf.CreateSimilar (Content.ColorAlpha, Slot.Width, Slot.Height);								

			IsDirty = false;			
		}
		protected override void UpdateCache(Context ctx)
		{			
			if (initialized && bmp != null) {				
				paintCache (ctx, Slot + Parent.ClientRectangle.Position);
				delResetDirtyState ();				
			}
			
		}

		void getLog () {
			using (Stream stream = new MemoryStream (1024)) {
				Type debuggerType = crowAssembly.GetType("Crow.DbgLogger");
				debuggerType.GetMethod("Save", new Type[] {dbgIfaceType, typeof(Stream)}).Invoke(null, new object[] {dbgIFace, stream});
				//debuggerType.GetMethod("Save", new Type[] {dbgIfaceType, typeof(string)}).Invoke(null, new object[] {dbgIFace, "debug.log"});
				//delSaveDebugLog(dbgIFace, "debug.log");
				stream.Seek(0, SeekOrigin.Begin);
				events = new List<DbgEvent>();
				widgets = new List<DbgWidgetRecord>();
				DbgLogger.Load (stream, events, widgets);
				//DbgLogger.Load ("debug.log", events, widgets);
				DebugLogAnalyzer.Program dla = IFace as DebugLogAnalyzer.Program;
				dla.Widgets = widgets;
				dla.Events = events;
			}
		}
	}
}
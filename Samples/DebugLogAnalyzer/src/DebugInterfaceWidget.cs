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
using System.Collections.Generic;
using Crow.DebugLogger;
using System.Linq;

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
		Func<float, bool> delMouseWheelChanged;
		Func<MouseButton, bool> delMouseDown, delMouseUp;
		Func<char, bool> delKeyPress;
		Func<Key, bool> delKeyDown, delKeyUp;
		Action delResetDirtyState;
		Action delResetDebugger;
		Action<object, string> delSaveDebugLog;		
		Func<IntPtr> delGetSurfacePointer;
		Action<string> delSetSource;

		FieldInfo fiDbg_IncludeEvents, fiDbg_DiscardEvents, fiDbg_ConsoleOutput;
		
		bool initialized, recording;
		string crowDbgAssemblyLocation;
		DbgEvtType recordedEvents, discardedEvents;

		public CommandGroup LoggerCommands =>
			new CommandGroup(
				new Command("Get logs", () => getLog ()),
				new Command("Reset logs", () => delResetDebugger ()),
				new Command("Save to file", () => saveLogToDebugLogFilePath ()),
				new Command("Load from file", () => loadLogFromDebugLogFilePath ())
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
		public string DebugLogFilePath {
			get => Configuration.Global.Get<string> ("DebugLogFilePath");
			set {
				if (DebugLogFilePath == value)
					return;
				Configuration.Global.Set ("DebugLogFilePath", value);
				NotifyValueChangedAuto (value);
			}
		}
		int firstWidgetIndexToSave, lastWidgetIndexToSave;
		public int FirstWidgetIndexToSave {
			get => firstWidgetIndexToSave;
			set {
				if (firstWidgetIndexToSave == value)
					return;
				if (value > lastWidgetIndexToSave)
					firstWidgetIndexToSave = lastWidgetIndexToSave;
				else
					firstWidgetIndexToSave = value;

				NotifyValueChangedAuto (firstWidgetIndexToSave);
			}
		}
		public int LastWidgetIndexToSave {
			get => lastWidgetIndexToSave;
			set {
				if (lastWidgetIndexToSave == value)
					return;
				if (lastWidgetIndexToSave > widgets.Count)
					lastWidgetIndexToSave = widgets.Count;
				else
					lastWidgetIndexToSave = value;

				NotifyValueChangedAuto (lastWidgetIndexToSave);
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
		public string CrowDebuggerErrorMessage = "";
		void notifyCrowDebuggerState (string errorMsg = null) {
			NotifyValueChanged("CrowDebuggerOK", CrowDebuggerOK);
			NotifyValueChanged("CrowDebuggerNOK", CrowDebuggerNOK);
			CrowDebuggerErrorMessage = errorMsg;
			NotifyValueChanged("CrowDebuggerErrorMessage", CrowDebuggerErrorMessage);
		}

		void tryStartDebugInterface () {
			if (initialized)
				return;
			if (!File.Exists (crowDbgAssemblyLocation))	{
				notifyCrowDebuggerState($"Crow.dll for debugging file not found");
				return;
			}
			
			crowLoadCtx = new AssemblyLoadContext("CrowDebuggerLoadContext");
		
			//using (crowLoadCtx.EnterContextualReflection()) {
				crowAssembly = crowLoadCtx.LoadFromAssemblyPath (crowDbgAssemblyLocation);
				thisAssembly = crowLoadCtx.LoadFromAssemblyPath (new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);				

				Type debuggerType = crowAssembly.GetType("Crow.DbgLogger");
				if (!(bool)debuggerType.GetField("IsEnabled").GetValue(null)) {
					notifyCrowDebuggerState("Crow.dll must be compiled with CrowDebugLogEnabled='True'");
					return;
				}				

				dbgIfaceType = thisAssembly.GetType("Crow.DebugInterface");
				
				dbgIFace = Activator.CreateInstance (dbgIfaceType, new object[] {IFace.WindowHandle});

				delResize = (Action<int, int>)Delegate.CreateDelegate(typeof(Action<int, int>),
											dbgIFace, dbgIfaceType.GetMethod("Resize"));

				delMouseMove = (Func<int, int, bool>)Delegate.CreateDelegate(typeof(Func<int, int, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseMove"));

				delMouseWheelChanged = (Func<float, bool>)Delegate.CreateDelegate(typeof(Func<float, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseWheelChanged"));


				delMouseDown = (Func<MouseButton, bool>)Delegate.CreateDelegate(typeof(Func<MouseButton, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseButtonDown"));

				delMouseUp = (Func<MouseButton, bool>)Delegate.CreateDelegate(typeof(Func<MouseButton, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseButtonUp"));

				delKeyDown = (Func<Key, bool>)Delegate.CreateDelegate(typeof(Func<Key, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnKeyDown"));
				delKeyUp = (Func<Key, bool>)Delegate.CreateDelegate(typeof(Func<Key, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnKeyUp"));
				delKeyPress = (Func<char, bool>)Delegate.CreateDelegate(typeof(Func<char, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnKeyPress"));


				delGetSurfacePointer = (Func<IntPtr>)Delegate.CreateDelegate(typeof(Func<IntPtr>),
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
				notifyCrowDebuggerState();
										
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

		public override void onKeyDown(object sender, KeyEventArgs e)
		{
			if (initialized) {
				try
				{					
					e.Handled = delKeyDown (e.Key);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace key down]{ex}");
				}				
			}
			base.onKeyDown(sender, e);
		}
		public override void onKeyUp(object sender, KeyEventArgs e)
		{
			if (initialized) {
				try
				{					
					e.Handled = delKeyUp (e.Key);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace key up]{ex}");
				}				
			}
			base.onKeyUp(sender, e);
		}
		public override void onKeyPress(object sender, KeyPressEventArgs e)
		{
			if (initialized) {
				try
				{					
					e.Handled = delKeyPress (e.KeyChar);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace key press]{ex}");
				}				
			}
			base.onKeyPress(sender, e);
		}
		public override void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			if (initialized) {
				try
				{
					Point m = ScreenPointToLocal (e.Position);
					e.Handled = delMouseMove (m.X, m.Y);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace mouse move]{ex}");
				}				
			}
			base.onMouseMove(sender, e);
		}
		public override void onMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (initialized) {				
				try
				{
					e.Handled = delMouseDown (e.Button);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace mouse down]{ex}");
				}				
			}
			base.onMouseDown (sender, e);			
		}
		public override void onMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (initialized) {				
				try
				{
					e.Handled = delMouseUp (e.Button);			
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace mouse up]{ex}");
				}				
			}
			base.onMouseUp (sender, e);
		}
		public override void onMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (initialized) {				
				try
				{
					e.Handled = delMouseWheelChanged (e.Delta);			
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace mouse wheel change]{ex}");
				}				
			}
			base.onMouseWheel(sender, e);
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

		int firstWidgetIndexToGet = 0;
		void getLog () {
			DebugLogAnalyzer.Program dla = IFace as DebugLogAnalyzer.Program;

			using (Stream stream = new MemoryStream (1024)) {
				Type debuggerType = crowAssembly.GetType("Crow.DbgLogger");
				MethodInfo miSave = debuggerType.GetMethod("Save",
					new Type[] {
						dbgIfaceType,
						typeof(Stream),
						typeof(int),
						typeof(bool)
					});


				widgets = new List<DbgWidgetRecord>();
				events = new List<DbgEvent>();
				miSave.Invoke(null, new object[] {dbgIFace, stream, firstWidgetIndexToGet, true});
				stream.Seek(0, SeekOrigin.Begin);
				DbgLogger.Load (stream, events, widgets);

				lock (dla.UpdateMutex) {
					for (int i = 0; i < widgets.Count; i++) {
						widgets[i].listIndex = dla.Widgets.Count;
						dla.Widgets.Add	(widgets[i]);
					}
					for (int i = 0; i < events.Count; i++) {
						dla.Events.Add (events[i]);
						updateWidgetEvents (dla.Widgets, events[i]);
					}
				}				
				firstWidgetIndexToGet += widgets.Count;				
				if (widgets.Count > 0 && firstWidgetIndexToGet != widgets.Last().InstanceIndex + 1)
					Debugger.Break ();
			}
		}
		void updateWidgetEvents (IList<DbgWidgetRecord> widgets, DbgEvent evt) {
			if (evt is DbgWidgetEvent we)
				widgets.FirstOrDefault (w => w.InstanceIndex == we.InstanceIndex)?.Events.Add (we);
			if (evt.Events == null)
				return;
			foreach (DbgEvent e in evt.Events) 
				updateWidgetEvents (widgets, e);			
		}
		void saveLogToDebugLogFilePath () {

		}
		void loadLogFromDebugLogFilePath () {

		}

		public virtual object GetScreenCoordinates () => ScreenCoordinates(Slot).TopLeft;
	}
}
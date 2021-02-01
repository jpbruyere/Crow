// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Cairo;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;


namespace Crow
{
	[Flags]
	public enum DbgEvtType {
		////9 nth bit set for iface event
		IFace							= 0x10000,
		Focus							= 0x20000,
		Override						= 0x40000,
		Binding							= 0x80000,
		Widget 							= 0x00100,
		//GOLayouting 					= 0x00200,
		//Drawing 						= 0x00400,
		Lock 							= 0x00800,
		Layouting	 					= IFace | 0x01000,
		Clipping						= IFace | 0x02000,
		Drawing							= IFace | 0x04000,
		Update							= IFace | 0x08000,
		IFaceLoad						= IFace | 0x05,
		IFaceInit						= IFace | 0x06,
		CreateITor						= IFace | 0x07,

		HoverWidget						= IFace | Focus | Widget | 0x01,
		FocusedWidget					= IFace | Focus | Widget | 0x02,
		ActiveWidget					= IFace | Focus | Widget | 0x03,
		UnfocusedWidget					= IFace | Focus | Widget | 0x04,

		//10 nth bit set for graphic obj
		TemplatedGroup					= 0x1000,
		Dispose		 					= 0x2000,
		Warning 						= 0x4000,
		Error							= 0x8000,
		GOClassCreation					= Widget | 0x01,
		GOInitialization				= Widget | 0x02,
		GORegisterForGraphicUpdate		= Widget | 0x03,
		GOEnqueueForRepaint				= Widget | 0x04,
		GONewDataSource					= Widget | 0x05,
		GONewParent						= Widget | 0x06,
		GONewLogicalParent				= Widget | 0x07,
		GOAddChild		 				= Widget | 0x08,

		GOSearchLargestChild			= Widget | 0x09,
		GOSearchTallestChild 			= Widget | 0x0A,
		GORegisterForRedraw		 		= Widget | 0x0B,

		AlreadyDisposed					= Dispose | Widget | Error | 0x01,
		DisposedByGC					= Dispose | Widget | Error | 0x02,
		Disposing 						= Dispose | Widget | 0x01,

		GOClippingRegistration			= Clipping | Widget | 0x01,
		GORegisterClip					= Clipping | Widget | 0x02,
		GORegisterLayouting 			= Layouting | Widget | 0x01,
		GOProcessLayouting				= Layouting | Widget | 0x02,
		GOProcessLayoutingWithNoParent 	= Layouting | Widget | Warning | 0x01,
		GOMeasure						= Widget | 0x03,
		GODraw							= Drawing | Widget | 0x01,
		GORecreateCache					= Drawing | Widget | 0x02,
		GOUpdateCache					= Drawing | Widget | 0x03,
		GOPaint							= Drawing | Widget | 0x04,

		GOLockUpdate					= Widget | Lock | 0x01,
		GOLockClipping					= Widget | Lock | 0x02,
		GOLockRender					= Widget | Lock | 0x03,
		GOLockLayouting					= Widget | Lock | 0x04,

		TGLoadingThread					= Widget | TemplatedGroup | 0x01,
		TGCancelLoadingThread			= Widget | TemplatedGroup | 0x02,

		All = 0x0FFFFFFF
	}
	
	public static class DbgLogger
	{
		public static DbgEvtType IncludeEvents = DbgEvtType.All;
		public static DbgEvtType DiscardEvents = DbgEvtType.Focus;
		public static bool ConsoleOutput = true;

		static bool logevt (DbgEvtType evtType)
			=> (evtType & DiscardEvents) == 0 && (evtType & IncludeEvents) != 0;


		static object logMutex = new object ();
		static Stopwatch chrono = Stopwatch.StartNew ();
		static List<DbgEvent> events = new List<DbgEvent> ();
		//started events per thread
		static Dictionary<int, Stack<DbgEvent>> startedEvents = new Dictionary<int, Stack<DbgEvent>> ();
		//helper for fetching current event list to add next event to while recording
		static List<DbgEvent> curEventList {
			get {
				if (startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId)) {
					if (startedEvents [Thread.CurrentThread.ManagedThreadId].Count == 0)
						return events;
					DbgEvent e = startedEvents [Thread.CurrentThread.ManagedThreadId].Peek ();
					if (e.Events == null) 
						e.Events = new List<DbgEvent> ();
					return e.Events;
				}
				return events;
			}
		}

		[Conditional ("DEBUG_LOG")]
		public static void StartEvent (DbgEvtType evtType, params object[] data)
		{
			if (!logevt (evtType))
				return;
			lock (logMutex) {
				chrono.Stop ();
				DbgEvent evt = addEventInternal (evtType, data);
				if (!startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId))
					startedEvents [Thread.CurrentThread.ManagedThreadId] = new Stack<DbgEvent> ();
				startedEvents [Thread.CurrentThread.ManagedThreadId].Push (evt);
				chrono.Start ();				
			}
		}

		[Conditional ("DEBUG_LOG")]
		public static void EndEvent (DbgEvtType evtType, bool discardIfNoChildEvents = false)
		{
			if (!logevt (evtType))
				return;

			lock (logMutex) {
				chrono.Stop ();
				if (!startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId))
					throw new Exception ("Current thread has no event started");
				DbgEvent e = startedEvents [Thread.CurrentThread.ManagedThreadId].Pop ();
				if (e.type != evtType)
					throw new Exception ($"Begin/end event logging mismatch: {e.type}/{evtType}");
				if (discardIfNoChildEvents && (e.Events == null || e.Events.Count == 0))
					curEventList.Remove (e);
				else
					e.end = chrono.ElapsedTicks;
				chrono.Start ();
			}
		}

		// End layouting queue event and set the corresponding lqi
		[Conditional ("DEBUG_LOG")]
		public static void EndEvent (DbgEvtType evtType, LayoutingQueueItem lqi) {
			if (!logevt (evtType))
				return;

			lock (logMutex) {
				chrono.Stop ();
				if (!startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId))
					throw new Exception ("Current thread has no event started");
				DbgLayoutEvent e = startedEvents[Thread.CurrentThread.ManagedThreadId].Pop () as DbgLayoutEvent;
				if (e?.type != evtType)
					throw new Exception ($"Begin/end event logging mismatch: {e.type}/{evtType}");
				e.end = chrono.ElapsedTicks;
				e.SetLQI (lqi);
				chrono.Start ();
			}

		}
		/// <summary>
		/// End event by reference to cancel unended events on failure
		/// </summary>
		/*
		public static void EndEvent (DbgEvtType evtType, DbgEvent evt)
		{
			if (!logevt (evtType))
				return;

			lock (logMutex) {
				chrono.Stop ();
				if (!startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId))
					throw new Exception ("Current thread has no event started");
				DbgEvent e = startedEvents [Thread.CurrentThread.ManagedThreadId].Pop ();
				while (e != evt)
					e = startedEvents [Thread.CurrentThread.ManagedThreadId].Pop ();
				e.end = chrono.ElapsedTicks;
				chrono.Start ();
				return e;
			}
		}*/

		[Conditional("DEBUG_LOG")]
		public static void AddEvent (DbgEvtType evtType, params object [] data) { 
			if (!logevt (evtType))
				return;

			lock (logMutex) {
				chrono.Stop ();
				DbgEvent evt = addEventInternal (evtType, data);
				chrono.Start ();
			}
		}

		static DbgEvent addEventInternal (DbgEvtType evtType, params object [] data)
		{
			DbgEvent evt = null;
#if DEBUG_LOG
			if (data == null || data.Length == 0)
				evt = new DbgEvent (chrono.ElapsedTicks, evtType);
			else if (data [0] is Widget w)
				evt = new DbgWidgetEvent (chrono.ElapsedTicks, evtType, w);
			else if (data [0] is LayoutingQueueItem lqi)
				evt = new DbgLayoutEvent (chrono.ElapsedTicks, evtType, lqi);
			else
				evt = new DbgEvent (chrono.ElapsedTicks, evtType);

			if (ConsoleOutput) {
				if (evt.type.HasFlag (DbgEvtType.Error)) {
					Console.ForegroundColor = ConsoleColor.Red;
				}
				Console.WriteLine (evt.Print());
				Console.ResetColor ();
			} else
				curEventList.Add (evt);
#endif
			return evt;
		}
#if DEBUG_LOG
		static void parseTree (Widget go, int level = 0, int y = 1) {
			if (go == null)
				return;

			go.yIndex = y;
			go.xLevel = level;

			Group gr = go as Group;
			if (gr != null) {
				foreach (Widget g in gr.Children) 
					parseTree (g, level + 1, y + 1);

			} else {
				PrivateContainer pc = go as PrivateContainer;
				if (pc != null)
					parseTree (pc.getTemplateRoot, level + 1, y + 1);				
			}
		}

#endif
		/// <summary>
		/// Clear all recorded events from logger.
		/// </summary>
		public static void Reset () {
#if DEBUG_LOG
			lock (logMutex) {
			startedEvents.Clear ();
				events.Clear ();
				chrono.Restart ();
			}
			Console.WriteLine ($"Crow Debug Log reseted");
#else
			Console.WriteLine ($"Logging disabled, compile Crow with DEBUG and DEBUG_LOG defined to enable logging.");
#endif
		}
		/// <summary>
		/// Save recorded events to disk
		/// </summary>
		/// <param name="iface">Iface.</param>		
		public static void Save(Interface iface, string dbgLogFilePath = "debug.log") {
#if DEBUG_LOG
			lock (logMutex) {

				foreach (Widget go in iface.GraphicTree)
					parseTree (go);

				using (StreamWriter s = new StreamWriter (dbgLogFilePath)) {
					s.WriteLine ("[GraphicObjects]");
					lock (Widget.GraphicObjects) {
						//Widget.GraphicObjects = Widget.GraphicObjects.OrderBy (o => o.yIndex).ToList ();
						for (int i = 0; i < Widget.GraphicObjects.Count; i++) {
							Widget g = Widget.GraphicObjects [i];
							s.WriteLine ($"{g.GetType ().Name};{g.yIndex};{g.xLevel};{g.Width};{g.Height}");
						}
					}
					s.WriteLine ("[Events]");
					saveEventList (s, events);
				}
			}
			Console.WriteLine ($"Crow Debug Log saved to: {dbgLogFilePath}");
#else
			Console.WriteLine ($"Compile Crow with DEBUG and DEBUG_LOG defined to enable logging. No log saved.");
#endif
		}

		static void saveEventList (StreamWriter s, List<DbgEvent> evts, int level = 0)
		{
			foreach (DbgEvent e in evts) {
				if (e == null)
					continue;
				s.WriteLine (new string ('\t', level) + e);
				if (e.Events != null)
					saveEventList (s, e.Events, level + 1);
			}
		}
	}
}



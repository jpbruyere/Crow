using System.Text;
// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Crow.DebugLogger;

namespace Crow
{	
	public static class DbgLogger
	{
		public static DbgEvtType IncludeEvents = DbgEvtType.All;
		public static DbgEvtType DiscardEvents = DbgEvtType.Focus;
		public static bool ConsoleOutput = true;


#if DEBUG_LOG
		static bool logevt (DbgEvtType evtType)
			//=> IncludeEvents != DbgEvtType.None && (evtType & DiscardEvents) == 0 && (evtType & IncludeEvents) == IncludeEvents;
			//=> IncludeEvents != DbgEvtType.None && (evtType & DiscardEvents) == 0 && (evtType & IncludeEvents) == IncludeEvents;
			=> IncludeEvents == DbgEvtType.All || (IncludeEvents != DbgEvtType.None && (evtType & IncludeEvents) != 0);
			


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
		public static readonly bool IsEnabled = true;
#else
		public static readonly bool IsEnabled = false;
#endif

		[Conditional ("DEBUG_LOG")]
		public static void StartEvent (DbgEvtType evtType, params object[] data)
		{
#if DEBUG_LOG
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
#endif
		}
[Conditional ("DEBUG_LOG")]
		public static void SetMsg (DbgEvtType evtType, string message)
		{
#if DEBUG_LOG
			if (!logevt (evtType))
				return;
lock (logMutex) {
				chrono.Stop ();
				if (!startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId))
					throw new Exception ("Current thread has no event started");
				DbgEvent e = startedEvents [Thread.CurrentThread.ManagedThreadId].Peek ();
				if (e.type != evtType)
					throw new Exception ($"Begin/end event logging mismatch: {e.type}/{evtType}");
				e.Message = message;
				chrono.Start ();
			}
#endif
		}
		[Conditional ("DEBUG_LOG")]
		public static void EndEvent (DbgEvtType evtType, bool discardIfNoChildEvents = false)
		{
#if DEBUG_LOG			
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
#endif
		}

		// End layouting queue event and set the corresponding lqi
		[Conditional ("DEBUG_LOG")]
		public static void EndEvent (DbgEvtType evtType, LayoutingQueueItem lqi) {
#if DEBUG_LOG			
			if (!logevt (evtType))
				return;

			lock (logMutex) {
				chrono.Stop ();
				if (!startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId))
					throw new Exception ($"Current thread has no event started\n{new System.Diagnostics.StackTrace()}");

				DbgLayoutEvent e = startedEvents[Thread.CurrentThread.ManagedThreadId].Pop () as DbgLayoutEvent;
				if (e?.type != evtType)
					throw new Exception ($"Begin/end event logging mismatch: {e.type}/{evtType}\n{new System.Diagnostics.StackTrace()}");
				e.end = chrono.ElapsedTicks;
				e.SetLQI (lqi.LayoutType, lqi.result, lqi.Slot, lqi.NewSlot);
				chrono.Start ();
			}
#endif
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
#if DEBUG_LOG			
			if (!logevt (evtType))
				return;

			lock (logMutex) {
				chrono.Stop ();
				DbgEvent evt = addEventInternal (evtType, data);
				chrono.Start ();
			}
#endif
		}
		[Conditional("DEBUG_LOG")]
		public static void AddEventWithMsg (DbgEvtType evtType, string message, params object [] data) {
#if DEBUG_LOG			
			if (!logevt (evtType))
				return;

			lock (logMutex) {
				chrono.Stop ();
				DbgEvent evt = addEventInternal (evtType, data);
				evt.Message = message;
				chrono.Start ();
			}
#endif
		}

#if DEBUG_LOG
		static DbgEvent addEventInternal (DbgEvtType evtType, params object [] data)
		{
			DbgEvent evt = null;
			if (data == null || data.Length == 0)
				evt = new DbgEvent (chrono.ElapsedTicks, evtType);
			else if (data [0] is Widget w) {
				evt = new DbgWidgetEvent (chrono.ElapsedTicks, evtType, w.instanceIndex);
				if (evtType == DbgEvtType.GONewParent) {
					if (data[1] is Widget wi)
						evt.Message = $"{wi.instanceIndex}";
					else if (data[1] is Interface)
						evt.Message = "Interface";
					else
						evt.Message = $"{data[1]}";
				}
					
			} else if (data [0] is LayoutingQueueItem lqi)
				evt = new DbgLayoutEvent (chrono.ElapsedTicks, evtType, (lqi.Layoutable as Widget).instanceIndex, lqi.LayoutType, lqi.result, lqi.Slot, lqi.NewSlot);
			else
				evt = new DbgEvent (chrono.ElapsedTicks, evtType);

			if (ConsoleOutput) {
				if (evt.type.HasFlag (DbgEvtType.Error)) {
					Console.ForegroundColor = ConsoleColor.Red;
				}				
				if (evt is DbgWidgetEvent we) 
					Console.WriteLine ($"{evt.Print()} {Widget.GraphicObjects[we.InstanceIndex]}");
				else
					Console.WriteLine ($"{evt.Print()}");
				Console.ResetColor ();
			} else
				curEventList.Add (evt);
			return evt;
		}

		static void parseTree (Widget go, int xLevel = 1, int y = 0) {
			if (go == null)
				return;

			go.yIndex = y;
			go.xLevel = xLevel;

			if (go is Group gr) {
				for (int i = 0; i < gr.Children.Count; i++)				
					parseTree (gr.Children[i], xLevel + 1, i);
			} else if (go is PrivateContainer pc)
				parseTree (pc.getTemplateRoot, xLevel + 1);		
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

#endif
		/// <summary>
		/// Clear all recorded events from logger.
		/// </summary>
		[Conditional("DEBUG_LOG")]
		public static void Reset () {
#if DEBUG_LOG
			lock (logMutex) {
				startedEvents.Clear ();
				events.Clear ();
				/*lock (Widget.GraphicObjects)
					Widget.GraphicObjects.Clear();*/
				chrono.Restart ();
			}
			Console.WriteLine ($"Crow Debug Log reseted");
#endif
		}
		/// <summary>
		/// Save recorded events to disk
		/// </summary>
		/// <param name="iface">Iface.</param>
		public static void Save(Interface iface, string dbgLogFilePath = "debug.log") {
#if DEBUG_LOG
			using (Stream stream = new FileStream (dbgLogFilePath, FileMode.Create, FileAccess.Write))
				Save (iface, stream);
			Console.WriteLine ($"Crow Debug Log saved to: {System.IO.Path.GetFullPath(dbgLogFilePath)}");
#endif
		}
		[Conditional("DEBUG_LOG")]
		public static void Save(Interface iface, Stream stream, int startingWidgetsIndex = -1, bool saveEvents = true) {			
#if DEBUG_LOG
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 1024, true)) {
				lock (logMutex)
				lock (iface.UpdateMutex) {
					chrono.Stop();

					if (startingWidgetsIndex >= 0 ) {
						foreach (Widget go in iface.GraphicTree)
							parseTree (go);

						if (saveEvents)
							writer.WriteLine ("[GraphicObjects]");
						for (int i = startingWidgetsIndex; i < Widget.GraphicObjects.Count; i++) {
							Widget g = Widget.GraphicObjects [i];
							writer.WriteLine ($"{g.GetType ().Name};{g.instanceIndex};{g.yIndex};{g.xLevel}");
						}
					}

					if (saveEvents) {
						if (startingWidgetsIndex >= 0)				
							writer.WriteLine ("[Events]");
						saveEventList (writer, events);
						startedEvents.Clear ();
						events.Clear ();
					}
					chrono.Start();
				}
			}
#endif
		}
		public static void Load (string logFile, List<DbgEvent> events, List<DbgWidgetRecord> widgets)
		{
			if (!File.Exists (logFile))
				return;
			using (Stream stream = new FileStream (logFile, FileMode.Open, FileAccess.Read))
				Load (stream, events, widgets);			
		}
		public static void Load (Stream stream, List<DbgEvent> events, List<DbgWidgetRecord> widgets)
		{			
			using (StreamReader reader = new StreamReader (stream)) {
				
				if (widgets != null) {
					if (events != null && reader.ReadLine () != "[GraphicObjects]")
						return;
					while (!reader.EndOfStream) {
						string l = reader.ReadLine ();
						if (l == "[Events]")
							break;
						DbgWidgetRecord o = DbgWidgetRecord.Parse (l);
						o.listIndex = widgets.Count;
						widgets.Add (o);
					}
				}

				if (events == null)
					return;

				Stack<DbgEvent> startedEvents = new Stack<DbgEvent> ();
				if (!reader.EndOfStream) {
					while (!reader.EndOfStream) {
						int level = 0;
						while (reader.Peek () == (int)'\t') {
							reader.Read ();
							level++;
						}
						DbgEvent evt = DbgEvent.Parse (reader.ReadLine ());							
						if (level == 0) {
							startedEvents.Clear ();
							events.Add (evt);
						} else {
							int levelDiff = level - startedEvents.Count + 1;
							if (levelDiff > 0) {
								if (levelDiff > 1)
									System.Diagnostics.Debugger.Break ();
								startedEvents.Peek ().AddEvent (evt);
							} else {
								startedEvents.Pop ();
								if (-levelDiff > startedEvents.Count)
									System.Diagnostics.Debugger.Break ();
								while (startedEvents.Count > level)
									startedEvents.Pop ();
								startedEvents.Peek ().AddEvent (evt);
							}
						}
						startedEvents.Push (evt);
						/*if (evt.type.HasFlag (DbgEvtType.Widget)) {
							DbgWidgetEvent dwe =  evt as DbgWidgetEvent;
							if (dwe.InstanceIndex >= 0)
								widgets [dwe.InstanceIndex].Events.Add (evt);
						}*/
					}
					startedEvents.Pop();

				}
			}
		}
	}
}



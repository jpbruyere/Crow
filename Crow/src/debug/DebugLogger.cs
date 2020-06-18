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

#if DEBUG_LOG
namespace Crow
{
	/*public class LayoutingEvent : DbgEvent {
		public List<LayoutingQueueItem> lqis = new List<LayoutingQueueItem>();

	}*/
	[Flags]
	public enum DbgEvtType {
		////9 nth bit set for iface event
		IFace							= 0x10000,
		IFaceFocus						= 0x20000,
		GraphicObject 					= 0x00100,
		GOLayouting 					= 0x00200,
		Drawing 						= 0x00400,
		GOLock 							= 0x00800,
		IFaceLayouting 					= IFace | 0x01,
		IFaceClipping					= IFace | 0x02,
		IFaceDrawing					= IFace | 0x03,
		IFaceUpdate						= IFace | 0x04,
		IFaceLoad						= IFace | 0x05,
		IFaceInit						= IFace | 0x06,

		HoverWidget						= IFaceFocus | 0x01,
		FocusedWidget					= IFaceFocus | 0x02,
		ActiveWidget					= IFaceFocus | 0x03,

		//10 nth bit set for graphic obj
		Warning = 0x4000,
		Error							= 0x8000,
		GOClassCreation					= GraphicObject | 0x01,
		GOInitialization				= GraphicObject | 0x02,
		GOClippingRegistration			= GraphicObject | 0x03,
		GORegisterClip					= GraphicObject | 0x04,
		GORegisterForGraphicUpdate		= GraphicObject | 0x05,
		GOEnqueueForRepaint				= GraphicObject | 0x06,
		GONewDataSource					= GraphicObject | 0x07,
		TemplatedGroup					= 0x1000,
		GORegisterLayouting 			= GraphicObject | GOLayouting | 0x01,
		GOProcessLayouting				= GraphicObject | GOLayouting | 0x02,
		GOProcessLayoutingWithNoParent 	= Warning | GraphicObject | GOLayouting | 0x01,
		GODraw							= GraphicObject | Drawing | 0x01,
		GORecreateCache					= GraphicObject | Drawing | 0x02,
		GOUpdateCache		= GraphicObject | Drawing | 0x03,
		GOPaint							= GraphicObject | Drawing | 0x04,

		GOLockUpdate					= GraphicObject | GOLock | 0x01,
		GOLockClipping					= GraphicObject | GOLock | 0x02,
		GOLockRender					= GraphicObject | GOLock | 0x03,
		GOLockLayouting					= GraphicObject | GOLock | 0x04,

		TGLoadingThread					= GraphicObject | TemplatedGroup | 0x01,
		TGCancelLoadingThread			= GraphicObject | TemplatedGroup | 0x02,

		All = 0x0FFFFFFF
	}



	public static class DbgLogger
	{
		public static DbgEvtType IncludeEvents = DbgEvtType.All;
		public static DbgEvtType DiscardEvents = DbgEvtType.IFaceFocus;

		static bool logevt (DbgEvtType evtType)
		{
			return (evtType & DiscardEvents) == 0 && (evtType & IncludeEvents) != 0;
		}

		/// <summary>
		/// debug events as recorded, another class is used in the viewer
		/// </summary>
		public class DbgEvent
		{
			public long begin, end;
			public DbgEvtType type;
			public object data = null;
			public int threadId;
			public List<DbgEvent> Events = new List<DbgEvent> ();

			public DbgEvent () { }

			public DbgEvent (long timeStamp, DbgEvtType evt, object _data = null)
			{
				data = _data;
				type = evt;
				begin = timeStamp;
				end = timeStamp;
				threadId = Thread.CurrentThread.ManagedThreadId;
			}

			public override string ToString ()
			{
				string tmp = $"{begin};{end};{threadId};{type}";
				if (type.HasFlag (DbgEvtType.GraphicObject)) {
					if (type.HasFlag (DbgEvtType.GOLayouting)) {
						LayoutingQueueItem lqi = (LayoutingQueueItem)data;
						tmp += $";{Widget.GraphicObjects.IndexOf (lqi.graphicObject).ToString ()};{lqi.LayoutType}";
						if (type == DbgEvtType.GOProcessLayouting)
							tmp += $";{lqi.result}";
					} else
						tmp += $";{Widget.GraphicObjects.IndexOf (data as Widget).ToString ()}";
				}
				return tmp;
			}
		}

		public static Stopwatch chrono = Stopwatch.StartNew();

		static List<DbgEvent> events = new List<DbgEvent>();
		static Dictionary<int, Stack<DbgEvent>> startedEvents = new Dictionary<int, Stack<DbgEvent>> ();

		static object logMutex = new object ();


		static List<DbgEvent> curEventList =>
			startedEvents.ContainsKey(Thread.CurrentThread.ManagedThreadId) ?
			startedEvents[Thread.CurrentThread.ManagedThreadId].Count == 0 ? events : startedEvents[Thread.CurrentThread.ManagedThreadId].Peek ().Events : events;

		public static DbgEvent StartEvent (DbgEvtType evtType, object data = null)
		{
			if (!logevt (evtType))
				return null;
			lock (logMutex) {
				chrono.Stop ();
				DbgEvent evt = new DbgEvent (chrono.ElapsedTicks, evtType, data);
				curEventList.Add (evt);
				if (!startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId))
					startedEvents [Thread.CurrentThread.ManagedThreadId] = new Stack<DbgEvent> ();
				startedEvents [Thread.CurrentThread.ManagedThreadId].Push (evt);
				chrono.Start ();
				return evt;
			}
		}
		public static DbgEvent EndEvent (DbgEvtType evtType, bool discardIfNoChildEvents = false)
		{
			if (!logevt (evtType))
				return null;

			lock (logMutex) {
				chrono.Stop ();
				if (!startedEvents.ContainsKey (Thread.CurrentThread.ManagedThreadId))
					throw new Exception ("Current thread has no event started");
				DbgEvent e = startedEvents [Thread.CurrentThread.ManagedThreadId].Pop ();
				if (e.type != evtType)
					throw new Exception ($"Begin/end event logging mismatch: {e.type}/{evtType}");
				if (discardIfNoChildEvents && e.Events.Count == 0)
					curEventList.Remove (e);
				else
					e.end = chrono.ElapsedTicks;
				chrono.Start ();
				return e;
			}
		}
		public static DbgEvent AddEvent (DbgEvtType evtType, object data = null) {
			if (!logevt (evtType))
				return null;

			lock (logMutex) {
				chrono.Stop ();
				DbgEvent evt = new DbgEvent (chrono.ElapsedTicks, evtType, data);
				curEventList.Add (evt);
				chrono.Start ();
				return evt;
			}
		}

		static int y, level;

		static void parseTree (Widget go) {
			if (go == null)
				return;

			go.yIndex = y++;
			go.xLevel = level++;

			Group gr = go as Group;
			if (gr != null) {
				foreach (Widget g in gr.Children) {
					parseTree (g);
				}
			} else {
				PrivateContainer pc = go as PrivateContainer;
				if (pc != null)
					parseTree (pc.getTemplateRoot);				
			}
			level--;		
		}
		public static void Reset ()
		{
			lock (logMutex) {
				startedEvents.Clear ();
				events.Clear ();
				chrono.Restart ();
			}
		}
		public static void save(Interface iface) {
			lock (logMutex) {
				y = 1;
				level = 0;

				foreach (Widget go in iface.GraphicTree)
					parseTree (go);

				using (StreamWriter s = new StreamWriter ("debug.log")) {
					s.WriteLine ("[GraphicObjects]");
					lock (Widget.GraphicObjects) {
						Widget.GraphicObjects = Widget.GraphicObjects.OrderBy (o => o.yIndex).ToList ();
						for (int i = 0; i < Widget.GraphicObjects.Count; i++) {
							Widget g = Widget.GraphicObjects [i];
							s.WriteLine ("{0};{1};{2};{3}", i, g.GetType ().Name, g.yIndex, g.xLevel);
						}
					}
					s.WriteLine ("[Events]");
					saveEventList (s, events);
				}
			}
		}

		static void saveEventList (StreamWriter s, List<DbgEvent> events, int level = 0)
		{
			foreach (DbgEvent e in events) {
				if (e == null)
					continue;
				s.WriteLine (new string ('\t', level) + e.ToString ());
				saveEventList (s, e.Events, level + 1);
			}
		}

	}
}
#endif


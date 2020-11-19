// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using Crow;
using Crow.Cairo;

namespace DebugLogAnalyzer
{
	public class Program : SampleBase
	{
		static void Main (string [] args)
		{
			using (Program app = new Program ()) 
				app.Run ();
		}

		List<DbgEvent> events = new List<DbgEvent>();
		List<DbgWidgetRecord> widgets = new List<DbgWidgetRecord>();
		DbgEvent curEvent = new DbgEvent();
		DbgWidgetRecord curWidget = new DbgWidgetRecord();

		public List<DbgEvent> Events {
			get => events;
			set {
				if (events == value)
					return;
				events = value;
				NotifyValueChanged (nameof (Events), events);
			}
		}
		public List<DbgWidgetRecord> Widgets {
			get => widgets;
			set {
				if (widgets == value)
					return;
				widgets = value;
				NotifyValueChanged (nameof (Widgets), widgets);
			}
		}
		public DbgEvent CurrentEvent {
			get => curEvent;
			set {
				if (curEvent == value)
					return;

				if (curEvent != null)
					curEvent.IsSelected = false;
				curEvent = value;
				if (curEvent != null) {
					curEvent.IsSelected = true;
					if (curEvent.parentEvent != null)
						curEvent.parentEvent.IsExpanded = true;
				}

				NotifyValueChanged (nameof (CurrentEvent), curEvent);
			}
		}
		public DbgWidgetRecord CurrentWidget {
			get => curWidget;
			set {
				if (curWidget == value)
					return;
				curWidget = value;
				NotifyValueChanged (nameof (CurrentWidget), curWidget);
				NotifyValueChanged ("CurWidgetRootEvents", CurWidgetRootEvents);
			}
		}
		public List<DbgWidgetEvent> CurWidgetRootEvents => curWidget == null? new List<DbgWidgetEvent>() : curWidget.RootEvents;

		Scroller dbgTreeViewScroller;

		protected override void OnInitialized ()
		{
			Load ("#Dbg.dbglog.crow").DataSource = this;

			TreeView tv = FindByName("dbgTV") as TreeView;
			dbgTreeViewScroller = tv.FindByNameInTemplate ("scroller1") as Scroller;

			loadDebugFile ("/var/tmp/debug.log");
		}

		void loadDebugFile (string logFile)
		{
			if (!File.Exists (logFile))
				return;

			List<DbgEvent> evts = new List<DbgEvent> ();
			List<DbgWidgetRecord> objs = new List<DbgWidgetRecord> ();

			using (StreamReader s = new StreamReader (logFile)) {
				if (s.ReadLine () != "[GraphicObjects]")
					return;
				while (!s.EndOfStream) {
					string l = s.ReadLine ();
					if (l == "[Events]")
						break;
					DbgWidgetRecord o = DbgWidgetRecord.Parse (l);
					objs.Add (o);
				}

				Stack<DbgEvent> startedEvents = new Stack<DbgEvent> ();

				if (!s.EndOfStream) {
					while (!s.EndOfStream) {
						int level = 0;
						while (s.Peek () == (int)'\t') {
							s.Read ();
							level++;
						}
						DbgEvent evt = DbgEvent.Parse (s.ReadLine ());							
						if (level == 0) {
							startedEvents.Clear ();
							evts.Add (evt);
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
						if (evt.type.HasFlag (DbgEvtType.Widget))
							objs [(evt as DbgWidgetEvent).InstanceIndex].Events.Add (evt);
					}
				}
			}
			Widgets = objs;
			Events = evts;
		}

		int targetTvScroll = -1;

		void onTvPainted (object sender, EventArgs e)
		{
			if (targetTvScroll < 0 || targetTvScroll > dbgTreeViewScroller.MaxScrollY + dbgTreeViewScroller.Slot.Height)
				return;
			dbgTreeViewScroller.MaxScrollY = targetTvScroll;
			targetTvScroll = -1;
		}

		void onSelectedItemContainerChanged (object sender, SelectionChangeEventArgs e)
		{
			TreeView tv = sender as TreeView;
			Group it = tv.FindByNameInTemplate ("ItemsContainer") as Group;

			ListItem li = e.NewValue as ListItem;
			Rectangle selRect = li.RelativeSlot (it);

			if (selRect.Y > dbgTreeViewScroller.ScrollY && selRect.Y < dbgTreeViewScroller.Slot.Height + dbgTreeViewScroller.ScrollY)
				return;

			Console.WriteLine ($"Scroll={dbgTreeViewScroller.ScrollY} selRectY={selRect.Y} MaxScrollY={dbgTreeViewScroller.MaxScrollY} ScrollerH={dbgTreeViewScroller.Slot.Height}");
			targetTvScroll = selRect.Y;
			if (selRect.Y > dbgTreeViewScroller.MaxScrollY + dbgTreeViewScroller.Slot.Height)
				targetTvScroll = selRect.Y;
			else {
				targetTvScroll = -1;
				dbgTreeViewScroller.ScrollY = selRect.Y;
			}
		}

	}
}


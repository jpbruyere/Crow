using System.Reflection.PortableExecutable;
// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using Glfw;
using Crow.Text;
using System.Collections.Generic;
using Encoding = System.Text.Encoding;
using Crow.DebugLogger;
using System.Linq;
using Samples;

namespace DebugLogAnalyzer
{
	public class Program : SampleBaseForEditor
	{
		
		static void Main (string [] args)
		{
			initDebugLog ();

			using (Program app = new Program ()) {
				CurrentProgramInstance = app;
				app.Run ();
			}
		}
		protected override void OnInitialized () {
			base.OnInitialized ();

			Load ("#Dbg.main.crow").DataSource = this;
			//crowContainer = FindByName ("CrowContainer") as Container;
			editor = FindByName ("tb") as TextBox;

			/*TreeView tv = FindByName("dbgTV") as TreeView;
			dbgTreeViewScroller = tv.FindByNameInTemplate ("scroller1") as Scroller;*/
			if (DebugLogOnStartup)
				DebugLogRecording = true;

			if (!File.Exists (CurrentFile))
				newFile ();
			//I set an empty object as datasource at this level to force update when new
			//widgets are added to the interface					

			reloadFromFile ();
		}

		public override void UpdateFrame()
		{
			base.UpdateFrame();

		}

			
		ObservableList<DbgEvent> events = new ObservableList<DbgEvent>();
		ObservableList<DbgWidgetRecord> widgets = new ObservableList<DbgWidgetRecord>();
		DbgEvent curEvent;
		bool disableCurrentEventHistory;
		Stack<DbgEvent> CurrentEventHistoryForward = new Stack<DbgEvent>();
		Stack<DbgEvent> CurrentEventHistoryBackward = new Stack<DbgEvent>();
		DbgWidgetRecord curWidget = new DbgWidgetRecord();
		bool debugLogRecording;
		int targetTvScroll = -1;

		public string[] AllEventTypes => Enum.GetNames (typeof(DbgEvtType));
		string searchEventType;
		DbgWidgetRecord searchWidget;
		public string SearchEventType {
			get => searchEventType;
			set {
				if (searchEventType == value)
					return;
				searchEventType = value;
				NotifyValueChanged (searchEventType);
			}
		}

		public DbgWidgetRecord SearchWidget {
			get => searchWidget;
			set {
				if (searchWidget == value)
					return;
				searchWidget = value;
				NotifyValueChanged (searchWidget);
			}
		}
		public Command CMDGotoParentEvent, CMDEventHistoryForward, CMDEventHistoryBackward;
		public CommandGroup EventCommands, DirectoryCommands;
		protected override void initCommands ()
		{
			base.initCommands ();

			CMDGotoParentEvent = new Command("parent", ()=> { CurrentEvent = CurrentEvent?.parentEvent; }, null, false);
			CMDEventHistoryBackward = new Command("back.", currentEventHistoryGoBack, null, false);
			CMDEventHistoryForward = new Command("forw.", currentEventHistoryGoForward, null, false);

			EventCommands = new CommandGroup(
				CMDGotoParentEvent, CMDEventHistoryBackward, CMDEventHistoryForward
			);
			DirectoryCommands = new CommandGroup(
				new Command("Set as root directory", ()=> { CurrentEvent = CurrentEvent?.parentEvent; })
			);

		}

		public string CrowDbgAssemblyLocation {
			get => Configuration.Global.Get<string>("CrowDbgAssemblyLocation");
			set {
				if (CrowDbgAssemblyLocation == value)
					return;
				Configuration.Global.Set ("CrowDbgAssemblyLocation", value);
				NotifyValueChanged(CrowDbgAssemblyLocation);
			}
		}
		public ObservableList<DbgEvent> Events {
			get => events;
			set {
				if (events == value)
					return;
				events = value;
				NotifyValueChanged (nameof (Events), events);
			}
		}
		public ObservableList<DbgWidgetRecord> Widgets {
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

				if (!disableCurrentEventHistory) {
					CurrentEventHistoryForward.Clear ();
					CMDEventHistoryForward.CanExecute = false;
					if (!(value == null || curEvent == null)) {
						CurrentEventHistoryBackward.Push (curEvent);
						CMDEventHistoryBackward.CanExecute = true;
					}
				}				
				
				curEvent = value;

				NotifyValueChanged (nameof (CurrentEvent), curEvent);
				NotifyValueChanged ("CurEventChildEvents", curEvent?.Events);
				if (CurrentEvent != null && CurrentEvent.parentEvent != null)
					CMDGotoParentEvent.CanExecute = true;
				else
					CMDGotoParentEvent.CanExecute = false;				
			}
		}
		void currentEventHistoryGoBack () {
			disableCurrentEventHistory = true;
			if (CurrentEvent != null) {
				CurrentEventHistoryForward.Push (CurrentEvent);
				CMDEventHistoryForward.CanExecute = true;
			}
			CurrentEvent = CurrentEventHistoryBackward.Pop ();
			CMDEventHistoryBackward.CanExecute = CurrentEventHistoryBackward.Count > 0;

			disableCurrentEventHistory = false;
		}

		void currentEventHistoryGoForward () {
			disableCurrentEventHistory = true;
			CurrentEventHistoryBackward.Push (CurrentEvent);
			CMDEventHistoryBackward.CanExecute = true;
			CurrentEvent = CurrentEventHistoryForward.Pop ();
			CMDEventHistoryForward.CanExecute = CurrentEventHistoryForward.Count > 0;

			disableCurrentEventHistory = false;
		}

		public DbgWidgetRecord CurrentWidget {
			get => curWidget;
			set {
				if (curWidget == value)
					return;
				curWidget = value;
				NotifyValueChanged (nameof (CurrentWidget), curWidget);
				NotifyValueChanged ("CurWidgetRootEvents", curWidget?.RootEvents);
				NotifyValueChanged ("CurrentWidgetEvents", curWidget?.Events);
			}
		}
		public List<DbgWidgetEvent> CurWidgetRootEvents => curWidget == null? new List<DbgWidgetEvent>() : curWidget.RootEvents;

		/*public string DebugLogFilePath {
			get => Configuration.Global.Get<string> (nameof (DebugLogFilePath));
			set {
				if (CurrentFile == value)
					return;
				Configuration.Global.Set (nameof (DebugLogFilePath), value);
				NotifyValueChanged (DebugLogFilePath);
			}
		}*/
		public bool DebugLogOnStartup {
			get => Configuration.Global.Get<bool> (nameof(DebugLogOnStartup));
			set {
				if (DebugLogOnStartup == value)
					return;
				Configuration.Global.Set (nameof(DebugLogOnStartup), value);
				NotifyValueChanged(DebugLogOnStartup);
			}
		}				

		
		Exception currentException;
		
		public Exception CurrentException {
			get => currentException;
			set {
				if (currentException == value)
					return;
				currentException = value;
				NotifyValueChanged ("ShowError", ShowError);
				NotifyValueChanged ("CurrentExceptionMSG", (object)CurrentExceptionMSG);
				NotifyValueChanged (currentException);
			}
		}
		public bool ShowError => currentException != null;
		public string CurrentExceptionMSG => currentException == null ? "" : currentException.Message;


        public override bool OnKeyDown (Key key) {

            switch (key) {
            case Key.F5:
                Load ("#Dbg.DebugLog.crow").DataSource = this;
                return true;
            /*case Key.F6:
				if (DebugLogRecording) {
					DbgLogger.IncludeEvents = DbgEvtType.None;
					DbgLogger.DiscardEvents = DbgEvtType.All;
					if (DebugLogToFile && !string.IsNullOrEmpty(DebugLogFilePath))
	                	DbgLogger.Save (this, DebugLogFilePath);
					DebugLogRecording = false;
 				} else {
					DbgLogger.Reset ();
					DbgLogger.IncludeEvents = RecordedEvents;
					DbgLogger.DiscardEvents = DiscardedEvents;
					DebugLogRecording = true;
				}
                return true;*/
            }
            return base.OnKeyDown (key);
        }
    }
}


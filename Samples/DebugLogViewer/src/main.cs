using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crow;
using Crow.DebugLogger;
using Drawing2D;
using Glfw;
using Samples;

namespace DebugLogViewer
{
	class DebugLogViewer : Interface {
		static void Main (string[] args) {
			using (DebugLogViewer app = new DebugLogViewer ())
				app.Run ();
		}

		public DebugLogViewer () : base (Configuration.Global.Get<int>("MainWinWidth", 800), Configuration.Global.Get<int>("MainWinHeight", 600), true) { }
		public override void ProcessResize(Rectangle bounds)
		{
			base.ProcessResize(bounds);
			Configuration.Global.Set ("MainWinWidth", clientRectangle.Width);
			Configuration.Global.Set ("MainWinHeight", clientRectangle.Height);
			//Console.WriteLine($"{clientRectangle.Width}x{clientRectangle.Height}");
		}
		protected override void OnInitialized () {
			base.OnInitialized ();

			initCommands ();

			SetWindowIcon ("#Crow.Icons.crow.png");

			if (ReopenLastLog)
				loadLog(CurrentLogFilePath);

			Load ("#ui.main.crow").DataSource = this;
		}

		#region Commands
		public Command CMDQuit, CMDHelp, CMDAbout, CMDOptions;
		public Command CMDStartRecording, CMDStopRecording, CMDRefresh, CMDAddEventToRecord, CMDRemoveEventToRecord;
		public Command CMDGotoParentEvent, CMDEventHistoryForward, CMDEventHistoryBackward;
		public CommandGroup LoggerCommands => new CommandGroup (CMDRefresh, CMDStartRecording, CMDStopRecording);
		public CommandGroup EventCommands => new CommandGroup(
				CMDGotoParentEvent, CMDEventHistoryBackward, CMDEventHistoryForward);
		public CommandGroup CommandsRoot, FileCommands;

		void initCommands ()
		{
			FileCommands = new CommandGroup ("File",
				new ActionCommand("Open Log...", openFileDialog, "#icons.outbox.svg"),
				new ActionCommand("Options", openOptionsDialog, "#icons.tools.svg"),
				new ActionCommand("Quit", base.Quit, "#icons.sign-out.svg")
			);
			

			CMDGotoParentEvent = new ActionCommand("parent", ()=> { CurrentEvent = CurrentEvent?.parentEvent; }, "#icons.level-up.svg", false);
			CMDEventHistoryBackward = new ActionCommand("back.", currentEventHistoryGoBack, "#icons.previous.svg", false);
			CMDEventHistoryForward = new ActionCommand("forw.", currentEventHistoryGoForward, "#icons.forward-arrow.svg", false);

			CMDHelp = new ActionCommand("Help", () => System.Diagnostics.Debug.WriteLine("help"), "#icons.question.svg");

			CommandsRoot = new CommandGroup (
				FileCommands,
				new CommandGroup ("Help", CMDHelp)
			);
		}
		void openOptionsDialog() =>	Load ("#ui.Options.crow").DataSource = this;
		void openFileDialog() =>
			LoadIMLFragment (
				@"<FileDialog Width='60%' Height='50%' Caption='Open Log File' AlwaysOnTop='true'
					CurrentDirectory='{²CurrentDir}'
					SelectedFile='{²CurrentLogFileName}'
					OkClicked='openFileDialog_OkClicked'/>").DataSource = this;

		void openFileDialog_OkClicked (object sender, EventArgs e)
		{
			loadLog (CurrentLogFilePath);
		}

		#endregion


		IList<DbgEvent> events;
		IList<DbgWidgetRecord> widgets;

		public string CurrentDir {
			get => Configuration.Global.Get<string>("CurrentDir", Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments));
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set ("CurrentDir", value);
				NotifyValueChanged (CurrentDir);
			}
		}
		public string CurrentLogFileName {
			get => Configuration.Global.Get<string> ("CurrentLogFileName");
			set {
				if (CurrentLogFileName == value)
					return;
				Configuration.Global.Set ("CurrentLogFileName", value);
				NotifyValueChanged (value);
			}
		}
		public string CurrentLogFilePath => Path.Combine(CurrentDir,CurrentLogFileName);
		public bool ReopenLastLog
		{
			get => Configuration.Global.Get<bool> ("ReopenLastLog", true); 
			set {
				if (ReopenLastLog == value)
					return;
				Configuration.Global.Set ("ReopenLastLog", value);
				NotifyValueChanged (value);
			}
		}
		
		public IList<DbgEvent> Events {
			get => events;
			set {
				if (events == value)
					return;
				events = value;
				NotifyValueChanged (nameof (Events), events);
			}
		}
		public IList<DbgWidgetRecord> Widgets {
			get => widgets;
			set {
				if (widgets == value)
					return;
				widgets = value;
				NotifyValueChanged (nameof (Widgets), widgets);
			}
		}

		void loadLog (string logFile) {
			if (!File.Exists(logFile))
				return;
			//lock(UpdateMutex) {
				using (Stream stream = new FileStream (logFile, FileMode.Open, FileAccess.Read)) {
					List<DbgWidgetRecord> widgets = new List<DbgWidgetRecord>();
					List<DbgEvent> events = new List<DbgEvent>();
					DbgLogger.Load (stream, events, widgets);

					for (int i = 0; i < widgets.Count; i++) {
						widgets[i].listIndex = i;
						//Widgets.Add	(widgets[i]);
					}
					for (int i = 0; i < events.Count; i++) {
						//Events.Add (events[i]);
						updateWidgetEvents (widgets, events[i]);
					}
				
					Events = events;
					Widgets = widgets;
				}
			//}
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

		DbgEvent curEvent;
		bool disableCurrentEventHistory;
		Stack<DbgEvent> CurrentEventHistoryForward = new Stack<DbgEvent>();
		Stack<DbgEvent> CurrentEventHistoryBackward = new Stack<DbgEvent>();
		DbgWidgetRecord curWidgetRecord = new DbgWidgetRecord();
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
				NotifyValueChanged ("CurWidgetProperties", CurWidgetProperties);

				if (CurrentEvent != null && CurrentEvent.parentEvent != null)
					CMDGotoParentEvent.CanExecute = true;
				else
					CMDGotoParentEvent.CanExecute = false;
			}
		}
		public DbgWidgetRecord CurrentWidget {
			get => curWidgetRecord;
			set {
				if (curWidgetRecord == value)
					return;
				curWidgetRecord = value;
				NotifyValueChanged ("CurrentWidget", curWidgetRecord);
				NotifyValueChanged ("CurWidgetRootEvents", curWidgetRecord?.RootEvents);
				NotifyValueChanged ("CurrentWidgetEvents", curWidgetRecord?.Events);
				NotifyValueChanged ("CurWidgetProperties", CurWidgetProperties);
			}
		}
		public List<DbgWidgetEvent> CurWidgetRootEvents => curWidgetRecord == null? new List<DbgWidgetEvent>() : curWidgetRecord.RootEvents;

		public IEnumerable<KeyValuePair<string, string>> CurWidgetProperties {
			get {
				if (curWidgetRecord == null)
					return null;
				long endTime = curEvent == null ? long.MaxValue : curEvent.end;
				Dictionary<string, string> result = new Dictionary<string, string> ();
				foreach (DbgWidgetEvent evt in curWidgetRecord?.Events?.Where (e => e.type == DbgEvtType.GOSetProperty && e.begin <= endTime)){
					string[] tmp = evt.Message.Split('=');
					if (result.ContainsKey (tmp[0]))
						result[tmp[0]] = tmp[1];
					else
						result.Add (tmp[0], tmp[1]);
				}
				return result;
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
	}
}

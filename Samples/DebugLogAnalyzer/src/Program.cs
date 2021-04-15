﻿using System.Reflection.PortableExecutable;
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

namespace DebugLogAnalyzer
{
	public class Program : SampleBase
	{
		public static Program CurrentProgramInstance;
		static void Main (string [] args)
		{
			DbgLogger.IncludeEvents = DbgEvtType.None;
			DbgLogger.DiscardEvents = DbgEvtType.All;

			using (Program app = new Program ()) {
				CurrentProgramInstance = app;
				app.Run ();
			}
		}
		protected override void OnInitialized () {
			initCommands ();

			base.OnInitialized ();

			if (string.IsNullOrEmpty (CurrentDir))
				CurrentDir = Path.Combine (Directory.GetCurrentDirectory (), "Interfaces");

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



		public Command CMDNew, CMDOpen, CMDSave, CMDSaveAs, CMDQuit, CMDShowLeftPane,
					CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDHelp, CMDAbout, CMDOptions,
					CMDGotoParentEvent, CMDEventHistoryForward, CMDEventHistoryBackward;
		public CommandGroup EventCommands, DirectoryCommands;
		public CommandGroup EditorCommands => new CommandGroup (CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDSave, CMDSaveAs);
		void initCommands ()
		{
			CMDNew	= new Command ("New", new Action (onNewFile), "#Icons.blank-file.svg");			
			CMDSave = new Command ("Save", new Action (onSave), "#Icons.save.svg", false);
			CMDSaveAs = new Command ("Save As...", new Action (onSaveAs), "#Icons.save.svg");
			CMDQuit = new Command ("Quit", new Action (() => base.Quit ()), "#Icons.exit.svg");
			CMDUndo = new Command ("Undo", new Action (undo),"#Icons.undo.svg", false);
			CMDRedo = new Command ("Redo", new Action (redo),"#Icons.redo.svg", false);
			CMDCut	= new Command ("Cut", new Action (() => cut ()), "#Icons.scissors.svg", false);
			CMDCopy = new Command ("Copy", new Action (() => copy ()), "#Icons.copy-file.svg", false);
			CMDPaste= new Command ("Paste", new Action (() => paste ()), "#Icons.paste-on-document.svg", false);

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
		/*IEnumerable<DbgWidgetEvent> widgetEvents (DbgWidgetRecord wr, DbgEvent evt) {
			if (evt is DbgWidgetEvent we && we.InstanceIndex == wr.InstanceIndex)
				yield return we;
			if (evt.Events != null) {
				foreach (DbgEvent e in evt.Events)
					foreach (DbgWidgetEvent ye in widgetEvents (wr, e))				
						yield return ye;
			}
		}
		IEnumerable<DbgWidgetEvent> currentWidgetEvents;

		public IEnumerable<DbgWidgetEvent> CurrentWidgetEvents {
			get => currentWidgetEvents;
			set {
				currentWidgetEvents = value;
				NotifyValueChanged (currentWidgetEvents);
				curWidget.Events = new List<DbgEvent> (currentWidgetEvents);
			}
		}
		IEnumerable<DbgWidgetEvent> getCurrentWidgetEvents () {
			if (CurrentWidget == null)
				yield return null;
			else {
				foreach (DbgEvent evt in Events)
					foreach (DbgWidgetEvent dwe in widgetEvents (CurrentWidget, evt))
						yield return dwe;
			}
		}*/

		 
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
		public DbgEvtType RecordedEvents {
			get => Configuration.Global.Get<DbgEvtType> (nameof (RecordedEvents));
			set {
				if (RecordedEvents == value)
					return;				
				Configuration.Global.Set (nameof (RecordedEvents), value);				
				if (DebugLogRecording)
					DbgLogger.IncludeEvents = RecordedEvents;
				NotifyValueChanged(RecordedEvents);
			}
		}
		public DbgEvtType DiscardedEvents {
			get => Configuration.Global.Get<DbgEvtType> (nameof (DiscardedEvents));
			set {
				if (DiscardedEvents == value)
					return;
				Configuration.Global.Set (nameof (DiscardedEvents), value);
				if (DebugLogRecording)
					DbgLogger.DiscardEvents = DiscardedEvents;
				NotifyValueChanged(DiscardedEvents);
			}
		}
		public bool DebugLoggingEnabled => DbgLogger.IsEnabled;
		public bool DebugLogToFile {
			get => Configuration.Global.Get<bool> (nameof(DebugLogToFile));
			set {
				if (DbgLogger.ConsoleOutput != value)
					return;				
				Configuration.Global.Set (nameof(DebugLogToFile), value);
				NotifyValueChanged(DebugLogToFile);
			}
		}
		public string DebugLogFilePath {
			get => Configuration.Global.Get<string> (nameof (DebugLogFilePath));
			set {
				if (CurrentFile == value)
					return;
				Configuration.Global.Set (nameof (DebugLogFilePath), value);
				NotifyValueChanged (DebugLogFilePath);
			}
		}
		public bool DebugLogRecording {
			get => debugLogRecording;
			set {
				if (debugLogRecording == value)
					return;
				debugLogRecording = value;
				NotifyValueChanged(debugLogRecording);
			}
		}
		public bool DebugLogOnStartup {
			get => Configuration.Global.Get<bool> (nameof(DebugLogOnStartup));
			set {
				if (DbgLogger.ConsoleOutput != value)
					return;				
				Configuration.Global.Set (nameof(DebugLogOnStartup), value);
				NotifyValueChanged(DebugLogOnStartup);
			}
		}				

		
		const string _defaultFileName = "unnamed.txt";
		string source = "", origSource;		
		TextBox editor;	
		Stack<TextChange> undoStack = new Stack<TextChange> ();
		Stack<TextChange> redoStack = new Stack<TextChange> ();
		TextSpan selection;
		Exception currentException;
		public string CurrentDir {
			get => Configuration.Global.Get<string> (nameof (CurrentDir));
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set (nameof (CurrentDir), value);
				NotifyValueChanged (CurrentDir);
			}
		}
		public string CurrentFile {
			get => Configuration.Global.Get<string> (nameof (CurrentFile));
			set {
				if (CurrentFile == value)
					return;
				Configuration.Global.Set (nameof (CurrentFile), value);
				NotifyValueChanged (CurrentFile);
			}
		}		
		public new bool IsDirty => source != origSource;
		public string Source {
			get => source;
			set {
				if (source == value)
					return;
				source = value;
				CMDSave.CanExecute = IsDirty;
				NotifyValueChanged (source);
				NotifyValueChanged ("IsDirty", IsDirty);
			}
		}
		string SelectedText =>	
				selection.IsEmpty ? "" : Source.AsSpan (selection.Start, selection.Length).ToString ();
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

	

		void undo () {
			if (undoStack.TryPop (out TextChange tch)) {
				redoStack.Push (tch.Inverse (source));
				CMDRedo.CanExecute = true;
				apply (tch);
				editor.SetCursorPosition (tch.End + tch.ChangedText.Length);
			}
			if (undoStack.Count == 0)
				CMDUndo.CanExecute = false;
		}
		void redo () {
			if (redoStack.TryPop (out TextChange tch)) {
				undoStack.Push (tch.Inverse (source));
				CMDUndo.CanExecute = true;
				apply (tch);
				editor.SetCursorPosition (tch.End + tch.ChangedText.Length);
			}
			if (redoStack.Count == 0)
				CMDRedo.CanExecute = false;
		}
		void cut () {
			copy ();
			applyChange (new TextChange (selection.Start, selection.Length, ""));
		}
		void copy () {
			Clipboard = SelectedText;
		}
		void paste () {			
			applyChange (new TextChange (selection.Start, selection.Length, Clipboard));
		}
		bool disableTextChangedEvent = false;
		void apply (TextChange change) {
			Span<char> tmp = stackalloc char[source.Length + (change.ChangedText.Length - change.Length)];
			ReadOnlySpan<char> src = source.AsSpan ();
			src.Slice (0, change.Start).CopyTo (tmp);
			if (!string.IsNullOrEmpty (change.ChangedText))
				change.ChangedText.AsSpan ().CopyTo (tmp.Slice (change.Start));
			src.Slice (change.End).CopyTo (tmp.Slice (change.Start + change.ChangedText.Length));
			disableTextChangedEvent = true;
			Source = tmp.ToString ();
			disableTextChangedEvent = false;			
		}	
		void applyChange (TextChange change) {
			undoStack.Push (change.Inverse (source));
			redoStack.Clear ();
			CMDUndo.CanExecute = true;
			CMDRedo.CanExecute = false;
			apply (change);
		}

		void resetUndoRedo () {
			undoStack.Clear ();
			redoStack.Clear ();
			CMDUndo.CanExecute = false;
			CMDRedo.CanExecute = false;			
		}		

		void onNewFile () {
			if (IsDirty) {				
				MessageBox.ShowModal (this, MessageBox.Type.YesNo, "Current file has unsaved changes, are you sure?").Yes += (sender, e) => newFile ();
			} else
				newFile ();
		}
		void onSave ()
		{
			if (!File.Exists (CurrentFile)) {
				onSaveAs ();
				return;
			}
			save ();
		}
		void onSaveAs ()
		{
			string dir = Path.GetDirectoryName (CurrentFile);
			if (string.IsNullOrEmpty (dir))
				dir = Directory.GetCurrentDirectory ();
			LoadIMLFragment (@"<FileDialog Width='60%' Height='50%' Caption='Save as ...' CurrentDirectory='" +
				dir + "' SelectedFile='" +
				Path.GetFileName(CurrentFile) + "' OkClicked='saveFileDialog_OkClicked'/>").DataSource = this;
		}
		void saveFileDialog_OkClicked (object sender, EventArgs e)
		{
			FileDialog fd = sender as FileDialog;

			if (string.IsNullOrEmpty (fd.SelectedFileFullPath))
				return;

			if (File.Exists(fd.SelectedFileFullPath)) {
				MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "File exists, overwrite?");
				mb.Yes += (sender2, e2) => {
					CurrentFile = fd.SelectedFileFullPath;
					save ();
				};
				return;
			}

			CurrentFile = fd.SelectedFileFullPath;
			save ();
		}

		void newFile()
		{
			disableTextChangedEvent = true;
			Source = @"<Label Text='Hello World' Background='MediumSeaGreen' Margin='10'/>";
			disableTextChangedEvent = false;
			resetUndoRedo ();
			if (!string.IsNullOrEmpty (CurrentFile))
				CurrentFile = Path.Combine (Path.GetDirectoryName (CurrentFile), "newfile.crow");
			else
				CurrentFile = Path.Combine (CurrentDir, "newfile.crow");
		}

		void save () {
			using (Stream s = new FileStream(CurrentFile, FileMode.Create)) {
				s.WriteByte (0xEF);
				s.WriteByte (0xBB);
				s.WriteByte (0xBF);
				byte [] buff = Encoding.UTF8.GetBytes (source);
				s.Write (buff, 0, buff.Length);
			}
			origSource = source;
			NotifyValueChanged ("IsDirty", IsDirty);
			CMDSave.CanExecute = false;
		}

		void reloadFromFile () {			
			disableTextChangedEvent = true;
			if (File.Exists (CurrentFile)) {
				using (Stream s = new FileStream (CurrentFile, FileMode.Open)) {
					using (StreamReader sr = new StreamReader (s))
						Source = origSource = sr.ReadToEnd ();
				}
			}
			disableTextChangedEvent = false;
			resetUndoRedo ();
		}		

		public void goUpDirClick (object sender, MouseButtonEventArgs e)
		{
			string root = Directory.GetDirectoryRoot (CurrentDir);
			if (CurrentDir == root)
				return;
			CurrentDir = Directory.GetParent (CurrentDir).FullName;
		}

		void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			FileSystemInfo fi = e.NewValue as FileSystemInfo;
			if (fi == null)
				return;
			if (fi is DirectoryInfo)
				return;

			if (IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "Current file has unsaved changes, are you sure?");
				mb.Yes += (mbsender, mbe) => { CurrentFile = fi.FullName; reloadFromFile (); };
				return;
			}

			CurrentFile = fi.FullName;
			reloadFromFile ();
		}
		void onTextChanged (object sender, TextChangeEventArgs e) {
			if (disableTextChangedEvent)
				return;
			applyChange (e.Change);
		}		
		
		void onSelectedTextChanged (object sender, EventArgs e) {			
			selection = (sender as Label).Selection;
			Console.WriteLine($"selection:{selection.Start} length:{selection.Length}");
			CMDCut.CanExecute = CMDCopy.CanExecute = !selection.IsEmpty;
		}
		void textView_KeyDown (object sender, Crow.KeyEventArgs e) {
			if (Ctrl) {
				if (e.Key == Glfw.Key.W) {
					if (Shift)
						CMDRedo.Execute ();
					else
						CMDUndo.Execute ();
				} else if (e.Key == Glfw.Key.S) {
					onSave ();
				}
			}
		}



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


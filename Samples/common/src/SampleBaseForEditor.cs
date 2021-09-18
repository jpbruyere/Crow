// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using System.Text;
using Crow.IML;
using System.Runtime.CompilerServices;
using Glfw;
using System.Diagnostics;
using Crow.Text;
using System.Collections.Generic;
using Encoding = System.Text.Encoding;

namespace Samples
{
	public class SampleBaseForEditor : SampleBase
	{
		public static SampleBaseForEditor CurrentProgramInstance;

		protected override void OnInitialized () {
			initCommands ();

			base.OnInitialized ();

			if (string.IsNullOrEmpty (CurrentDir))
				CurrentDir = Path.Combine (Directory.GetCurrentDirectory (), "Interfaces");

		}

		protected const string _defaultFileName = "unnamed.txt";
		protected string source = "", origSource;
		protected TextBox editor;
		bool debugLogRecording;


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
		public virtual string Source {
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
		public bool DebugLoggingEnabled => DbgLogger.IsEnabled;

		/*public DbgEvtType RecordedEvents {
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
		}*/
		public bool DebugLogRecording {
			get => debugLogRecording;
			set {
				if (debugLogRecording == value)
					return;
				debugLogRecording = value;
				NotifyValueChanged(debugLogRecording);
			}
		}
		public bool DebugLogToFile {
			get => Configuration.Global.Get<bool> (nameof(DebugLogToFile));
			set {
				if (DebugLogToFile == value)
					return;
				Configuration.Global.Set (nameof(DebugLogToFile), value);
				NotifyValueChanged(DebugLogToFile);
				DbgLogger.ConsoleOutput = !value;
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

		protected static void initDebugLog () {
			DbgLogger.ConsoleOutput = !Configuration.Global.Get<bool> (nameof (DebugLogToFile));
		}


		public new bool IsDirty => source != origSource;


		public ActionCommand CMDNew, CMDOpen, CMDSave, CMDSaveAs, CMDQuit, CMDShowLeftPane,
					CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDHelp, CMDAbout, CMDOptions;
		public CommandGroup EditorCommands => new CommandGroup (CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDSave, CMDSaveAs);
		protected virtual void initCommands ()
		{
			CMDNew	= new ActionCommand ("New", new Action (onNewFile), "#Icons.blank-file.svg");
			CMDSave = new ActionCommand ("Save", new Action (onSave), "#Icons.save.svg", false);
			CMDSaveAs = new ActionCommand ("Save As...", new Action (onSaveAs), "#Icons.save.svg");
			CMDQuit = new ActionCommand ("Quit", new Action (() => base.Quit ()), "#Icons.exit.svg");
			CMDUndo = new ActionCommand ("Undo", new Action (undo),"#Icons.undo.svg", false);
			CMDRedo = new ActionCommand ("Redo", new Action (redo),"#Icons.redo.svg", false);
			CMDCut	= new ActionCommand ("Cut", new Action (cut), "#Icons.scissors.svg", false);
			CMDCopy = new ActionCommand ("Copy", new Action (copy), "#Icons.copy-file.svg", false);
			CMDPaste= new ActionCommand ("Paste", new Action (paste), "#Icons.paste-on-document.svg", false);
		}


		protected Stack<TextChange> undoStack = new Stack<TextChange> ();
		protected Stack<TextChange> redoStack = new Stack<TextChange> ();
		protected TextSpan selection;
		protected string SelectedText =>
				selection.IsEmpty ? "" : Source.AsSpan (selection.Start, selection.Length).ToString ();

		protected void undo () {
			if (undoStack.TryPop (out TextChange tch)) {
				redoStack.Push (tch.Inverse (source));
				CMDRedo.CanExecute = true;
				apply (tch);
				editor.SetCursorPosition (tch.End + tch.ChangedText.Length);
			}
			if (undoStack.Count == 0)
				CMDUndo.CanExecute = false;
		}
		protected void redo () {
			if (redoStack.TryPop (out TextChange tch)) {
				undoStack.Push (tch.Inverse (source));
				CMDUndo.CanExecute = true;
				apply (tch);
				editor.SetCursorPosition (tch.End + tch.ChangedText.Length);
			}
			if (redoStack.Count == 0)
				CMDRedo.CanExecute = false;
		}
		protected void resetUndoRedo () {
			undoStack.Clear ();
			redoStack.Clear ();
			CMDUndo.CanExecute = false;
			CMDRedo.CanExecute = false;
		}

		protected void cut () {
			copy ();
			applyChange (new TextChange (selection.Start, selection.Length, ""));
		}
		protected void copy () {
			Clipboard = SelectedText;
		}
		protected void paste () {
			applyChange (new TextChange (selection.Start, selection.Length, Clipboard));
		}
		protected void onNewFile () {
			if (IsDirty) {
				MessageBox.ShowModal (this, MessageBox.Type.YesNo, "Current file has unsaved changes, are you sure?").Yes += (sender, e) => newFile ();
			} else
				newFile ();
		}
		protected void onSave ()
		{
			if (!File.Exists (CurrentFile)) {
				onSaveAs ();
				return;
			}
			save ();
		}
		protected void onSaveAs ()
		{
			string dir = Path.GetDirectoryName (CurrentFile);
			if (string.IsNullOrEmpty (dir))
				dir = Directory.GetCurrentDirectory ();
			LoadIMLFragment (@"<FileDialog Width='60%' Height='50%' Caption='Save as ...' CurrentDirectory='" +
				dir + "' SelectedFile='" +
				Path.GetFileName(CurrentFile) + "' OkClicked='saveFileDialog_OkClicked'/>").DataSource = this;
		}
		protected void saveFileDialog_OkClicked (object sender, EventArgs e)
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

		protected void newFile()
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

		protected void save () {
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

		protected void reloadFromFile () {
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
		protected bool disableTextChangedEvent = false;
		protected void apply (TextChange change) {
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
		protected void applyChange (TextChange change) {
			undoStack.Push (change.Inverse (source));
			redoStack.Clear ();
			CMDUndo.CanExecute = true;
			CMDRedo.CanExecute = false;
			apply (change);
		}
		public void goUpDirClick (object sender, MouseButtonEventArgs e)
		{
			if (string.IsNullOrEmpty (CurrentDir))
				return;
			string root = Directory.GetDirectoryRoot (CurrentDir);
			if (CurrentDir == root)
				return;
			CurrentDir = Directory.GetParent (CurrentDir).FullName;
		}

		protected void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
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
		protected void onTextChanged (object sender, TextChangeEventArgs e) {
			if (disableTextChangedEvent)
				return;
			applyChange (e.Change);
		}

		protected void onSelectedTextChanged (object sender, EventArgs e) {
			selection = (sender as Label).Selection;
			Console.WriteLine($"selection:{selection.Start} length:{selection.Length}");
			CMDCut.CanExecute = CMDCopy.CanExecute = !selection.IsEmpty;
		}
		protected void textView_KeyDown (object sender, Crow.KeyEventArgs e) {
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
	}
}
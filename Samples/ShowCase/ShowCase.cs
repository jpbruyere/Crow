// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
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

namespace ShowCase
{
	class Showcase : SampleBase
	{
		static void Main ()
		{
			DbgLogger.IncludeEvents = DbgEvtType.Widget;
			DbgLogger.DiscardEvents = DbgEvtType.Focus;

			Environment.SetEnvironmentVariable ("FONTCONFIG_PATH", @"C:\Users\Jean-Philippe\source\vcpkg\installed\x64-windows\tools\fontconfig\fonts");

			using (Showcase app = new Showcase ()) 
				app.Run ();
		}

		public Container crowContainer;

		public string CurrentDir {
			get { return Configuration.Global.Get<string> (nameof (CurrentDir)); }
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set (nameof (CurrentDir), value);
				NotifyValueChanged (CurrentDir);
			}
		}
		public string CurrentFile {
			get { return Configuration.Global.Get<string> (nameof (CurrentFile)); }
			set {
				if (CurrentFile == value)
					return;
				Configuration.Global.Set (nameof (CurrentFile), value);
				NotifyValueChanged (CurrentFile);
			}
		}
		
		public Command CMDNew, CMDOpen, CMDSave, CMDSaveAs, CMDQuit, CMDShowLeftPane,
					CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDHelp, CMDAbout, CMDOptions;

		const string _defaultFileName = "unnamed.txt";
		string source = "";
		int dirtyUndoLevel;
		TextBox editor;
		Stopwatch reloadChrono = Stopwatch.StartNew ();

		public new bool IsDirty { get { return undoStack.Count != dirtyUndoLevel; } }
		public string Source {
			get => source;
			set {
				if (source == value)
					return;
				source = value;
				if (!reloadChrono.IsRunning)
					reloadChrono.Restart ();				
				NotifyValueChanged (source);
			}
		}
		public CommandGroup EditorCommands => new CommandGroup (CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDSave, CMDSaveAs);

		Stack<TextChange> undoStack = new Stack<TextChange> ();
		Stack<TextChange> redoStack = new Stack<TextChange> ();

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
			NotifyValueChanged ("IsDirty", IsDirty);
		}	
		
		void initCommands ()
		{
			CMDNew = new Command (new Action (onNewFile)) { Caption = "New", Icon = "#Icons.blank-file.svg", CanExecute = true };			
			CMDSave = new Command (new Action (onSave)) { Caption = "Save", Icon = "#Icons.save.svg", CanExecute = false };
			CMDSaveAs = new Command (new Action (onSaveAs)) { Caption = "Save As...", Icon = "#Icons.save.svg", CanExecute = true };
			CMDQuit = new Command (new Action (() => base.Quit ())) { Caption = "Quit", Icon = "#Icons.exit.svg", CanExecute = true };
			CMDUndo = new Command (new Action (undo)) { Caption = "Undo", Icon = "#Icons.undo.svg", CanExecute = false };
			CMDRedo = new Command (new Action (redo)) { Caption = "Redo", Icon = "#Icons.redo.svg", CanExecute = false };
			CMDCut = new Command (new Action (() => Quit ())) { Caption = "Cut", Icon = "#Icons.scissors.svg", CanExecute = false };
			CMDCopy = new Command (new Action (() => Quit ())) { Caption = "Copy", Icon = "#Icons.copy-file.svg", CanExecute = false };
			CMDPaste = new Command (new Action (() => Quit ())) { Caption = "Paste", Icon = "#Icons.paste-on-document.svg", CanExecute = false };

		}
		void onNewFile () {
			if (IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "Current file has unsaved changes, are you sure?");
				mb.Yes += (sender, e) => newFile ();
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
			dirtyUndoLevel = undoStack.Count;
			NotifyValueChanged ("IsDirty", IsDirty);
		}

		void reloadFromFile () {
			hideError ();
			disableTextChangedEvent = true;
			if (File.Exists (CurrentFile)) {
				using (Stream s = new FileStream (CurrentFile, FileMode.Open)) {
					using (StreamReader sr = new StreamReader (s))
						Source = sr.ReadToEnd ();
				}
			}
			disableTextChangedEvent = false;
			resetUndoRedo ();
		}
		void reloadFromSource () {
			hideError ();
			Widget g = null;
			try {
				lock (UpdateMutex) {
					Instantiator inst = null;
					using (MemoryStream ms = new MemoryStream (Encoding.UTF8.GetBytes (source)))
						inst = new Instantiator (this, ms);
					g = inst.CreateInstance ();
					crowContainer.SetChild (g);
					g.DataSource = this;
				}
			} catch (InstantiatorException itorex) {
				//Console.WriteLine (itorex);
				showError (itorex.InnerException);
			} catch (Exception ex) {
				//Console.WriteLine (ex);
				showError (ex);
			}
		}

		void resetUndoRedo () {
			undoStack.Clear ();
			redoStack.Clear ();
			CMDUndo.CanExecute = false;
			CMDRedo.CanExecute = false;
			dirtyUndoLevel = 0;
		}
		void showError (Exception ex) {
			NotifyValueChanged ("ErrorMessage", (object)ex.Message);
			NotifyValueChanged ("ShowError", true);
		}
		void hideError () {
			NotifyValueChanged ("ShowError", false);
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
			undoStack.Push (e.Change.Inverse (source));
			redoStack.Clear ();
			CMDUndo.CanExecute = true;
			CMDRedo.CanExecute = false;
			apply (e.Change);
		}
		void textView_KeyDown (object sender, Crow.KeyEventArgs e) {
			if (Ctrl && e.Key == Glfw.Key.W) {
				if (Shift)
					CMDRedo.Execute ();
				else
					CMDUndo.Execute ();
			}
		}

		protected override void OnInitialized () {
			initCommands ();

			base.OnInitialized ();

			if (string.IsNullOrEmpty (CurrentDir))
				CurrentDir = Path.Combine (Directory.GetCurrentDirectory (), "Interfaces");

			Load ("#ShowCase.showcase.crow").DataSource = this;
			crowContainer = FindByName ("CrowContainer") as Container;
			editor = FindByName ("tb") as TextBox;

			if (!File.Exists (CurrentFile))
				newFile ();
			//I set an empty object as datasource at this level to force update when new
			//widgets are added to the interface
			crowContainer.DataSource = new object ();
			hideError ();

			reloadFromFile ();
		}

		public override void UpdateFrame () {
            base.UpdateFrame ();
			if (reloadChrono.ElapsedMilliseconds < 200)
				return;
			reloadFromSource ();
			reloadChrono.Reset ();
		}

    }
}
// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace Crow.Coding {
	/// <summary>
	/// represent a file node in the tree in a project
	/// </summary>
    public class ProjectFileNode : ProjectItemNode {		
		bool isOpened = false;
		DateTime accessTime;
		string source;
		string origSource;
		object selectedItem;
		int curLine, curColumn;

		internal ReaderWriterLockSlim srcEditMtx = new ReaderWriterLockSlim();

		/// <summary>
		/// dictionnary of boolean per editor, true if editor must reload content from project node, false if uptodate.
		/// </summary>
		public Dictionary<object, bool> RegisteredEditors = new Dictionary<object, bool>();
		List<String> undoStack = new List<string>();
		List<String> redoStack = new List<string>();

		public Command cmdSave, cmdSaveAs, cmdOpen, cmdClose, cmdUndo, cmdRedo;

		public ProjectFileNode ()
		{
			initCommands ();
		}

		public ProjectFileNode (ProjectItemNode pi)
			: base (pi.Project, pi.Item)
		{
			initCommands ();
		}

		void initCommands (){
			cmdSave = new Command (new Action (() => Save ()))
			{ Caption = "Save", Icon = CrowIDE.IcoSave, CanExecute = false };
			cmdSaveAs = new Command (new Action (() => SaveAs ()))
			{ Caption = "Save As ..", Icon = CrowIDE.IcoSave, CanExecute = false };
			cmdOpen = new Command (new Action (() => Open ())) 
			{ Caption = "Open", Icon = CrowIDE.IcoOpen, CanExecute = true };
			cmdClose = new Command (new Action (() => OnQueryClose (this,null))) 
			{ Caption = "Close", Icon = CrowIDE.IcoQuit, CanExecute = false };
			cmdUndo = new Command (new Action (() => Undo (null))) 
			{ Caption = "Undo", Icon = CrowIDE.IcoUndo, CanExecute = false };
			cmdRedo = new Command (new Action (() => Redo (null))) 
			{ Caption = "Redo", Icon = CrowIDE.IcoRedo, CanExecute = false };

			Commands.Insert (0, cmdOpen);
			Commands.Insert (1, cmdSave);
			Commands.Insert (2, cmdSaveAs);
			Commands.Insert (3, cmdUndo);
			Commands.Insert (4, cmdRedo);
			Commands.Add (cmdClose);
		}

		public string LogicalName =>
			Item.HasMetadata ("LogicalName") ? Item.GetMetadata ("LogicalName").EvaluatedValue :
				$"{Project.DisplayName}.{Item.EvaluatedInclude.Replace('/','.').Replace('\\','.')}";

		public string Link => Item.GetMetadata ("Link")?.EvaluatedValue;
		public CopyToOutputState CopyToOutputDirectory {
			get => Item.HasMetadata ("CopyToOutputDirectory") ?
				Enum.TryParse (Item.GetMetadata ("CopyToOutputDirectory").EvaluatedValue, true, out CopyToOutputState tmp) ?
					tmp : CopyToOutputState.Never : CopyToOutputState.Never;
			set {
				if (string.Equals(Item.GetMetadata ("CopyToOutputDirectory")?.EvaluatedValue, value.ToString(), StringComparison.OrdinalIgnoreCase))
					return;
				//TODO: check if updated in xml and writable to disk with preserve formating
				Item.SetMetadataValue ("CopyToOutputDirectory", value.ToString ());
				NotifyValueChanged ("CopyToOutputDirectory", CopyToOutputDirectory);
			}
		}

		public bool IsOpened {
			get { return isOpened; }
			set {
				if (isOpened == value)
					return;
				isOpened = value;

				cmdOpen.CanExecute = !isOpened;
				cmdClose.CanExecute = isOpened;
				cmdSave.CanExecute = isOpened && IsDirty;

				if (isOpened) 
					Project.solution.OpenItem (this);
				else
					Project.solution.CloseItem (this);				

				NotifyValueChanged ("IsOpened", isOpened);
			}
		}

		public void UnregisterEditor (object editor){
			lock(RegisteredEditors){
				RegisteredEditors.Remove (editor);
			}
		}
		public void RegisterEditor (object editor) {
			lock(RegisteredEditors){
				RegisteredEditors.Add (editor, false);
			}
		}
		public virtual void UpdateSource (object sender, string newSrc){
			System.Diagnostics.Debug.WriteLine ("update source by {0}", sender);
			Source = newSrc;
			signalOtherRegisteredEditors (sender);
		}
		public void SignalEditorOfType<T> (){
			lock (RegisteredEditors) {
				object[] keys = RegisteredEditors.Keys.ToArray ();
				foreach (object ed in keys) {
					T editor = (T)ed;
					if (editor == null)
						continue;
					RegisteredEditors [editor] = false;
					break;
				}
			}
		}
		protected void signalOtherRegisteredEditors (object sender) {
			lock (RegisteredEditors) {
				object[] keys = RegisteredEditors.Keys.ToArray ();
				foreach (object editor in keys) {
					if (editor != sender)
						RegisteredEditors [editor] = false;
				}
			}
		}
		public string Source {
			get {
				if (!IsOpened) {
					using (StreamReader sr = new StreamReader (FullPath))
						source = sr.ReadToEnd ();
					
				} else {
					if (DateTime.Compare (
						    accessTime,
						    System.IO.File.GetLastWriteTime (FullPath)) < 0)
						Console.WriteLine ("File has been modified outside CrowIDE");
				}
				return source;
			}
			set {
				if (source == value)
					return;
				
				srcEditMtx.EnterWriteLock ();

				undoStack.Add (source);
				cmdUndo.CanExecute = true;
				redoStack.Clear ();
				cmdRedo.CanExecute = false;
				source = value;

				NotifyValueChanged ("Source", source);
				NotifyValueChanged ("IsDirty", IsDirty);

				cmdSave.CanExecute = cmdSaveAs.CanExecute = IsDirty;

				srcEditMtx.ExitWriteLock ();
			}
		}
		public bool IsDirty {
			get { return source != origSource; }
		}
		public int CurrentColumn{
			get { return curColumn; }
			set {
				if (curColumn == value)
					return;
				curColumn = value;
				NotifyValueChanged ("CurrentColumn", curColumn);
			}
		}
		public int CurrentLine{
			get { return curLine; }
			set {
				if (curLine == value)
					return;
				curLine = value;
				NotifyValueChanged ("CurrentLine", curLine);
			}
		}

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem == value)
					return;
				selectedItem= value;
				Project.solution.SelectedItemElement = value;
				NotifyValueChanged ("SelectedItem", selectedItem);
			}
		}

		public virtual void Open () {			
			accessTime = File.GetLastWriteTime (FullPath);
			using (StreamReader sr = new StreamReader (FullPath)) {
				source = sr.ReadToEnd ();
			}
			origSource = source;
			IsOpened = true;
			NotifyValueChanged ("IsDirty", false);
		}
		public virtual void Save () {
			if (!IsDirty)
				return;
			using (StreamWriter sw = new StreamWriter (FullPath)) {
				sw.Write (source);
			}
			origSource = source;
			NotifyValueChanged ("IsDirty", false);
		}
		public virtual void SaveAs () {
			if (!IsDirty)
				return;
			using (StreamWriter sw = new StreamWriter (FullPath)) {
				sw.Write (source);
			}
			origSource = source;
			NotifyValueChanged ("IsDirty", false);
		}
		public void Close () {
			origSource = source = null;
			IsOpened = false;
		}
		public void Undo(object sender){
			undo();
			signalOtherRegisteredEditors (sender);
		}
		public void Redo(object sender){
			redo();
			signalOtherRegisteredEditors (sender);
		}

		void undo () {			
			if (undoStack.Count == 0)
				return;
			
			srcEditMtx.EnterWriteLock ();
			string step = undoStack [undoStack.Count -1];
			redoStack.Add (source);
			cmdRedo.CanExecute = true;
			undoStack.RemoveAt(undoStack.Count -1);

			source = step;

			NotifyValueChanged ("Source", source);
			NotifyValueChanged ("IsDirty", IsDirty);
			cmdSave.CanExecute = IsDirty;

			if (undoStack.Count == 0)
				cmdUndo.CanExecute = false;
			srcEditMtx.ExitWriteLock ();
		}

		void redo () {
			if (redoStack.Count == 0)
				return;
			srcEditMtx.EnterWriteLock ();
			string step = redoStack [redoStack.Count -1];
			undoStack.Add (source);
			cmdUndo.CanExecute = true;
			redoStack.RemoveAt(redoStack.Count -1);
			source = step;
			NotifyValueChanged ("Source", source);
			NotifyValueChanged ("IsDirty", IsDirty);
			cmdSave.CanExecute = IsDirty;

			if (redoStack.Count == 0)
				cmdRedo.CanExecute = false;
			srcEditMtx.ExitWriteLock ();

		}

		public void onDoubleClick (object sender, MouseButtonEventArgs e){
			if (IsOpened)
				return;
			Open ();
		}

		/*public void onClick (object sender, MouseButtonEventArgs e){
			IsSelected = true;
		}*/
		public void OnQueryClose (object sender, EventArgs e){
			if (IsDirty) {
				MessageBox mb = MessageBox.ShowModal (Project.solution.IDE,
					                MessageBox.Type.YesNoCancel, $"{DisplayName} has unsaved changes.\nSave it now?");
				mb.Yes += (object _sender, EventArgs _e) => { Save (); Close (); };
				mb.No += (object _sender, EventArgs _e) => Close();
			} else
				Close ();
		}
	}
}


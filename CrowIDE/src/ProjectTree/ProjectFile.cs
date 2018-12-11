//
// ProjectNodes.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;
using Crow;
using System.Threading;

namespace Crow.Coding
{	
	public class ProjectFile : ProjectItem {		
		bool isOpened = false;
		DateTime accessTime;
		string source;
		string origSource;
		object selectedItem;
		int curLine, curColumn;

		internal ReaderWriterLockSlim srcEditMtx = new ReaderWriterLockSlim();

		public Dictionary<object, bool> RegisteredEditors = new Dictionary<object, bool>();
		List<String> undoStack = new List<string>();
		List<String> redoStack = new List<string>();

		public Crow.Command cmdSave, cmdSaveAs, cmdOpen, cmdClose, cmdUndo, cmdRedo;

		void initCommands (){
			cmdSave = new Crow.Command (new Action (() => Save ()))
			{ Caption = "Save", Icon = new SvgPicture ("#icons.save.svg"), CanExecute = false };
			cmdSaveAs = new Crow.Command (new Action (() => SaveAs ()))
			{ Caption = "Save As ..", Icon = new SvgPicture ("#icons.save.svg"), CanExecute = false };
			cmdOpen = new Crow.Command (new Action (() => Open ())) 
			{ Caption = "Open", Icon = new SvgPicture ("#icons.open.svg"), CanExecute = true };
			cmdClose = new Crow.Command (new Action (() => OnQueryClose (this,null))) 
			{ Caption = "Close", Icon = new SvgPicture ("#icons.open.svg"), CanExecute = false };
			cmdUndo = new Crow.Command (new Action (() => Undo (null))) 
			{ Caption = "Undo", Icon = new SvgPicture ("#icons.undo.svg"), CanExecute = false };
			cmdRedo = new Crow.Command (new Action (() => Redo (null))) 
			{ Caption = "Redo", Icon = new SvgPicture ("#icons.redo.svg"), CanExecute = false };

			Commands.Insert (0, cmdOpen);
			Commands.Insert (1, cmdSave);
			Commands.Insert (2, cmdSaveAs);
			Commands.Insert (3, cmdUndo);
			Commands.Insert (4, cmdRedo);
			Commands.Add (cmdClose);
		}
		public ProjectFile () {
			initCommands();
		}
		public ProjectFile (ProjectItem pi)
			: base (pi.Project, pi.node) {
			initCommands ();
		}

		public string ResourceID {
			get {				
				return Type != ItemType.EmbeddedResource ? null :
					node.SelectSingleNode ("LogicalName") == null ?
					Project.Name + "." + Path.Replace ('/', '.') :
					LogicalName;
			}
		}
		public string LogicalName {
			get {
				return node.SelectSingleNode ("LogicalName")?.InnerText;
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
					using (StreamReader sr = new StreamReader (AbsolutePath))
						source = sr.ReadToEnd ();
					
				} else {
					if (DateTime.Compare (
						    accessTime,
						    System.IO.File.GetLastWriteTime (AbsolutePath)) < 0)
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

		public CopyToOutputState CopyToOutputDirectory {
			get {
				XmlNode xn = node.SelectSingleNode ("CopyToOutputDirectory");
				return xn == null ? CopyToOutputState.Never :
					(CopyToOutputState)Enum.Parse (typeof(CopyToOutputState), xn.InnerText, true);
			}
		}

		public void Open () {			
			accessTime = System.IO.File.GetLastWriteTime (AbsolutePath);
			using (StreamReader sr = new StreamReader (AbsolutePath)) {
				source = sr.ReadToEnd ();
			}
			origSource = source;
			IsOpened = true;
			NotifyValueChanged ("IsDirty", false);
		}
		public virtual void Save () {
			if (!IsDirty)
				return;
			using (StreamWriter sw = new StreamWriter (AbsolutePath)) {
				sw.Write (source);
			}
			origSource = source;
			NotifyValueChanged ("IsDirty", false);
		}
		public virtual void SaveAs () {
			if (!IsDirty)
				return;
			using (StreamWriter sw = new StreamWriter (AbsolutePath)) {
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

		public void onClick (object sender, MouseButtonEventArgs e){
			IsSelected = true;
		}
		public void OnQueryClose (object sender, EventArgs e){
			if (IsDirty) {
				MessageBox mb = MessageBox.ShowModal (CrowIDE.MainIFace,
					                MessageBox.Type.YesNoCancel, $"{DisplayName} has unsaved changes.\nSave it now?");
				mb.Yes += onClickSaveAndCloseNow;
				mb.No += onClickCloseNow;
			} else
				Close ();
		}

		void onClickCloseNow (object sender, EventArgs e)
		{
			Close ();
		}
		void onClickSaveAndCloseNow (object sender, EventArgs e)
		{
			Save ();
			Close ();
		}
	}
}


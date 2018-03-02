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
	public enum ItemType {
		ReferenceGroup,
		Reference,
		ProjectReference,
		VirtualGroup,
		Folder,
		None,
		Compile,
		EmbeddedResource,
	}
	public enum CopyToOutputState {
		Never,
		Always,
		PreserveNewest
	}
	public class ProjectNode  : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		#region CTOR
		public ProjectNode (Project project, ItemType _type, string _name) : this(project){			
			type = _type;
			name = _name;
			initCommands ();
		}
		public ProjectNode (Project project){
			Project = project;
			initCommands ();
		}
		#endregion

		void initCommands () {
			Commands = new List<Crow.Command> ();
		}

		ItemType type;
		string name;
		List<ProjectNode> childNodes = new List<ProjectNode>();

		public Project Project;
		public List<Crow.Command> Commands;//list of command available for that node

		public virtual ItemType Type {
			get { return type; }
		}
		public virtual string DisplayName {
			get { return name; }
		}
		public List<ProjectNode> ChildNodes {
			get { return childNodes;	}
		}

		public void SortChilds () {
			foreach (ProjectNode pn in childNodes)
				pn.SortChilds ();			
			childNodes = childNodes.OrderBy(c=>c.Type).ThenBy(cn=>cn.DisplayName).ToList();
		}
		public override string ToString ()
		{
			return DisplayName;
		}
	}
	public class ProjectItem : ProjectNode {
		#region CTOR
		public ProjectItem (Project project, XmlNode _node) : base (project){
			node = _node;
		}
		#endregion

		public XmlNode node;

		public string Extension {
			get { return System.IO.Path.GetExtension (Path); }
		}
		public string Path {
			get {
				return node.Attributes["Include"]?.Value.Replace('\\','/');
			}
		}
		public string AbsolutePath {
			get {
				return System.IO.Path.Combine (Project.RootDir, Path);
			}
		}
		public override ItemType Type {
			get { 
				return (ItemType)Enum.Parse (typeof(ItemType), node.Name, true);
			}
		}
		public override string DisplayName {
			get { 
				return Type == ItemType.Reference ?
					Path :
					Path.Split ('/').LastOrDefault();
			}
		}
		public string HintPath {
			get { return node.SelectSingleNode ("HintPath")?.InnerText; }
		}
	}
	public class ProjectReference : ProjectItem {
		public ProjectReference (ProjectItem pi) : base (pi.Project, pi.node){
		}
		public string ProjectGUID {
			get {
				return node.SelectSingleNode ("Project")?.InnerText;
			}
		}
		public override string DisplayName {
			get {
				return node.SelectSingleNode ("Name").InnerText;
			}
		}
	}

	public class ProjectFile : ProjectItem {		
		protected bool isOpened = false;
		DateTime accessTime;
		string source;
		string origSource;
		object selectedItem;
		int curLine, curColumn;

		internal ReaderWriterLockSlim srcEditMtx = new ReaderWriterLockSlim();

		public Dictionary<object, bool> RegisteredEditors = new Dictionary<object, bool>();

		Crow.Command cmdSave, cmdOpen;

		public ProjectFile (ProjectItem pi)
			: base (pi.Project, pi.node) {

			cmdSave = new Crow.Command (new Action (() => Save ()))
				{ Caption = "Save", Icon = new SvgPicture ("#Crow.Coding.ui.icons.inbox.svg"), CanExecute = true };
			cmdOpen = new Crow.Command (new Action (() => Open ())) 
				{ Caption = "Open", Icon = new SvgPicture ("#Crow.Coding.ui.icons.outbox.svg"), CanExecute = false };

			Commands.Insert (0, cmdOpen);
			Commands.Insert (1, cmdSave);
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
		public void UpdateSource (object sender, string newSrc){
			System.Diagnostics.Debug.WriteLine ("update source by {0}", sender);
			Source = newSrc;
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
				if (!isOpened)
					Open ();
				else {
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

				source = value;
				NotifyValueChanged ("Source", source);
				NotifyValueChanged ("IsDirty", IsDirty);

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
			isOpened = true;
			origSource = source;
			NotifyValueChanged ("IsDirty", false);
		}
		public void Save () {
			using (StreamWriter sw = new StreamWriter (AbsolutePath)) {
				sw.Write (source);
			}
			origSource = source;
			NotifyValueChanged ("IsDirty", false);
		}
		public void Close () {
			origSource = null;
			isOpened = false;
			Project.solution.CloseItem (this);
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
	public class ImlProjectItem : ProjectFile
	{
		#region CTOR
		public ImlProjectItem (ProjectItem pi) : base (pi){			
		}
		#endregion

		GraphicObject instance;

		/// <summary>
		/// instance created with an instantiator from the source by a DesignInterface,
		/// for now, the one in ImlVisualEditor
		/// </summary>
		public GraphicObject Instance {
			get { return instance; }
			set {
				if (instance == value)
					return;
				instance = value;
				NotifyValueChanged ("Instance", instance);
			}
		}
	}
}


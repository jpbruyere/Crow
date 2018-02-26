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
		}
		public ProjectNode (Project project){
			Project = project;
		}
		#endregion

		ItemType type;
		string name;
		List<ProjectNode> childNodes = new List<ProjectNode>();

		public Project Project;

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
		bool isOpened = false;
		DateTime accessTime;
		string source;
		string origSource;
		object selectedItem;

		public List<Crow.Command> Commands;

		public ProjectFile (ProjectItem pi) : base (pi.Project, pi.node){
			Commands = new List<Crow.Command> (new Crow.Command[] {
				new Crow.Command(new Action(() => Open())) 
				{ Caption = "Open", Icon = new SvgPicture("#Crow.Coding.ui.icons.outbox.svg"), CanExecute = false},
				new Crow.Command(new Action(() => Save()))
				{ Caption = "Save", Icon = new SvgPicture("#Crow.Coding.ui.icons.inbox.svg"), CanExecute = false},
			});
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
				source = value;
				NotifyValueChanged ("Source", source);
				NotifyValueChanged ("IsDirty", IsDirty);
			}
		}
		public bool IsDirty {
			get { return source != origSource; }
		}
		int curLine, curColumn;
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
//		public bool IsSelected {
//			get { return isSelected; }
//			set { 
//				if (isSelected == value)
//					return;
//				isSelected = value;
//				NotifyValueChanged ("IsSelected", isSelected);				
//			}
//		}

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
//				if (xn == null)
//					return CopyToOutputState.Never;
//				CopyToOutputState tmp = (CopyToOutputState)Enum.Parse (typeof(CopyToOutputState), xn.InnerText, true);
//				return tmp;
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

		public void OnQueryClose (object sender, EventArgs e){
			Project.solution.CloseItem (this);
		}
	}
	public class ImlProjectItem : ProjectFile
	{
		#region CTOR
		public ImlProjectItem (ProjectItem pi) : base (pi){			
		}
		#endregion

	}
}


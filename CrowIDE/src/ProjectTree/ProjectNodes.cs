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
		public ProjectNode (){
			initCommands ();
		}
		#endregion

		void initCommands () {
			Commands = new List<Crow.Command> ();
		}

		ProjectNode parent;
		bool isExpanded;
		protected bool isSelected;
		ItemType type;
		string name;
		List<ProjectNode> childNodes = new List<ProjectNode>();

		public Project Project;
		public List<Crow.Command> Commands;//list of command available for that node

		public virtual Crow.Picture Icon {
			get {
				switch (Type) {
				case ItemType.Reference:
					return new SvgPicture ("#Crow.Icons.assembly.svg");
				case ItemType.ProjectReference:
					return new SvgPicture("#Crow.Icons.projectRef.svg"); 
				default:
					return new SvgPicture("#icons.blank-file.svg"); 
				}
			}
		}
		public ProjectNode Parent {
			get { return parent; }
			set { parent = value; }
		}
		public virtual ItemType Type {
			get { return type; }
		}
		public virtual string DisplayName {
			get { return name; }
		}
		public List<ProjectNode> ChildNodes {
			get { return childNodes;	}
		}
		public void AddChild (ProjectNode pn) {
			childNodes.Add(pn);
			pn.Parent = this;
		}
		public void RemoveChild (ProjectNode pn){
			pn.Parent = null;
			childNodes.Remove (pn);
		}
		public void SortChilds () {
			foreach (ProjectNode pn in childNodes)
				pn.SortChilds ();			
			childNodes = childNodes.OrderBy(c=>c.Type).ThenBy(cn=>cn.DisplayName).ToList();
		}

		public bool IsExpanded
		{
			get { return isExpanded; }
			set
			{
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged ("IsExpanded", isExpanded);
			}
		}
		public virtual bool IsSelected
		{
			get { return isSelected; }
			set
			{
				if (value == isSelected)
					return;
				
				isSelected = value;

				NotifyValueChanged ("IsSelected", isSelected);
			}
		}
		public override string ToString ()
		{
			return DisplayName;
		}
	}
}


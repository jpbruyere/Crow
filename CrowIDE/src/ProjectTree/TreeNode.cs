// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crow.Coding
{
	public class TreeNode : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		#endregion

		public TreeNode () { }
		public TreeNode (string _name)
		{
			name = _name;
		}

		ObservableList<TreeNode> childs = new ObservableList<TreeNode> ();

		protected string name;
		protected bool isSelected, isExpanded;

		public TreeNode Parent;

		public virtual string DisplayName {
			get { return name; }
		}
		public ObservableList<TreeNode> Childs {
			get => childs;
			set {
				if (childs == value)
					return;
				childs = value;
				NotifyValueChanged ("Childs", childs);
			}
		}
		ObservableList<Crow.Command> commands = new ObservableList<Command> ();
		public ObservableList<Command> Commands {
			get => commands;
			set {
				if (commands == value)
					return;
				commands = value;
				NotifyValueChanged ("Command", commands);
			}
		}

		public void AddChild (TreeNode pn)
		{
			childs.Add (pn);
			pn.Parent = this;
		}
		public void RemoveChild (TreeNode pn)
		{
			pn.Parent = null;
			childs.Remove (pn);
		}

		public virtual bool IsExpanded {
			get { return isExpanded; }
			set {
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged ("IsExpanded", isExpanded);
			}
		}
		public virtual bool IsSelected {
			get { return isSelected; }
			set {
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

		public IEnumerable<TreeNode> Flatten {
			get {
				yield return this;
				foreach (var node in childs.SelectMany (child => child.Flatten))
					yield return node;
			}
		}

		public virtual void SortChilds ()
		{
			foreach (TreeNode pn in Childs)
				pn.SortChilds ();
			Childs = new ObservableList<TreeNode> (Childs.OrderBy (c => c, new NodeComparer()));
		}

		public class NodeComparer : IComparer<TreeNode>
		{
			public int Compare (TreeNode x, TreeNode y)
			{
				ProjectNode nX = x as ProjectNode;
				ProjectNode nY = y as ProjectNode;
				if (x == null) 
					return (y == null) ? string.Compare (x.DisplayName, y.DisplayName) : 1;
				if (y == null)
					return -1;
				int typeCompare = nX.Type.CompareTo (nY.Type);
				return typeCompare != 0 ? typeCompare : string.Compare (x.DisplayName, y.DisplayName);
			}
		}
	}


}

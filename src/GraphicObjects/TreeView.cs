//
// TreeView.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
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
using System.Xml.Serialization;
using System.Diagnostics;
using System.ComponentModel;

namespace Crow
{
	//treeview expect expandable child (or not)
	//if their are expandable, some functions and events are added
	public class TreeView : TemplatedGroup
	{
		GraphicObject selectedItemContainer = null;
		bool isRoot;

		#region CTOR
		protected TreeView() : base(){}
		public TreeView (Interface iface) : base(iface)
		{
		}
		#endregion

		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool IsRoot {
			get { return isRoot; }
			set {
				if (isRoot == value)
					return;
				isRoot = value;
				NotifyValueChanged ("IsRoot", isRoot);
			}
		}
		[XmlIgnore]public override object SelectedItem {
			get {
				return selectedItemContainer == null ?
					"" : selectedItemContainer.DataSource;
			}
		}

		protected override void registerItemClick (GraphicObject g)
		{
			//register ItemClick on the Root node
			TreeView tv = this as TreeView;
			while (!tv.IsRoot) {
				ILayoutable tmp = tv.Parent;
				while (!(tmp is TreeView)) {
					tmp = tmp.Parent;
				}
				tv = tmp as TreeView;
			}
			g.MouseClick += tv.itemClick;
		}
		internal override void itemClick (object sender, MouseButtonEventArgs e)
		{
			GraphicObject tmp = sender as GraphicObject;
			if (!tmp.HasFocus)
				return;
			if (selectedItemContainer != null) {
				selectedItemContainer.Foreground = Color.Transparent;
				selectedItemContainer.Background = Color.Transparent;
			}
			selectedItemContainer = tmp;
			selectedItemContainer.Foreground = SelectionForeground;
			selectedItemContainer.Background = SelectionBackground;
			NotifyValueChanged ("SelectedItem", SelectedItem);
			raiseSelectedItemChanged ();
		}

		void onExpandAll_MouseClick (object sender, MouseButtonEventArgs e)
		{
			ExpandAll ();
		}

		public void ExpandAll(){
			foreach (Group grp in items.Children) {
				foreach (GraphicObject go in grp.Children) {
					Expandable exp = go as Expandable;
					if (exp == null)
						continue;
					TreeView subTV = exp.FindByName ("List") as TreeView;
					if (subTV == null)
						continue;
					EventHandler handler = null;
					handler = delegate(object sender, EventArgs e) {
						TreeView tv = sender as TreeView;
						tv.Loaded -= handler;
						tv.ExpandAll ();
					};
					subTV.Loaded += handler;
					exp.IsExpanded = true;
				}
			}
		}
	}
}


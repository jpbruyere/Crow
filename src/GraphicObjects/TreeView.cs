//
//  TreeView.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
		public TreeView () : base()
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


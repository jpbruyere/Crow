// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow
{
	//treeview expect expandable child (or not)
	//if their are expandable, some functions and events are added
	public class TreeView : TemplatedGroup
	{
		bool isRoot;

		#region CTOR
		protected TreeView() {}
		public TreeView (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		[DefaultValue(false)]
		public virtual bool IsRoot {
			get { return isRoot; }
			set {
				if (isRoot == value)
					return;
				isRoot = value;
				NotifyValueChangedAuto (isRoot);
			}
		}


		void onExpandAll_MouseClick (object sender, MouseButtonEventArgs e)
		{
			ExpandAll ();
		}

		public void ExpandAll(){
			foreach (Group grp in items.Children) {
				foreach (Widget go in grp.Children) {
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


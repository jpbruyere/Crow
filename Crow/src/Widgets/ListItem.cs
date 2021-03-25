// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// Top container to use as ItemTemplate's root for TemplatedGroups (lists, treeviews, ...) that add selection
	/// status and events
	/// </summary>
	public class ListItem : Container, ISelectable
	{
		#region CTOR
		public ListItem (){}
		public ListItem (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		bool isSelected;

		public event EventHandler Selected;
		public event EventHandler Unselected;

		[DefaultValue (false)]
		public virtual bool IsSelected {
			get { return isSelected; }
			set {
				if (isSelected == value)
					return;
				isSelected = value;

				if (isSelected)
					Selected.Raise (this, null);
				else
					Unselected.Raise (this, null);

				NotifyValueChangedAuto (isSelected);
			}
		}
	}
}

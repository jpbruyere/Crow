//
//  MenuItem.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
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
using System.ComponentModel;

namespace Crow
{
	public class MenuItem : TemplatedGroup
	{
		#region CTOR
		public MenuItem () : base() {}
		#endregion

		public event EventHandler Open;
		public event EventHandler Close;
		public event EventHandler Execute;

		string caption;
		Command command;//TODO
		bool isOpened;

		[XmlAttributeAttribute][DefaultValue(false)]
		public bool IsOpened {
			//get { return MenuRoot == null ? false : MenuRoot.IsOpened; }
			get { return isOpened; }
			set {
				if (isOpened == value)
					return;
				isOpened = value;
				NotifyValueChanged ("IsOpened", isOpened);

				if (isOpened) {
					onOpen (this, null);
					if (MenuRoot != null)
						MenuRoot.IsOpened = true;
				}else
					onClose (this, null);
			}
		}

		[XmlAttributeAttribute][DefaultValue(null)]
		public virtual Command Command {
			get { return command; }
			set {
				if (command == value)
					return;
				command = value;
				NotifyValueChanged ("Command", command);
			}
		}

		[XmlAttributeAttribute][DefaultValue("MenuItem")]
		public string Caption {
			get { return caption; }
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChanged ("Caption", caption);
			}
		}

		[XmlIgnore]Menu MenuRoot {
			get {
				ILayoutable tmp = LogicalParent;
				while (tmp != null) {
					if (tmp is Menu)
						return tmp as Menu;
					tmp = tmp.LogicalParent;
				}
				return null;
			}
		}

		public override void AddItem (GraphicObject g)
		{
			base.AddItem (g);
			g.NotifyValueChanged ("PopDirection", Alignment.Right);
		}

		void onMI_Click (object sender, MouseButtonEventArgs e)
		{
			Execute.Raise (this, null);
		}
		protected virtual void onOpen (object sender, EventArgs e){
			MenuRoot.IsOpened = true;
			Open.Raise (this, null);
		}
		protected virtual void onClose (object sender, EventArgs e){
			//MenuRoot.IsOpened = true;
			Close.Raise (this, null);
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if (MenuRoot == null || Items.Count == 0)
				return;
			IsOpened = MenuRoot.IsOpened;
		}
	}
}


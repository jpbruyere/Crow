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
	public class MenuItem : Menu
	{
		#region CTOR
		public MenuItem () : base() {}
		#endregion

		public event EventHandler Open;
		public event EventHandler Close;

		string caption;
		Command command;
		Picture icon;
		bool isOpened;
		Measure popWidth, popHeight;

		#region Public properties
		[XmlAttributeAttribute][DefaultValue(false)]
		public bool IsOpened {
			get { return isOpened; }
			set {
				if (isOpened == value)
					return;
				isOpened = value;
				NotifyValueChanged ("IsOpened", isOpened);

				if (isOpened) {
					onOpen (this, null);
					(LogicalParent as Menu).AutomaticOpenning = true;
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

				if (command != null) {
					command.raiseAllValuesChanged ();
					command.ValueChanged -= Command_ValueChanged;
				}

				command = value;

				if (command != null) {
					command.ValueChanged += Command_ValueChanged;
					command.raiseAllValuesChanged ();
				}

				NotifyValueChanged ("Command", command);
			}
		}
		[XmlAttributeAttribute][DefaultValue("MenuItem")]
		public string Caption {
			get { return Command == null ? caption : Command.Caption; }
			set {
				if (caption == value)
					return;
				caption = value;

				if (command == null)//raise value changed only if not bound to a command
					NotifyValueChanged ("Caption", caption);
			}
		}
		[XmlAttributeAttribute]
		public Picture Icon {
			get { return Command == null ? icon : Command.Icon;; }
			set {
				if (icon == value)
					return;
				icon = value;
				if (command == null)
					NotifyValueChanged ("Icon", icon);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Fit")]
		public virtual Measure PopWidth {
			get { return popWidth; }
			set {
				if (popWidth == value)
					return;
				popWidth = value;
				NotifyValueChanged ("PopWidth", popWidth);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Fit")]
		public virtual Measure PopHeight {
			get { return popHeight; }
			set {
				if (popHeight == value)
					return;
				popHeight = value;
				NotifyValueChanged ("PopHeight", popHeight);
			}
		}
		#endregion

		public override void AddItem (GraphicObject g)
		{
			base.AddItem (g);
			g.NotifyValueChanged ("PopDirection", Alignment.Right);
		}

		void Command_ValueChanged (object sender, ValueChangeEventArgs e)
		{
			NotifyValueChanged (e.MemberName, e.NewValue);
		}
		void onMI_Click (object sender, MouseButtonEventArgs e)
		{
			if (command != null)
				command.Execute ();
			if(!IsOpened)
				(LogicalParent as Menu).AutomaticOpenning = false;
		}
		protected virtual void onOpen (object sender, EventArgs e){
			Open.Raise (this, null);
		}
		protected virtual void onClose (object sender, EventArgs e){
			Close.Raise (this, null);
		}
		public override bool MouseIsIn (Point m)
		{
			return base.MouseIsIn (m) || child.MouseIsIn (m);
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if ((LogicalParent as Menu).AutomaticOpenning && items.Children.Count>0)
				IsOpened = true;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			if (IsOpened)
				IsOpened = false;
			base.onMouseLeave (this, e);
		}
	}
}


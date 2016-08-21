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

		public event EventHandler Execute;

		string caption;
		Command command;//TODO

		[XmlAttributeAttribute()][DefaultValue(null)]
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
				ILayoutable tmp = Parent;
				while (tmp != null) {
					if (tmp is Menu)
						return tmp as Menu;
					tmp = tmp.Parent;
				}
				return null;
			}
		}

		public override void AddItem (GraphicObject g)
		{
			base.AddItem (g);
			g.NotifyValueChanged ("Orientation", Alignment.Right);
		}

		void onMI_Click (object sender, MouseButtonEventArgs e)
		{
			Execute.Raise (this, null);
		}
	}
}


﻿//
//  Menu.cs
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
	public class Menu : TemplatedGroup
	{
		#region CTOR
		public Menu () : base() {}
		#endregion

		Orientation orientation;
		bool autoOpen = false;

		#region Public properties
		[XmlAttributeAttribute][DefaultValue(Orientation.Horizontal)]
		public Orientation Orientation {
			get { return orientation; }
			set {
				if (orientation == value)
					return;
				orientation = value;
				NotifyValueChanged ("Orientation", orientation);
			}
		}
		[XmlIgnore]public bool AutomaticOpenning
		{
			get { return autoOpen; }
			set	{ autoOpen = value;	}
		}
		#endregion

		public override void AddItem (GraphicObject g)
		{			
			base.AddItem (g);

			if (orientation == Orientation.Horizontal)
				g.NotifyValueChanged ("PopDirection", Alignment.Bottom);
			else
				g.NotifyValueChanged ("PopDirection", Alignment.Right);
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			AutomaticOpenning = false;
		}
	}
}


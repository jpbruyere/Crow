//
//  MembersView.cs
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
using Crow;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Cairo;

namespace Crow.Coding
{	
	public class MembersView : ListBox
	{		
		object instance;

		[XmlAttributeAttribute][DefaultValue(null)]
		public virtual object Instance {
			get { return instance; }
			set {
				if (instance == value)
					return;
				instance = value;
				NotifyValueChanged ("Instance", instance);
				if (Instance is GraphicObject)
					NotifyValueChanged ("SelectedItemName", (Instance as GraphicObject).Name);
				else
					NotifyValueChanged ("SelectedItemName", "");

				if (instance == null) {
					Data = null;
					return;
				}

				MemberInfo[] members = instance.GetType ().GetMembers (BindingFlags.Public | BindingFlags.Instance);

				List<PropertyContainer> props = new List<PropertyContainer> ();
				foreach (MemberInfo m in members) {
					if (m.MemberType == MemberTypes.Property) {
						PropertyInfo pi = m as PropertyInfo;
						if (!pi.CanWrite)
							continue;
						if (pi.GetCustomAttribute (typeof(XmlIgnoreAttribute)) != null)
							continue;
						props.Add (new PropertyContainer (pi, instance));
					}
				}
				Data = props.OrderBy(p=>p.Name).ToArray ();
			}
		}
		public MembersView () : base()
		{
		}

//		public override void Paint (ref Context ctx)
//		{
//			base.Paint (ref ctx);
//
//			if (SelectedIndex < 0)
//				return;
//
//			Rectangle r =  Parent.ContextCoordinates(Items [SelectedIndex].Slot);
//			ctx.SetSourceRGB (0, 0, 1);
//			ctx.Rectangle (r);
//			ctx.LineWidth = 2;
//			ctx.Stroke ();
//		}

	}
}

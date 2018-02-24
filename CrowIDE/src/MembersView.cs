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

namespace Crow.Coding
{
	public class PropertyContainer : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		PropertyInfo pi;
		object instance;

		public string Name { get { return pi.Name; }}
		public object Value {
			get { return pi.GetValue(instance); }
			set {
				try {
					if (!pi.PropertyType.IsAssignableFrom(value.GetType()) && pi.PropertyType != typeof(string)){
						if (pi.PropertyType.IsEnum) {
							if (value is string) {
								pi.SetValue (instance, Enum.Parse (pi.PropertyType, (string)value));
							}else
								pi.SetValue (instance, value);
						} else {
							MethodInfo me = pi.PropertyType.GetMethod
								("Parse", BindingFlags.Static | BindingFlags.Public,
									System.Type.DefaultBinder, new Type [] {typeof (string)},null);
							pi.SetValue (instance, me.Invoke (null, new object[] { value }), null);
						}
					}else
						pi.SetValue(instance, value);
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine ("Error setting property:"+ ex.ToString());
				}
				NotifyValueChanged ("Value", value);
			}
		}
		public string Type { get { return pi.PropertyType.IsEnum ?
					"System.Enum"
					: pi.PropertyType.FullName; }}
		public object[] Choices {
			get {
				return Enum.GetValues (pi.PropertyType).Cast<object>().ToArray();
			}
		}

		public PropertyContainer(PropertyInfo prop, object _instance){
			pi = prop;
			instance = _instance;
		}

	}
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
				Data = props.ToArray ();
			}
		}
		public MembersView () : base()
		{
		}
	}
}

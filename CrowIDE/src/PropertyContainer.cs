//
// PropertyContainer.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.Reflection;
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
		GraphicObject go;

		public string Name { get { return pi.Name; }}
		public object Value {
			get { return go.design_members.ContainsKey(Name) ?
				go.design_members[Name] : pi.GetValue(instance); }
			set {
				if (go.design_members.ContainsKey (Name)) {
					if (go.design_members [Name] == (string)value)
						return;
				}
				go.design_members [Name] = (string)value;
//				try {
//					if (!pi.PropertyType.IsAssignableFrom(value.GetType()) && pi.PropertyType != typeof(string)){
//						if (pi.PropertyType.IsEnum) {
//							if (value is string) {
//								pi.SetValue (instance, Enum.Parse (pi.PropertyType, (string)value));
//							}else
//								pi.SetValue (instance, value);
//						} else {
//							MethodInfo me = pi.PropertyType.GetMethod
//								("Parse", BindingFlags.Static | BindingFlags.Public,
//									System.Type.DefaultBinder, new Type [] {typeof (string)},null);
//							pi.SetValue (instance, me.Invoke (null, new object[] { value }), null);
//						}
//					}else
//						pi.SetValue(instance, value);
//				} catch (Exception ex) {
//					System.Diagnostics.Debug.WriteLine ("Error setting property:"+ ex.ToString());
//				}
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

		public Fill LabForeground {
			get { return go.design_members.ContainsKey(Name) ? Color.Black : Color.DimGray;}
		}

		public PropertyContainer(PropertyInfo prop, object _instance){
			pi = prop;
			instance = _instance;
			go = instance as GraphicObject;
		}

	}
}


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
using System.Collections.Generic;
using System.Diagnostics;

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

		public List<Crow.Command> Commands;
		PropertyInfo pi;
		MembersView mview;
//		object instance;
//		GraphicObject go;

		public PropertyContainer(MembersView mv, PropertyInfo prop){
			mview = mv;
			pi = prop;
//			instance = _instance;
//			go = instance as GraphicObject;

			Commands = new List<Crow.Command> (new Crow.Command[] {
				new Crow.Command(new Action(() => Reset())) { Caption = "Reset to default"},
			});
		}

		public string Name { get { return pi.Name; }}
		public object Value {
			get {
				return pi.GetValue(mview.ProjectNode.SelectedItem);
//				GraphicObject inst = mview.ProjectNode.SelectedItem as GraphicObject;
//				Debug.WriteLine("read {0}.{1}", inst.Name, Name);
//				if (!inst.design_members.ContainsKey (Name))
//					return pi.GetValue (inst);
//				
//				if (inst.design_members [Name].StartsWith ("{"))
//					return inst.design_members [Name];
//				else
//					return pi.GetValue (inst);
			}
			set {
				try {
					GraphicObject inst = mview.ProjectNode.SelectedItem as GraphicObject;
					string valstr = null;
					if (value != null)
						valstr = value.ToString();
					if (inst.design_members.ContainsKey (Name)) {
						if (inst.design_members [Name] == valstr)
							return;
						Debug.WriteLine("update {0} : {1} = {2}", inst.Name, Name, valstr);
						inst.design_members [Name] = value.ToString();
					} else {
						Debug.WriteLine("add {0} : {1} = {2}", inst.Name, Name, valstr);
						inst.design_members.Add (Name, value.ToString());
					}				

					if (!pi.PropertyType.IsAssignableFrom(value.GetType()) && pi.PropertyType != typeof(string)){
						if (pi.PropertyType.IsEnum) {
							if (value is string) {
								pi.SetValue (inst, Enum.Parse (pi.PropertyType, (string)value));
							}else
								pi.SetValue (inst, value);
						} else {
							MethodInfo me = pi.PropertyType.GetMethod
								("Parse", BindingFlags.Static | BindingFlags.Public,
									System.Type.DefaultBinder, new Type [] {typeof (string)},null);
							pi.SetValue (inst, me.Invoke (null, new object[] { value }), null);
						}
					}else
						pi.SetValue(inst, value);
					
					mview.ProjectNode.Instance.HasChanged = true;
					NotifyValueChanged ("Value", value);
					NotifyValueChanged ("LabForeground", LabForeground);
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine ("Error setting property:"+ ex.ToString());
				}
				//
			}
		}
		/// <summary>
		/// for style attribute which is a string, return Style as type
		/// </summary>
		public string Type { get { return pi.PropertyType.IsEnum ?
				"System.Enum"
					: pi.Name == "Style" ? "Style" : pi.PropertyType.FullName; }}
		
		public object[] Choices {
			get {
				return pi.PropertyType.IsEnum ?
					Enum.GetValues (pi.PropertyType).Cast<object>().ToArray() :
					mview.ProjectNode.Project.solution.AvailaibleStyles;
			}
		}

		public Fill LabForeground {
			get { return (mview.ProjectNode.SelectedItem as GraphicObject).design_members.ContainsKey(Name) ? Color.Black : Color.DimGray;}
		}

		/// <summary>
		/// reset to default value
		/// </summary>
		public void Reset () {
			GraphicObject inst = mview.ProjectNode.SelectedItem as GraphicObject;
			if (!inst.design_members.ContainsKey (Name))
				return;
			inst.design_members.Remove (Name);
			//NotifyValueChanged ("Value", Value);
			mview.ProjectNode.Instance.HasChanged = true;
			//should reinstantiate to get default
		}



	}
}


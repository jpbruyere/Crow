// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;

namespace Crow.Coding
{
	public class StyleItemContainer
	{
		public object Value;
		public string Name;
		public StyleItemContainer (string name, object _value)
		{
			Name = name;
			Value = _value;
		}
	}
	public class StyleContainer : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		#endregion

		Style style;
		bool isExpanded;

		public string Name;
		public List<StyleItemContainer> Items;
		public bool IsExpanded {
			get { return isExpanded; }
			set {
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged ("IsExpanded", isExpanded);
			}
		}
		public StyleContainer (string name, Style _style)
		{
			Name = name;
			style = _style;

			Items = new List<StyleItemContainer> ();
			foreach (string k in style.Keys) {
				Items.Add (new StyleItemContainer (k, style [k]));
			}
		}
	}
}

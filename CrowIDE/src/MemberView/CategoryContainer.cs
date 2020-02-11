// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Diagnostics;

namespace Crow.Coding
{
	[DebuggerDisplay ("{Name}")]
	public class CategoryContainer : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		bool _isExpanded = true;

		public PropertyContainer[] Properties;
		public string Name;

		public bool IsExpanded
		{
			get { return _isExpanded; }
			set
			{
				if (value == _isExpanded)
					return;

				_isExpanded = value;

				NotifyValueChanged ("IsExpanded", _isExpanded);
			}
		}

		public CategoryContainer (string categoryName, PropertyContainer[] properties){
			Name = categoryName;
			Properties = properties;
		}
	}
}


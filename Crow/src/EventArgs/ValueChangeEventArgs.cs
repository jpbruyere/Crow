// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	/// <summary>
	/// Arguments for the ValueChange event used for Binding
	/// </summary>
	public class ValueChangeEventArgs: EventArgs
	{
		/// <summary>The name of the member whose value has changed</summary>
		public string MemberName;
		/// <summary>New value for that member</summary>
		public object NewValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Crow.ValueChangeEventArgs"/> class.
		/// </summary>
		/// <param name="_memberName">Member name.</param>
		/// <param name="_newValue">New value.</param>
		public ValueChangeEventArgs (string _memberName, object _newValue) : base()
		{
			MemberName = _memberName;
			NewValue = _newValue;
		}
	}
}


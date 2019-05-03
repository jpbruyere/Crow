﻿//
// Border.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
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
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using vkvg;

namespace Crow
{
	/// <summary>
	/// Convenient widget for selecting value from enum
	/// </summary>
	public class EnumSelector : GenericStack
	{
		#region CTOR
		protected EnumSelector () : base(){}
		public EnumSelector (Interface iface) : base(iface){}
		#endregion

		#region private fields
		Enum enumValue;
		Type enumType;
		#endregion

		#region public properties
		/// <summary>
		/// use to define the colors of the 3d border
		/// </summary>
		[DefaultValue(null)]
		public virtual Enum EnumValue {
			get { return enumValue; }
			set {
				if (enumValue == value)
					return;

				enumValue = value;

				if (enumValue != null) {
					if (enumType != enumValue.GetType ()) {
						ClearChildren ();
						enumType = enumValue.GetType ();
						foreach (string en in enumType.GetEnumNames ()) {
							RadioButton rb = new RadioButton (IFace);
							rb.Caption = en;
							if (enumValue.ToString() == en)
								rb.IsChecked = true;
							rb.Checked += (sender, e) => (((RadioButton)sender).Parent as EnumSelector).EnumValue = (Enum)Enum.Parse (enumType, (sender as RadioButton).Caption);
							AddChild (rb);
							RegisterForLayouting (LayoutingType.All);
						}
					}
				} else 
					ClearChildren ();

				NotifyValueChanged ("EnumValue", enumValue);
				RegisterForRedraw ();
			}
		}
		#endregion

	}
}


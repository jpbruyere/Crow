//
// CheckBox.cs
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
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
	/// <summary>
	/// templated 2 state control
	/// </summary>
	public class Switch : TemplatedControl
	{
		#region CTOR
		protected Switch() : base(){}
		public Switch (Interface iface) : base(iface){}
		#endregion

		bool isOn;

		public event EventHandler SwitchedOn;
		public event EventHandler SwitchedOff;

		[XmlAttributeAttribute()][DefaultValue(false)]
		public bool IsOn
		{
			get { return isOn; }
			set
			{
				if (isOn == value)
					return;

				isOn = value;

				NotifyValueChanged ("IsOn", value);

				if (isOn)
					SwitchedOn.Raise (this, null);
				else
					SwitchedOff.Raise (this, null);
			}
		}

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsOn = !IsOn;
			base.onMouseClick (sender, e);
		}
	}
}

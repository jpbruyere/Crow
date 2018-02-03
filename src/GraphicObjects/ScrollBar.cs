//
// ScrollBar.cs
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

namespace Crow
{
	/// <summary>
	/// templeted numeric control
	/// </summary>
	public class ScrollBar : NumericControl
	{
		//TODO:could be replaced by a template for a Slider

		Orientation _orientation;

		#region CTOR
		public ScrollBar () : base(){}
		public ScrollBar(Interface iface) : base(iface)	{}
		#endregion

		[XmlAttributeAttribute()][DefaultValue(Orientation.Vertical)]
		public virtual Orientation Orientation
		{
			get { return _orientation; }
			set {				
				if (_orientation == value)
					return;
				_orientation = value;
				NotifyValueChanged ("Orientation", _orientation);
				RegisterForGraphicUpdate ();
			}
		}
		public void onScrollBack (object sender, MouseButtonEventArgs e)
		{
			Value -= SmallIncrement;
		}
		public void onScrollForth (object sender, MouseButtonEventArgs e)
		{
			Value += SmallIncrement;
		}

		public void onSliderValueChange(object sender, ValueChangeEventArgs e){
			if (e.MemberName == "Value")
				Value = Convert.ToDouble(e.NewValue);
		}
	}
}

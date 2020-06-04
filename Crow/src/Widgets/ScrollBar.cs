// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
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
		int _cursorSize;

		#region CTOR
		protected ScrollBar () {}
		public ScrollBar(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		[DefaultValue(Orientation.Vertical)]
		public virtual Orientation Orientation
		{
			get { return _orientation; }
			set {				
				if (_orientation == value)
					return;
				_orientation = value;
				NotifyValueChangedAuto (_orientation);
				if (_orientation == Orientation.Horizontal)
					NotifyValueChanged ("ScrollBackShape", "M 1.5,3.5 L 6.5,0.5 L 6.5,6.5 Z G");
				else
					NotifyValueChanged ("ScrollBackShape", "M 4.5,0.5 L 9.5,9.5 L 0.5,9.5 Z G");

                RegisterForGraphicUpdate ();
			}
		}
		[DefaultValue(20)]
		public virtual int CursorSize {
			get { return _cursorSize; }
			set {
				if (_cursorSize == value)
					return;
				_cursorSize = value;
				RegisterForGraphicUpdate ();
				NotifyValueChangedAuto (_cursorSize);
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

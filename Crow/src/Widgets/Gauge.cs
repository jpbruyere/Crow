// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


using System;
using System.ComponentModel;
using Crow.Cairo;

namespace Crow {
	public class Gauge : Widget
	{
		#region CTOR
		protected Gauge () {}
		public Gauge (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region protected fields
		protected double actualValue, minValue, maxValue;
		CursorType cursorType;
		Orientation orientation;
		int borderWidth;
		#endregion

		#region public properties
		[DefaultValue (0.0)]
		public virtual double Minimum {
			get { return minValue; }
			set {
				if (minValue == value)
					return;

				minValue = value;
				NotifyValueChangedAuto (minValue);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(100.0)]
		public virtual double Maximum
		{
			get { return maxValue; }
			set {
				if (maxValue == value)
					return;

				maxValue = value;
				NotifyValueChangedAuto (maxValue);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(0.0)]
		public virtual double Value
		{
			get { return actualValue; }
			set
			{
				if (value == actualValue)
					return;

				if (value < minValue)
					actualValue = minValue;
				else if (value > maxValue)
					actualValue = maxValue;
				else                    
					actualValue = value;

				NotifyValueChangedAuto (actualValue);
				RegisterForGraphicUpdate();
			}
		}
		[DefaultValue (CursorType.Pentagone)]
		public CursorType CursorType {
			get => cursorType;
			set {
				if (cursorType == value)
					return;
				cursorType = value;
				NotifyValueChangedAuto (cursorType);
				RegisterForRedraw ();
			}
		}
		[DefaultValue (Orientation.Horizontal)]
		public virtual Orientation Orientation {
			get => orientation;
			set {
				if (orientation == value)
					return;
				orientation = value;
				NotifyValueChangedAuto (orientation);
				RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			}
		}
		/// <summary>
		/// border width in pixels
		/// </summary>
		[DefaultValue (0)]
		public virtual int BorderWidth {
			get { return borderWidth; }
			set {
				if (borderWidth == value)
					return;
				borderWidth = value;
				NotifyValueChangedAuto (borderWidth);
				RegisterForGraphicUpdate ();
			}
		}
		#endregion

		protected override void onDraw (Context gr) {
			Rectangle cb = ClientRectangle;

			if (orientation == Orientation.Horizontal)
				cb.Width = (int)(cb.Width / Maximum * Value);
			else
				cb.Height = (int)(cb.Height / Maximum * Value);

			Background.SetAsSource (gr, cb);
			CairoHelpers.CairoRectangle (gr, cb, CornerRadius);
			gr.Fill ();
			Foreground.SetAsSource (gr, cb);
			if (borderWidth > 0)
				CairoHelpers.CairoRectangle (gr, cb, CornerRadius, borderWidth);
		}
	}
}


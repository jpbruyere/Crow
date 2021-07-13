// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif

namespace Crow {
	public class Gauge : Widget
	{
		#region CTOR
		protected Gauge () {}
		public Gauge (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region protected fields
		protected double actualValue, minValue, maxValue;
		Orientation orientation;
		bool inverted;
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
		/// if true, horizontal gauge will align drawing right, and vertical on bottom.
		/// </summary>
		public bool Inverted {
			get => inverted;
			set {
				if (inverted == value)
					return;
				inverted = value;
				NotifyValueChangedAuto (inverted);
				RegisterForRedraw ();
			}
		}
		#endregion

		protected override void onDraw (Context gr) {
			DbgLogger.StartEvent (DbgEvtType.GODraw, this);

			base.onDraw (gr);

			Rectangle cb = ClientRectangle;
			Rectangle r = cb;

			if (orientation == Orientation.Horizontal) {
				r.Width = (int)(cb.Width / Maximum * Value);
				if (inverted)
					r.Left = cb.Right - r.Width;
			} else {				
				r.Height = (int)(cb.Height / Maximum * Value);
				if (inverted)
					r.Top = cb.Bottom - r.Height;
			}

			Foreground?.SetAsSource (IFace, gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill ();

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}
	}
}


// Copyright (c) 2013-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;


using Drawing2D;

namespace Crow {
	public class ColorGauge : Widget
	{
		#region CTOR
		protected ColorGauge () {}
		public ColorGauge (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		ColorComponent component;
		Color currentColor = Colors.Black;
		protected Orientation orientation;
		protected bool inverted;

		
		[DefaultValue (ColorComponent.Value)]
		public ColorComponent Component {
			get => component;
			set {
				if (component == value)
					return;
				component = value;
				NotifyValueChangedAuto (component);
				RegisterForRedraw ();
			}
		}
		public Color CurrentColor {
			get => currentColor;
			set {
				if (currentColor == value)
					return;

				currentColor = value;

				NotifyValueChangedAuto (currentColor);
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
		protected override void onDraw (IContext gr) {
			DbgLogger.StartEvent (DbgEvtType.GODraw, this);

			//base.onDraw (gr);

			Rectangle cb = ClientRectangle;
			Rectangle r = cb;

			/*if (orientation == Orientation.Horizontal) {
				r.Width = (int)(cb.Width / Maximum * Value);
				if (inverted)
					r.Left = cb.Right - r.Width;
			} else {
				r.Height = (int)(cb.Height / Maximum * Value);
				if (inverted)
					r.Top = cb.Bottom - r.Height;
			}*/
			Gradient grad = new Gradient (
				Orientation == Orientation.Vertical ? GradientType.Vertical : GradientType.Horizontal);
			Color c = currentColor;

			switch (component) {
			case ColorComponent.Red:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (0, c.G, c.B, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (255, c.G, c.B, c.A)));
				break;
			case ColorComponent.Green:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (c.R, 0, c.B, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (c.R, 255, c.B, c.A)));
				break;
			case ColorComponent.Blue:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (c.R, c.G, 0, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (c.R, c.G, 255, c.A)));
				break;
			case ColorComponent.Alpha:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (c.R, c.G, c.B, 0)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (c.R, c.G, c.B, 255)));
				break;
			case ColorComponent.Hue:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (1.0, 0, 0, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.167, new Color (1.0, 1, 0, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.333, new Color (0.0, 1, 0, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.5, new Color (0.0, 1, 1, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.667, new Color (0.0, 0, 1, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.833, new Color (1.0, 0, 1, 1)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (1.0, 0, 0, 1)));
				break;
			case ColorComponent.Saturation:
				grad.Stops.Add (new Gradient.ColorStop (0, Color.FromHSV (c.Hue, c.Value, 0, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, Color.FromHSV (c.Hue, c.Value, 0xff, c.A)));
				break;
			case ColorComponent.Value:
				grad.Stops.Add (new Gradient.ColorStop (0, Color.FromHSV (c.Hue, 0, c.Saturation, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, Color.FromHSV (c.Hue, 0xff, c.Saturation, c.A)));
				break;
			}

			grad.SetAsSource (IFace, gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill ();

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}
	}
}


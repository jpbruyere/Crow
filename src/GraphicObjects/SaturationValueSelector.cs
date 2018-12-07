//
// SaturationValueSelector.cs
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
using Cairo;
using System.Xml.Serialization;

namespace Crow
{
	[DesignIgnore]
	public class SaturationValueSelector : ColorSelector
	{
		public SaturationValueSelector () : base(){}
		public SaturationValueSelector (Interface iface) : base(iface)
		{
		}

		double v, s;

		
		public virtual double V {
			get { return v; }
			set {
				if (v == value)
					return;
				v = value;
				NotifyValueChanged ("V", v);
				mousePos.Y = (int)Math.Floor((1.0-v) * (double)ClientRectangle.Height);

				RegisterForRedraw ();
			}
		}
		
		public virtual double S {
			get { return s; }
			set {
				if (s == value)
					return;
				s = value;
				NotifyValueChanged ("S", s);
				mousePos.X = (int)Math.Floor(s * (double)ClientRectangle.Width);

				RegisterForRedraw ();
			}
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Rectangle r = ClientRectangle;

			if (Foreground != null) {//TODO:test if null should be removed
				Foreground.SetAsSource (gr, r);
				CairoHelpers.CairoRectangle (gr, r, CornerRadius);
				gr.Fill ();
			}

			Crow.Gradient grad = new Gradient (Gradient.Type.Horizontal);
			grad.Stops.Add (new Gradient.ColorStop (0, new Color (1, 1, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (1, new Color (1, 1, 1, 0)));
			grad.SetAsSource (gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();
			grad = new Gradient (Gradient.Type.Vertical);
			grad.Stops.Add (new Gradient.ColorStop (0, new Color (0, 0, 0, 0)));
			grad.Stops.Add (new Gradient.ColorStop (1, new Color (0, 0, 0, 1)));
			grad.SetAsSource (gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();

		}

		public override void Paint (ref Context ctx)
		{
			base.Paint (ref ctx);

			Rectangle rb = Slot + Parent.ClientRectangle.Position;
			ctx.Save ();

			ctx.Translate (rb.X, rb.Y);

			ctx.SetSourceColor (Color.DimGrey);
			ctx.Arc (mousePos.X, mousePos.Y, 3.5, 0, Math.PI * 2.0);
			ctx.LineWidth = 0.5;
			ctx.Stroke ();
			ctx.Translate (-0.5, -0.5);
			ctx.Arc (mousePos.X, mousePos.Y, 3.5, 0, Math.PI * 2.0);
			ctx.SetSourceColor (Color.White);
			ctx.Stroke ();

			ctx.Restore ();
		}

		protected override void updateMouseLocalPos (Point mPos)
		{
			base.updateMouseLocalPos (mPos);

			Rectangle cb = ClientRectangle;
			s = (double)mousePos.X / (double)cb.Width;
			v = 1.0 - (double)mousePos.Y / (double)cb.Height;
			NotifyValueChanged ("S", s);
			NotifyValueChanged ("V", v);

			RegisterForRedraw ();
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);
			switch (layoutType) {
			case LayoutingType.Width:
				mousePos.X = (int)Math.Floor(s * (double)ClientRectangle.Width);
				break;
			case LayoutingType.Height:
				mousePos.Y = (int)Math.Floor((1.0-v) * (double)ClientRectangle.Height);
				break;
			}
		}
	}
}


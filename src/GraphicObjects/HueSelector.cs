//
//  HueSelector.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Xml.Serialization;
using System.ComponentModel;
using Cairo;

namespace Crow
{
	public class HueSelector : ColorSelector
	{
		public HueSelector () : base()
		{
		}

		Orientation _orientation;
		double hue;

		[XmlAttributeAttribute][DefaultValue(Orientation.Horizontal)]
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
		[XmlAttributeAttribute()]
		public virtual double Hue {
			get { return hue; }
			set {
				if (hue == value)
					return;
				hue = value;

				notifyHueChanged ();
				updateMousePosFromHue ();
			}
		}
		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			Rectangle r = ClientRectangle;

			Gradient.Type gt = Gradient.Type.Horizontal;
			if (Orientation == Orientation.Vertical)
				gt = Gradient.Type.Vertical;

			Crow.Gradient grad = new Gradient (gt);

			grad.Stops.Add (new Gradient.ColorStop (0, new Color (1, 0, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.167, new Color (1, 1, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.333, new Color (0, 1, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.5, new Color (0, 1, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.667, new Color (0, 0, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.833, new Color (1, 0, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (1, new Color (1, 0, 0, 1)));

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

			ctx.SetSourceColor (Color.White);
			Rectangle r = ClientRectangle;
			if (Orientation == Orientation.Horizontal) {
				r.Width = 5;
				r.X = mousePos.X;
			} else {
				r.Height = 5;
				r.Y = mousePos.Y;
			}

			CairoHelpers.CairoRectangle (ctx, r, 2);
			ctx.SetSourceColor (Color.White);
			ctx.LineWidth = 2.0;
			ctx.Stroke();
			ctx.Restore ();
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (Orientation == Orientation.Horizontal) {
				if (layoutType == LayoutingType.Width)
					updateMousePosFromHue ();
			} else if (layoutType == LayoutingType.Height)
				updateMousePosFromHue ();
		}
		protected override void updateMouseLocalPos (Point mPos)
		{
			base.updateMouseLocalPos (mPos);
			if (Orientation == Orientation.Horizontal)
				hue = (double)mousePos.X / (double)ClientRectangle.Width;
			else
				hue = (double)mousePos.Y / (double)ClientRectangle.Height;
			notifyHueChanged ();
		}
		void updateMousePosFromHue(){
			if (Orientation == Orientation.Horizontal)
				mousePos.X = (int)Math.Floor(hue * (double)ClientRectangle.Width);
			else
				mousePos.Y = (int)Math.Floor(hue * (double)ClientRectangle.Height);
			CurrentInterface.EnqueueForRepaint (this);
		}
		void notifyHueChanged(){
			NotifyValueChanged ("Hue", hue);
			NotifyValueChanged ("HueColor", new SolidColor (Color.FromHSV (hue)));
		}
	}
}


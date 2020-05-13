//
// HueSelector.cs
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
using Crow.Cairo;

namespace Crow
{
	[DesignIgnore]
	public class HueSelector : ColorSelector
	{
		#region CTOR
		protected HueSelector () : base(){}
		public HueSelector (Interface iface) : base(iface)
		{
		}
		#endregion

		Orientation _orientation;
		double hue;
		CursorType cursor = CursorType.Pentagone;

		[DefaultValue(Orientation.Horizontal)]
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
		
		public virtual double Hue {
			get { return hue; }
			set {
				if (hue == value)
					return;
				hue = value;
				notifyHueChanged ();
				updateMousePosFromHue ();
				RegisterForRedraw ();
			}
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			RectangleD r = ClientRectangle;
			r.Height -= 4;
			r.Y += 2;

			Gradient.Type gt = Gradient.Type.Horizontal;
			if (Orientation == Orientation.Vertical)
				gt = Gradient.Type.Vertical;

			Gradient grad = new Gradient (gt);

			grad.Stops.Add (new Gradient.ColorStop (0,     new Color (1, 0, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.167, new Color (1, 1, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.333, new Color (0, 1, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.5,   new Color (0, 1, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.667, new Color (0, 0, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.833, new Color (1, 0, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (1,     new Color (1, 0, 0, 1)));

			grad.SetAsSource (gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();

			r = ClientRectangle;

			switch (cursor) {
			case CursorType.Rectangle:
				if (Orientation == Orientation.Horizontal) {
					r.Width = 5;
					r.X = mousePos.X - 2.5;
				} else {
					r.Height = 5;
					r.Y = mousePos.Y - 2.5;
				}
				CairoHelpers.CairoRectangle (gr, r, 1);
				break;
			case CursorType.Circle:
				if (Orientation == Orientation.Horizontal)
					gr.Arc (mousePos.X, r.Center.Y, 3.5, 0, Math.PI * 2.0);
				else
					gr.Arc (r.Center.X, mousePos.Y, 3.5, 0, Math.PI * 2.0);
				break;
			case CursorType.Pentagone:
				if (Orientation == Orientation.Horizontal) {
					r.Width = 5;
					r.X = mousePos.X - 2.5;
					double y = r.CenterD.Y-r.Height*0.2;
					gr.MoveTo (mousePos.X, y);
					y += r.Height * 0.15;
					gr.LineTo (r.Right, y);
					gr.LineTo (r.Right, r.Bottom-0.5);
					gr.LineTo (r.Left, r.Bottom-0.5);
					gr.LineTo (r.Left, y);
					gr.ClosePath ();
				} else { 
				}
				break;			
			}

			gr.SetSourceColor (Colors.Black);
			gr.LineWidth = 2.0;
			gr.StrokePreserve ();
			gr.SetSourceColor (Colors.White);
			gr.LineWidth = 1.0;
			gr.Stroke ();
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
			RegisterForRedraw ();
		}
		void updateMousePosFromHue(){
			if (Orientation == Orientation.Horizontal)
				mousePos.X = (int)Math.Floor(hue * (double)ClientRectangle.Width);
			else
				mousePos.Y = (int)Math.Floor(hue * (double)ClientRectangle.Height);
		}
		void notifyHueChanged(){
			NotifyValueChanged ("Hue", hue);
			NotifyValueChanged ("HueColor", new SolidColor (Color.FromHSV (hue)));
		}
	}
}


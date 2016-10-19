//
//  ColorPicker.cs
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

namespace Crow
{
	public class ColorSelector : GraphicObject
	{
		const double colDiv = 1.0 / 255.0;

		public ColorSelector ():base()
		{
		}
		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			Rectangle r = ClientRectangle;

//			if (r.Width > r.Height) {
//				int diff = r.Width - r.Height;
//				r.Width = r.Height;
//				r.X -= diff / 2;
//			} else if (r.Height > r.Width) {
//				int diff = r.Height - r.Width;
//				r.Height = r.Width;
//				r.Y += diff / 2;
//			}

			Crow.Gradient grad = new Gradient (Gradient.Type.Horizontal);

			grad.Stops.Add (new Gradient.ColorStop (0, new Color (1, 0, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.2, new Color (1, 1, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.4, new Color (0, 1, 0, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.6, new Color (0, 1, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.8, new Color (0, 0, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (1, new Color (1, 0, 0, 1)));

			grad.SetAsSource (gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();

			grad = new Gradient (Gradient.Type.Vertical);
			grad.Stops.Add (new Gradient.ColorStop (0, new Color (1, 1, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (0.5, new Color (0, 0, 0, 0)));
			grad.Stops.Add (new Gradient.ColorStop (1, new Color (0, 0, 0, 1)));

			grad.SetAsSource (gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();
		}
		[XmlAttributeAttribute()]
		public virtual double R {
			get { return Math.Ceiling((selectedColor as SolidColor).color.R * 255.0); }
			set {
				if (R == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (value * colDiv, c.G, c.B, c.A));
			}
		}
		[XmlAttributeAttribute()]
		public virtual double G {
			get { return Math.Ceiling((selectedColor as SolidColor).color.G * 255.0); }
			set {
				if (G == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (c.R, value * colDiv, c.B, c.A));
			}
		}
		[XmlAttributeAttribute()]
		public virtual double B {
			get { return Math.Ceiling((selectedColor as SolidColor).color.B * 255.0); }
			set {
				if (B == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (c.R, c.G, value * colDiv, c.A));
			}
		}
		[XmlAttributeAttribute()]
		public virtual double A {
			get { return Math.Ceiling((selectedColor as SolidColor).color.A * 255.0); }
			set {
				if (A == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (c.R, c.G, c.B, value * colDiv));
			}
		}

		Fill pointedColor, selectedColor;
		[XmlAttributeAttribute()][DefaultValue("White")]
		public virtual Fill PointedColor {
			get { return pointedColor; }
			set {
				if (pointedColor == value)
					return;
				pointedColor = value; 
				NotifyValueChanged ("PointedColor", pointedColor);
				string n = (pointedColor as SolidColor).color.ToString ();
				if (char.IsLetter(n[0]))
					NotifyValueChanged ("PointedColorName", n);
				else
					NotifyValueChanged ("PointedColorName", "-");
			}
		}
		[XmlAttributeAttribute()][DefaultValue("White")]
		public virtual Fill SelectedColor {
			get { return selectedColor; }
			set {
				if (selectedColor == value)
					return;
				selectedColor = value;
				NotifyValueChanged ("SelectedColor", selectedColor);
				string n = (selectedColor as SolidColor).color.ToString ();
				if (char.IsLetter(n[0]))
					NotifyValueChanged ("SelectedColorName", n);
				else
					NotifyValueChanged ("SelectedColorName", "-");
				NotifyValueChanged ("R", R);
				NotifyValueChanged ("G", G);
				NotifyValueChanged ("B", B);
				NotifyValueChanged ("A", A);
				//NotifyValueChanged ("A", sc.color.A);
			}
		} 

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			Rectangle r = ScreenCoordinates (Slot);
			Point pos = e.Position - r.Position;
			//System.Diagnostics.Debug.WriteLine ("{0} {1}", pos.X, pos.Y);
			if (bmp == null)
				return;
			PointedColor = new SolidColor(getPixelAt(pos.X, pos.Y));
		}

		Color getPixelAt(int x, int y){			
			int ptr = y * Slot.Width * 4 + x * 4;

			return new Color(
				(double)bmp[ptr + 2] * colDiv,
				(double)bmp[ptr + 1] * colDiv,
				(double)bmp[ptr] * colDiv,
				(double)bmp[ptr + 3] * colDiv);

//			return new Color(
//				1.0 / (double)bmp[ptr + 1],
//				1.0 / (double)bmp[ptr + 2],
//				1.0 / (double)bmp[ptr + 3],
//				1.0 / (double)bmp[ptr]);
		}

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{			
			base.onMouseClick (sender, e);
			SelectedColor = PointedColor;
		}
	}
}


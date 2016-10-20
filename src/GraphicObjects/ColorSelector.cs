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
		public ColorSelector (): base()
		{
		}

		const double div = 255.0;
		const double colDiv = 1.0 / div;

		Fill selectedColor;
		protected Point mousePos;
		double h,s,v;

		[XmlAttributeAttribute()]
		public virtual double R {
			get { return Math.Round((selectedColor as SolidColor).color.R * div); }
			set {
				if (R == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (value * colDiv, c.G, c.B, c.A));
				NotifyValueChanged ("R", R);
				updateHSV ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double G {
			get { return Math.Round((selectedColor as SolidColor).color.G * div); }
			set {
				if (G == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (c.R, value * colDiv, c.B, c.A));
				NotifyValueChanged ("G", G);
				updateHSV ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double B {
			get { return Math.Round((selectedColor as SolidColor).color.B * div); }
			set {
				if (B == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (c.R, c.G, value * colDiv, c.A));
				NotifyValueChanged ("B", B);
				updateHSV ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double A {
			get { return Math.Round((selectedColor as SolidColor).color.A * div); }
			set {
				if (A == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (c.R, c.G, c.B, value * colDiv));
				NotifyValueChanged ("A", A);
				updateHSV ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double H {
			get { return Math.Round (h, 3); }
			set {
				if (H == value)
					return;
				h = value;
				NotifyValueChanged ("H", H);
				computeColorFromHSV ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double S {
			get { return Math.Round (s, 2); }
			set {
				if (s == value)
					return;
				s = value;
				NotifyValueChanged ("S", S);
				computeColorFromHSV ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double V {
			get { return Math.Round (v, 2); }
			set {
				if (v == value)
					return;
				v = value;
				NotifyValueChanged ("V", V);
				computeColorFromHSV ();
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
			}
		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			if (bmp == null || CurrentInterface.Mouse.LeftButton == ButtonState.Released)
				return;
			updateMouseLocalPos (e.Position);
			updateColorFromPicking ();
		}

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			base.onMouseClick (sender, e);

			updateMouseLocalPos (e.Position);
			updateColorFromPicking ();
		}
		void updateMouseLocalPos(Point mPos){
			Rectangle r = ScreenCoordinates (Slot);
			Rectangle cb = ClientRectangle;
			mousePos = mPos - r.Position;

			mousePos.X = Math.Max(cb.X, mousePos.X);
			mousePos.X = Math.Min(cb.Right-1, mousePos.X);
			mousePos.Y = Math.Max(cb.Y, mousePos.Y);
			mousePos.Y = Math.Min(cb.Bottom-1, mousePos.Y);
		}
		virtual protected void updateColorFromPicking(bool redraw = true){
			SelectedColor = new SolidColor(getPixelAt(mousePos.X, mousePos.Y));

			updateHSV ();

			NotifyValueChanged ("R", R);
			NotifyValueChanged ("G", G);
			NotifyValueChanged ("B", B);
			NotifyValueChanged ("A", A);

			if (redraw)
				RegisterForRedraw ();
		}

		protected Color getPixelAt(int x, int y){
			if (bmp == null)
				return Color.Transparent;

			int ptr = y * Slot.Width * 4 + x * 4;

			return new Color(
				(double)bmp[ptr + 2] * colDiv,
				(double)bmp[ptr + 1] * colDiv,
				(double)bmp[ptr] * colDiv,
				(double)bmp[ptr + 3] * colDiv);
		}
		void updateHSV(){
			Color c = (SelectedColor as SolidColor).color;
			double min = Math.Min (c.R, Math.Min (c.G, c.B));	//Min. value of RGB
			double max = Math.Max (c.R, Math.Max (c.G, c.B));	//Max. value of RGB
			double diff = max - min;							//Delta RGB value

			v = max;

			if ( diff == 0 )//This is a gray, no chroma...
			{
				h = 0;
				s = 0;
			}else{//Chromatic data...				
				s = diff / max;

				double diffR = (((max - c.R) / 6.0) + (diff / 2.0)) / diff;
				double diffG = (((max - c.G) / 6.0) + (diff / 2.0)) / diff;
				double diffB = (((max - c.B) / 6.0) + (diff / 2.0)) / diff;

				if (c.R == max)
					h = diffB - diffG;
				else if (c.G == max)
					h = (1.0 / 3.0) + diffR - diffB;
				else if (c.B == max)
					h = (2.0 / 3.0) + diffG - diffR;
				
				if (h < 0)
					h += 1;
				if (h > 1)
					h -= 1;
				
			}

			NotifyValueChanged ("H", H);
			NotifyValueChanged ("S", S);
			NotifyValueChanged ("V", V);
		}
		void computeColorFromHSV(){
			Color c = Color.Black;

			if (s == 0) {//HSV from 0 to 1
				c.R = v;
				c.G = v;
				c.B = v;
			}else{
				double var_h = h * 6.0;

				if (var_h == 6.0)
					var_h = 0;	//H must be < 1
				double var_i = Math.Floor( var_h );	//Or ... var_i = floor( var_h )
				double var_1 = v * ( 1.0 - s );
				double var_2 = v * (1.0 - s * (var_h - var_i));
				double var_3 = v * (1.0 - s * (1.0 - (var_h - var_i)));

				if (var_i == 0.0) {
					c.R = v;
					c.G = var_3;
					c.B = var_1;
				}else if ( var_i == 1.0 ) { c.R = var_2 ; c.G = v     ; c.B = var_1; }
				else if ( var_i == 2 ) { c.R = var_1 ; c.G = v     ; c.B = var_3; }
				else if ( var_i == 3 ) { c.R = var_1 ; c.G = var_2 ; c.B = v;     }
				else if ( var_i == 4 ) { c.R = var_3 ; c.G = var_1 ; c.B = v;    }
				else                   { c.R = v     ; c.G = var_1 ; c.B = var_2; }
			}

			SelectedColor = new SolidColor (c);

			NotifyValueChanged ("R", R);
			NotifyValueChanged ("G", G);
			NotifyValueChanged ("B", B);
			NotifyValueChanged ("A", A);
		}
	}
}


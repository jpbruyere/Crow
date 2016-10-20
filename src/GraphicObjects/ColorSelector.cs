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

		Fill pointedColor, selectedColor;
		protected Point mousePos;

		[XmlAttributeAttribute()]
		public virtual double R {
			get { return Math.Round((selectedColor as SolidColor).color.R * div); }
			set {
				if (R == value)
					return;
				Color c = (SelectedColor as SolidColor).color;
				SelectedColor = new SolidColor (new Color (value * colDiv, c.G, c.B, c.A));
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
			}
		}
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
			}
		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			if (bmp == null || CurrentInterface.Mouse.LeftButton == ButtonState.Released)
				return;
			updateMouseLocalPos (e.Position);
			updateColor ();
		}

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			base.onMouseClick (sender, e);

			updateMouseLocalPos (e.Position);
			updateColor ();
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
		virtual protected void updateColor(){
			SelectedColor = new SolidColor(getPixelAt(mousePos.X, mousePos.Y));
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

	}
}


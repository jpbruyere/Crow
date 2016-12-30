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
		protected Point mousePos;

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			if (CurrentInterface.Mouse.LeftButton == ButtonState.Released)
				return;
			updateMouseLocalPos (e.Position);
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
			if (e.Button == MouseButton.Left)
				updateMouseLocalPos (e.Position);
		}

		protected virtual void updateMouseLocalPos(Point mPos){
			Rectangle r = ScreenCoordinates (Slot);
			Rectangle cb = ClientRectangle;
			mousePos = mPos - r.Position;

			mousePos.X = Math.Max(cb.X, mousePos.X);
			mousePos.X = Math.Min(cb.Right-1, mousePos.X);
			mousePos.Y = Math.Max(cb.Y, mousePos.Y);
			mousePos.Y = Math.Min(cb.Bottom-1, mousePos.Y);
		}
//		virtual protected void updateColorFromPicking(bool redraw = true){
//			SelectedColor = new SolidColor(getPixelAt(mousePos.X, mousePos.Y));
//
//			updateHSV ();
//
//			NotifyValueChanged ("R", R);
//			NotifyValueChanged ("G", G);
//			NotifyValueChanged ("B", B);
//			NotifyValueChanged ("A", A);
//
//			if (redraw)
//				RegisterForRedraw ();
//		}
//
//		protected Color getPixelAt(int x, int y){
//			if (bmp == null)
//				return Color.Transparent;
//
//			int ptr = y * Slot.Width * 4 + x * 4;
//
//			return new Color(
//				(double)bmp[ptr + 2] * colDiv,
//				(double)bmp[ptr + 1] * colDiv,
//				(double)bmp[ptr] * colDiv,
//				(double)bmp[ptr + 3] * colDiv);
//		}
	}
}


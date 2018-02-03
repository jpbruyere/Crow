//
// ColorSelector.cs
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

namespace Crow
{
	/// <summary>
	/// simple squarred rgb color selector
	/// </summary>
	public class ColorSelector : GraphicObject
	{
		#region CTOR
		public ColorSelector () : base(){}
		public ColorSelector (Interface iface) : base(iface){}
		#endregion

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
			mousePos.X = Math.Min(cb.Right, mousePos.X);
			mousePos.Y = Math.Max(cb.Y, mousePos.Y);
			mousePos.Y = Math.Min(cb.Bottom, mousePos.Y);
		}
	}
}


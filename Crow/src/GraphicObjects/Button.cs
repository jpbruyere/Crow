//
// Button.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OpenTK.Graphics.OpenGL;

using System.Diagnostics;

using System.Xml.Serialization;
using Crow.Cairo;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// templated button control
	/// </summary>
    public class Button : TemplatedContainer
    {
		#region CTOR
		protected Button() : base() {}
		public Button (Interface iface) : base(iface){}
		#endregion

		string image;
		bool isPressed;

		public event EventHandler Pressed;
		public event EventHandler Released;

		#region GraphicObject Overrides
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			IsPressed = true;

			base.onMouseDown (sender, e);

			//TODO:remove
			NotifyValueChanged ("State", "pressed");
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			IsPressed = false;

			base.onMouseUp (sender, e);

			//TODO:remove
			NotifyValueChanged ("State", "normal");
		}
		#endregion

		[DefaultValue("#Crow.Images.button.svg")]
		public string Image {
			get { return image; }
			set {
				if (image == value)
					return;
				image = value;
				NotifyValueChanged ("Image", image);
			}
		}
		[DefaultValue(false)]
		public bool IsPressed
		{
			get { return isPressed; }
			set
			{
				if (isPressed == value)
					return;

				isPressed = value;

				NotifyValueChanged ("IsPressed", isPressed);

				if (isPressed)
					Pressed.Raise (this, null);
				else
					Released.Raise (this, null);
			}
		}
	}
}

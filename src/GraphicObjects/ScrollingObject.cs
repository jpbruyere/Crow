//
// ScrollingObject.cs
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
using System.Collections;
using Cairo;


namespace Crow
{
	/// <summary>
	/// generic class to build scrolling control in both directions
	/// </summary>
	public class ScrollingObject : GraphicObject
	{
		#region CTOR
		protected ScrollingObject ():base(){}
		public ScrollingObject (Interface iface):base(iface){}
		#endregion

		int scrollX, scrollY, maxScrollX, maxScrollY, mouseWheelSpeed;

		/// <summary>
		/// if true, key stroke are handled in derrived class
		/// </summary>
		protected bool KeyEventsOverrides = false;

		/// <summary> Horizontal Scrolling Position </summary>
		[XmlAttributeAttribute][DefaultValue(0)]
		public virtual int ScrollX {
			get { return scrollX; }
			set {
				if (scrollX == value)
					return;

				int newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScrollX)
					newS = maxScrollX;

				if (newS == scrollX)
					return;

				scrollX = value;

				NotifyValueChanged ("ScrollX", scrollX);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Vertical Scrolling Position </summary>
		[XmlAttributeAttribute][DefaultValue(0)]
		public virtual int ScrollY {
			get { return scrollY; }
			set {
				if (scrollY == value)
					return;

				int newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScrollY)
					newS = maxScrollY;

				if (newS == scrollY)
					return;

				scrollY = value;

				NotifyValueChanged ("ScrollY", scrollY);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Horizontal Scrolling maximum value </summary>
		[XmlAttributeAttribute][DefaultValue(0)]
		public virtual int MaxScrollX {
			get { return maxScrollX; }
			set {
				if (maxScrollX == value)
					return;

				maxScrollX = value;

				if (scrollX > maxScrollX)
					ScrollX = maxScrollX;
				
				NotifyValueChanged ("MaxScrollX", maxScrollX);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Vertical Scrolling maximum value </summary>
		[XmlAttributeAttribute][DefaultValue(0)]
		public virtual int MaxScrollY {
			get { return maxScrollY; }
			set {
				if (maxScrollY == value)
					return;

				maxScrollY = value;

				if (scrollY > maxScrollY)
					ScrollY = maxScrollY;

				NotifyValueChanged ("MaxScrollY", maxScrollY);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Mouse Wheel Scrolling multiplier </summary>
		[XmlAttributeAttribute][DefaultValue(1)]
		public virtual int MouseWheelSpeed {
			get { return mouseWheelSpeed; }
			set {
				if (mouseWheelSpeed == value)
					return;
				
				mouseWheelSpeed = value;

				NotifyValueChanged ("MouseWheelSpeed", mouseWheelSpeed);
			}
		}

		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{
			base.onMouseWheel (sender, e);
			if (IFace.Keyboard.IsKeyDown (Key.ShiftLeft))
				ScrollX += e.Delta * MouseWheelSpeed;
			else
				ScrollY -= e.Delta * MouseWheelSpeed;
		}
		/// <summary> Process scrolling with arrow keys, home and end keys. </summary>
		public override void onKeyDown (object sender, KeyboardKeyEventArgs e)
		{
			base.onKeyDown (sender, e);

			if (KeyEventsOverrides)
				return;

			switch (e.Key) {
			case Key.Up:
				ScrollY--;
				break;
			case Key.Down:
				ScrollY++;
				break;
			case Key.Left:
				ScrollX--;
				break;
			case Key.Right:
				ScrollX++;
				break;
			case Key.Home:
				ScrollX = 0;
				ScrollY = 0;
				break;
			case Key.End:
				ScrollX = MaxScrollX;
				ScrollY = MaxScrollY;
				break;
			}
		}
	}
}


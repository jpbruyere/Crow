#region License
//
// MouseEventArgs.cs
//
// Author:
//       Stefanos A. <stapostol@gmail.com>
//
// Copyright (c) 2006-2014 Stefanos Apostolopoulos
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
//
#endregion

using System;
using Glfw;

namespace Crow
{
	public class CrowEventArgs : EventArgs
	{
		public bool Handled;
	}
	public class MouseEventArgs : CrowEventArgs
	{
		public readonly int X, Y;
		public Point Position => new Point (X, Y);
		public MouseEventArgs () { }
		public MouseEventArgs (int x, int y) {
			X = x;
			Y = y;
		}
	}
	public class MouseMoveEventArgs : MouseEventArgs
	{
		public readonly int XDelta, YDelta;
		public MouseMoveEventArgs () { }
		public MouseMoveEventArgs (int x, int y, int xDelta, int yDelta) : base (x, y)
		{
			XDelta = xDelta;
			YDelta = yDelta;
		}
	}

	public class MouseButtonEventArgs : MouseEventArgs 
	{
		public readonly MouseButton Button;
		public readonly InputAction Action;
		public MouseButtonEventArgs (int x, int y, MouseButton button, InputAction action) : base (x, y)
		{
			Button = button;
			Action = action;
		}
	}
	public class MouseWheelEventArgs : MouseEventArgs
	{
		public readonly int Delta;
		public MouseWheelEventArgs (int delta)
		{
			Delta = delta;
		}
	}
}

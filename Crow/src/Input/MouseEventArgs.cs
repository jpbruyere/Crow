// Copyright (c) 2006-2014 Stefanos Apostolopoulos <stapostol@gmail.com>
// Copyright (c) 2014-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using Glfw;

namespace Crow
{
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

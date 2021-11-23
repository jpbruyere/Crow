// Copyright (c) 2006-2014 Stefanos Apostolopoulos <stapostol@gmail.com>
// Copyright (c) 2014-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Glfw;
using Drawing2D;

namespace Crow
{
	[Flags]
	public enum DeviceEventType {
		None			= 0x00,
		MouseMove		= 0x01,
		ButtonDown		= 0x02,
		ButtonUp		= 0x04,
		Buttons			= ButtonDown | ButtonUp,
		MouseClick		= 0x08,
		MouseWheel		= 0x10,
		Mouse			= Buttons | MouseWheel | MouseClick | MouseMove,
		KeyDown			= 0x20,
		KeyUp			= 0x40,
		KeyPress		= 0x80,
		Keyboard		= KeyDown | KeyUp | KeyPress,
		All				= Mouse | Keyboard
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
		public MouseEventArgs (Point mousePosition) {
			X = mousePosition.X;
			Y = mousePosition.Y;
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
		public MouseWheelEventArgs (int delta, Point mousePosition) : base (mousePosition)
		{
			Delta = delta;
		}
	}
}

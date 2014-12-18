using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
{
    public enum MouseButtonStates
    {
        Released,
        Pressed
    }

    public static class Mouse
    {
        static Point _position;
        static Point _lastPosition;
        static Point _delta;
		static int _wheelDelta;

        public static Point Position
        {
            get { return _position; }
            set
            {
                _lastPosition = _position;
                _position = value;
                _delta = new Point(_lastPosition.X - _position.X, _lastPosition.Y - _position.Y);
            }
        }
        public static Point LastPosition
        {
            get { return _lastPosition; }
        }
        public static Point Delta
        {
            get { return _delta; }
        }
		public static int WheelDelta {
			get { return _wheelDelta; }
			set { _wheelDelta = value; }
		}

		public static int X
		{
			get { return _position.X; }			
		}
		public static int Y
		{
			get { return _position.Y; }
		}

        public static MouseButtonStates LeftButton = MouseButtonStates.Released;
        public static MouseButtonStates RightButton = MouseButtonStates.Released;
        public static MouseButtonStates MiddleButton = MouseButtonStates.Released;

    }
}

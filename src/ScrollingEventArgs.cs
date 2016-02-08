using System;

namespace Crow
{
	public class ScrollingEventArgs: EventArgs
	{
		public Orientation  Direction;

		public ScrollingEventArgs (Orientation  _direction) : base()
		{
			Direction = _direction;
		}
	}
}


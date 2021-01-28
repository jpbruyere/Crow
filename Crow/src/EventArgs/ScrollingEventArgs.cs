// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
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


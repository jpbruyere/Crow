// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow
{
	/// <summary>
	/// Occurs when the mouse cursor changes.
	/// </summary>
	public class MouseCursorChangedEventArgs : EventArgs
	{
		public MouseCursor NewCursor;
		public MouseCursorChangedEventArgs (MouseCursor newCursor) : base()
		{
			NewCursor = newCursor;
		}
	}
}

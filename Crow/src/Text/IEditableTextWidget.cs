// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif

namespace Crow
{
	/// <summary>
	/// Implement this interface to have a blinking cursor for a widget
	/// with editable text ability.
	/// </summary>
	public interface IEditableTextWidget
	{
		/// <summary>
		/// Draw text cursor in widget with screen coordinates. This interface when implemented is called
		/// automatically by the Interface to make the cursor blink. The blinking frequency is controlled by
		/// Interface.TEXT_CURSOR_BLINK_FREQUENCY static field.
		/// </summary>
		/// <param name="ctx">The master interface context on which to draw the cursor</param>
		/// <param name="rect">Return a clipping rectangle containing the cursor position to be cleared when needed</param>
		/// <returns>True if cursor were drawed, false otherwise</returns>
		bool DrawCursor (Context ctx, out Rectangle rect);		
	}
}
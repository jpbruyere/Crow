// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Text;
using System;

namespace Crow
{
	/// <summary>
	/// Occurs in the TextBox widget and Label when the current selected text has changed.
	/// </summary>
	public class TextSelectionChangeEventArgs: EventArgs
	{
		/// <summary>
		/// The text span of the current selection.
		/// </summary>
		public TextSpan Selection;

		public TextSelectionChangeEventArgs (TextSpan newSelection) : base()
		{
			Selection = newSelection;
		}
	}
}


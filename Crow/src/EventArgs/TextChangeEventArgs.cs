// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Text;
using System;

namespace Crow
{
	/// <summary>
	/// Occurs in the TextBox widget when the text has changed.
	/// </summary>
	public class TextChangeEventArgs: EventArgs
	{
		/// <summary>
		/// The TextChange structure representing the change.
		/// </summary>
		public TextChange Change;

		public TextChangeEventArgs (TextChange _newValue) : base()
		{
			Change = _newValue;
		}
	}
}


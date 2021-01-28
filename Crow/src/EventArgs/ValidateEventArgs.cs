// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow
{
	/// <summary>
	/// Occurs in the TextBox widget when the text has changed. Contains the 
	/// validated text.
	/// </summary>
	public class ValidateEventArgs : EventArgs
	{
		/// <summary>
		/// The validated text.
		/// </summary>
		public string ValidatedText;
		public ValidateEventArgs (string _text) : base () {
			ValidatedText = _text;
		}
	}
}

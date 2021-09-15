// Copyright (c) 2006 - 2009 the Open Toolkit library.
// Copyright (c) 2014-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Glfw;

namespace Crow
{
	public class KeyEventArgs : CrowEventArgs
	{
		Key key;
		int scancode;
		Modifier modifiers;
		bool repeat;

		public KeyEventArgs (Key _key, bool _repeat = false)
		{
			key = _key;
			repeat = _repeat;
		}
		public KeyEventArgs (Key _key, int _scancode, Modifier _modifiers, bool _repeat = false)
		{
			key = _key;
			scancode = _scancode;
			modifiers = _modifiers;
			repeat = _repeat;
		}
		public KeyEventArgs (KeyEventArgs e)
		{
			key = e.Key;
			repeat = e.IsRepeat;
			scancode = e.ScanCode;
			modifiers = e.Modifiers;
		}
		public Key Key => key;
		public int ScanCode => scancode;
		public Modifier Modifiers => modifiers;
		public bool IsRepeat => repeat;
	}
}

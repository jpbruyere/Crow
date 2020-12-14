// Copyright (c) 2006 - 2009 the Open Toolkit library.
// Copyright (c) 2014-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow
{
    /// <summary>
    /// Defines the event arguments for KeyPress events. Instances of this class are cached:
    /// KeyPressEventArgs should only be used inside the relevant event, unless manually cloned.
    /// </summary>
    public class KeyPressEventArgs : CrowEventArgs
    {
        char key_char;
        
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="keyChar">The ASCII character that was typed.</param>
        public KeyPressEventArgs(char keyChar)
        {
            KeyChar = keyChar;
        }

        /// <summary>
        /// Gets a <see cref="System.Char"/> that defines the ASCII character that was typed.
        /// </summary>
        public char KeyChar
        {
            get { return key_char; }
            internal set { key_char = value; }
        }
    }
}

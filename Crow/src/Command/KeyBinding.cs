// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using EnumsNET;
using Glfw;

namespace Crow
{
    public class KeyBinding
    {
		public readonly Key Key;
   		public readonly Modifier Modifiers;
		public KeyBinding (Key key, Modifier modifiers = 0) {
            Key = key;
            Modifiers = modifiers;
        }
        public KeyBinding Parse (string str) {
            string[] tmp = str.Split (',');
            if (Enums.TryParse<Key> (tmp[0], out Key k)) {
                if (tmp.Length > 1 && FlagEnums.TryParseFlags<Modifier> (tmp[1], out Modifier mods))
                    return new KeyBinding (k, mods);
                else
                    return new KeyBinding (k);
            }
            return default;
        }
        public override string ToString () => $"{FlagEnums.FormatFlags (Modifiers, "+")} + {Key}";
    }
}
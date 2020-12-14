// Copyright (c) 2006 - 2009 the Open Toolkit library.
// Copyright (c) 2014-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Glfw;

namespace Crow
{
	public class KeyEventArgs : CrowEventArgs
    {
		int keyCode=0;
		Key key;
		bool repeat;	

		public KeyEventArgs(Key _key, bool _repeat)
		{
			key = _key;
			repeat = _repeat;
		}
		public KeyEventArgs(KeyEventArgs args)
		{
		    Key = args.Key;
		}
		public Key Key
		{
		    get { return key; }
		    internal set { key = value; }
		}
		public uint ScanCode
		{
		    get { return (uint)keyCode; }
		}
		public bool IsRepeat
		{
		    get { return repeat; }
		    internal set { repeat = value; }
		}
    }
}

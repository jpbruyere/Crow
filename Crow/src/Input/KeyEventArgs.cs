#region License
//
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2009 the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
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

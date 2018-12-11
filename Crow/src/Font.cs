﻿//
// Font.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Cairo;

namespace Crow
{
	public enum FontStyle
	{
		Normal,
		Bold,
		Italic,
		Underlined
	}
	public class Font
	{
		#region CTOR
		public Font ()
		{
		}
		#endregion

		string _name = "sans";
		int _size = 10;
		FontStyle _style = FontStyle.Normal;

		public string Name {
			get { return _name; }
			set { _name = value; }
		}
		public int Size {
			get { return _size; }
			set { _size = value; }
		}
		public FontStyle Style {
			get { return _style; }
			set { _style = value; }
		}

		public FontSlant Slant {
			get{ 
				switch (Style) {
				case FontStyle.Normal:
				default:
					return FontSlant.Normal;
				case FontStyle.Italic:
					return FontSlant.Italic;
				}			
			}
		}
		public FontWeight Wheight {
			get{ 
				switch (Style) {
				case FontStyle.Bold:
					return FontWeight.Bold;
				case FontStyle.Italic:
				case FontStyle.Normal:
				default:
					return FontWeight.Normal;
				}			
			}
		}

		#region Operators
		public static implicit operator string(Font c)
		{
			return c.ToString();
		}
		public static implicit operator Font(string s)
		{
			Font f = new Font ();

			if (!string.IsNullOrEmpty (s)) {
				string[] c = s.TrimStart().TrimEnd().Split (new char[] { ',' });

				if (c.Length == 2)
					f.Size = int.Parse (c [1].TrimStart());

				string[] n = c [0].TrimEnd().Split (new char[] { ' ' });

				f.Name = n [0];

				if (n.Length > 1)
					f.Style = (FontStyle)Enum.Parse (typeof(FontStyle), n[n.Length-1], true);
			}

			return f;
		}
		#endregion

		public override string ToString()
		{
			if (_style == FontStyle.Normal)
				return string.Format("{0},{1}", _name, _size);
			else
				return string.Format("{0} {1},{2}", _name, _style, _size);

		}

		public static object Parse(string s)
		{
			return (Font)s;
		}
	}
}


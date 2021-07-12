// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Cairo;
//using FastEnumUtility;

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
				case FontStyle.Italic:
					return FontSlant.Italic;
				default:
					return FontSlant.Normal;
				}
			}
		}
		public FontWeight Wheight {
			get{ 
				switch (Style) {
				case FontStyle.Bold:
					return FontWeight.Bold;
				default:
					return FontWeight.Normal;
				}			
			}
		}

		#region Operators
		public static implicit operator string(Font c) => c.ToString();
		
		public static implicit operator Font(string s) => (Font)Parse(s);
		#endregion

		public override string ToString () =>
			_style == FontStyle.Normal ? $"{_name},{_size}" : $"{_name} {_style},{_size}";

		public static Font Parse(string s)
		{
			Font f = new Font ();
			ReadOnlySpan<char> tmp = s.AsSpan ().Trim ();
			if (tmp.Length > 0) {
				int ioc = tmp.IndexOf (',');

				if (ioc >= 0) {
					f.Size = int.Parse (tmp.Slice (ioc + 1).Trim ());
					tmp = tmp.Slice (0, ioc).TrimEnd ();
				}

				ioc = tmp.IndexOf (' ');

				if (ioc < 0)
					f.Name = tmp.ToString ();
				else {
					f.Name = tmp.Slice (0, ioc).ToString ();
					f.Style = EnumsNET.Enums.Parse<FontStyle> (tmp.Slice (ioc + 1).ToString (), true);
				}
			}
			return f;			
		}
	}
}


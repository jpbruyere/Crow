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

		string _name = "droid";
		int _size = 12;
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


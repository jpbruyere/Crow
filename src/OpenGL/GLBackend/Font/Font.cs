using System;


namespace go.GLBackend
{
	public class Font
	{
		public Font ()
		{
		}

		public Font (string name, FontSlant fs, FontWeight fw)
		{
			_name = name;
			switch (fs) {
			case FontSlant.Italic:
				Style |= FontStyle.Italic;
				break;
			case FontSlant.Oblique:
				Style |= FontStyle.Oblique;
				break;
			}
			if (fw == FontWeight.Bold)
				Style |= FontStyle.Bold;
		}

		string _name = "droid";
		FontStyle _style = FontStyle.Normal;
		FontFlag _flags = FontFlag.None;

		int _size = 10;

		public string Name {
			get { return _name; }
			set { _name = value; }
		}
		public FontStyle Style {
			get { return _style; }
			set { _style = value; }
		}
		public FontFlag Flags {
			get { return _flags; }
			set { _flags = value; }
		}
		public FontSlant Slant {
			get{ 
				if ((Style & FontStyle.Italic) == FontStyle.Italic)
					return FontSlant.Italic;
				if ((Style & FontStyle.Oblique) == FontStyle.Oblique)
					return FontSlant.Oblique;
				return FontSlant.Normal;
			}
		}
		public FontWeight Wheight {
			get{ return (Style & FontStyle.Bold) == FontStyle.Bold ? FontWeight.Bold : FontWeight.Normal; }
		}
		public int Size {
			get { return _size; }
			set { _size = value; }
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
				f.Name = "";
				string[] attribs = s.TrimStart().TrimEnd().Split(',');
				for (int i = 0; i < attribs.Length; i++) {
					FontFlag fl;
					FontStyle fs;
					int sz;
					if (Enum.TryParse<FontFlag> (attribs [i], true, out fl))
						f.Flags |= fl;
					else if (Enum.TryParse<FontStyle> (attribs [i], true, out fs))
						f.Style |= fs;
					else if (int.TryParse (attribs [i], out sz))
						f.Size = sz;
					else
						f.Name += attribs[i] + " ";
				}
				f.Name = f.Name.Trim();
			}

			return f;
		}
		#endregion
		public string FontFaceString
		{
			get {
				string tmp = Name;
				if (Style != FontStyle.Normal)
					tmp += "," + Style.ToString ();
				if (Flags != FontFlag.None)
					tmp += "," + Flags.ToString ();
				return tmp;
			}
		}
		public override string ToString()
		{
			return string.Format("{0},{1}", FontFaceString, _size);
		}

		public static object Parse(string s)
		{
			return (Font)s;
		}
	}
}


// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

namespace Crow
{
    public struct Size
    {
		public static Size Zero => new Size (0, 0);

		public int Width, Height;

		#region CTOR
		public Size (int width, int height)
		{
			Width = width;
			Height = height;
		}
		public Size (int size)
		{
			Width = size;
			Height = size;
		}
		#endregion

		#region operators
		public static implicit operator Rectangle(Size s)=> new Rectangle (s);
        public static implicit operator Size(int i)=> new Size(i, i);
		public static implicit operator string(Size s)=> s.ToString ();
		public static implicit operator Size(string s)=> string.IsNullOrEmpty (s) ? Zero : Parse (s);

		public static bool operator == (Size s1, Size s2) => (s1.Width == s2.Width && s1.Height == s2.Height);
		public static bool operator != (Size s1, Size s2) => (s1.Width != s2.Width || s1.Height != s2.Height);
		public static bool operator > (Size s1, Size s2) => (s1.Width > s2.Width && s1.Height > s2.Height);
		public static bool operator >= (Size s1, Size s2) => (s1.Width >= s2.Width && s1.Height >= s2.Height);
		public static bool operator < (Size s1, Size s2) => (s1.Width < s2.Width) ? s1.Height <= s2.Height :
																(s1.Width == s2.Width && s1.Height < s2.Height);
		public static bool operator < (Size s, int i) => s.Width < i && s.Height < i;
		public static bool operator <= (Size s, int i) => s.Width <= i && s.Height <= i;
		public static bool operator > (Size s, int i) => s.Width > i && s.Height > i;
		public static bool operator >= (Size s, int i) => s.Width >= i && s.Height >= i;
		public static bool operator <= (Size s1, Size s2) => (s1.Width <= s2.Width && s1.Height <= s2.Height);
		public static bool operator == (Size s, int i) => (s.Width == i && s.Height == i);
		public static bool operator != (Size s, int i) => (s.Width != i || s.Height != i);
		public static Size operator + (Size s1, Size s2) => new Size (s1.Width + s2.Width, s1.Height + s2.Height);
		public static Size operator + (Size s, int i) => new Size (s.Width + i, s.Height + i);
		public static Size operator * (Size s, int i) => new Size (s.Width * i, s.Height * i);
		public static Size operator / (Size s, int i) => new Size (s.Width / i, s.Height / i);
		#endregion

		public override int GetHashCode ()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + Width.GetHashCode();
				hash = hash * 23 + Height.GetHashCode();
				return hash;
			}
		}
		public override bool Equals (object obj) => (obj == null || obj.GetType () != typeof (Size)) ? false : this == (Size)obj;
		public override string ToString () => $"{Width},{Height}";
		public static Size Parse(string s)
		{
			string[] d = s.Split(',');
			return d.Length == 1 ? new Size(int.Parse(d[0])) : new Size(
				int.Parse(d[0]),
				int.Parse(d[1]));
		}
	}
}

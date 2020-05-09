// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

namespace Crow
{
	public struct SizeD {
		public static SizeD Zero => new SizeD (0, 0);

		public double Width, Height;

		#region CTOR
		public SizeD (double width, double height)
		{
			Width = width;
			Height = height;
		}
		public SizeD (double size)
		{
			Width = size;
			Height = size;
		}
		#endregion

		#region operators
		public static implicit operator RectangleD (SizeD s) => new RectangleD (s);
		public static implicit operator SizeD (double i) => new SizeD (i, i);
		public static implicit operator string (SizeD s) => s.ToString ();
		public static implicit operator SizeD (string s) => string.IsNullOrEmpty (s) ? Zero : Parse (s);

		public static bool operator == (SizeD s1, SizeD s2) => (s1.Width == s2.Width && s1.Height == s2.Height);
		public static bool operator != (SizeD s1, SizeD s2) => (s1.Width == s2.Width && s1.Height == s2.Height);
		public static bool operator > (SizeD s1, SizeD s2) => (s1.Width > s2.Width && s1.Height > s2.Height);
		public static bool operator >= (SizeD s1, SizeD s2) => (s1.Width >= s2.Width && s1.Height >= s2.Height);
		public static bool operator < (SizeD s1, SizeD s2) => (s1.Width < s2.Width) ? s1.Height <= s2.Height :
																(s1.Width == s2.Width && s1.Height < s2.Height);
		public static bool operator < (SizeD s, double i) => s.Width < i && s.Height < i;
		public static bool operator <= (SizeD s, double i) => s.Width <= i && s.Height <= i;
		public static bool operator > (SizeD s, double i) => s.Width > i && s.Height > i;
		public static bool operator >= (SizeD s, double i) => s.Width >= i && s.Height >= i;
		public static bool operator <= (SizeD s1, SizeD s2) => (s1.Width <= s2.Width && s1.Height <= s2.Height);
		public static bool operator == (SizeD s, double i) => (s.Width == i && s.Height == i);
		public static bool operator != (SizeD s, double i) => (s.Width == i && s.Height == i);
		public static SizeD operator + (SizeD s1, SizeD s2) => new SizeD (s1.Width + s2.Width, s1.Height + s2.Height);
		public static SizeD operator + (SizeD s, double i) => new SizeD (s.Width + i, s.Height + i);
		public static SizeD operator * (SizeD s, double i) => new SizeD (s.Width * i, s.Height * i);
		public static SizeD operator / (SizeD s, double i) => new SizeD (s.Width / i, s.Height / i);
		#endregion

		public override int GetHashCode ()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + Width.GetHashCode ();
				hash = hash * 23 + Height.GetHashCode ();
				return hash;
			}
		}
		public override bool Equals (object obj) => (obj == null || obj.GetType () != typeof (SizeD)) ? false : this == (SizeD)obj;
		public override string ToString () => $"{Width},{Height}";
		public static SizeD Parse (string s)
		{
			string [] d = s.Split (new char [] { ',' });
			return d.Length == 1 ? new SizeD (double.Parse (d [0])) : new SizeD (
				double.Parse (d [0]),
				double.Parse (d [1]));
		}
	}
}

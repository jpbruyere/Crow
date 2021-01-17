// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
    public struct Size : IEquatable<Size>, IEquatable<int>
	{		
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
		public static implicit operator Size(string s)=> Parse (s);

		public static bool operator == (Size s1, Size s2) => s1.Equals(s2);
		public static bool operator != (Size s1, Size s2) => !s1.Equals (s2);
		public static bool operator > (Size s1, Size s2) => (s1.Width > s2.Width && s1.Height > s2.Height);
		public static bool operator >= (Size s1, Size s2) => (s1.Width >= s2.Width && s1.Height >= s2.Height);
		public static bool operator < (Size s1, Size s2) => (s1.Width < s2.Width) ? s1.Height <= s2.Height :
																(s1.Width == s2.Width && s1.Height < s2.Height);
		public static bool operator < (Size s, int i) => s.Width < i && s.Height < i;
		public static bool operator <= (Size s, int i) => s.Width <= i && s.Height <= i;
		public static bool operator > (Size s, int i) => s.Width > i && s.Height > i;
		public static bool operator >= (Size s, int i) => s.Width >= i && s.Height >= i;
		public static bool operator <= (Size s1, Size s2) => (s1.Width <= s2.Width && s1.Height <= s2.Height);
		/*public static bool operator == (Size s, int i) => (s.Width == i && s.Height == i);
		public static bool operator != (Size s, int i) => (s.Width != i || s.Height != i);*/
		public static Size operator + (Size s1, Size s2) => new Size (s1.Width + s2.Width, s1.Height + s2.Height);
		public static Size operator + (Size s, int i) => new Size (s.Width + i, s.Height + i);
		public static Size operator * (Size s, int i) => new Size (s.Width * i, s.Height * i);
		public static Size operator / (Size s, int i) => new Size (s.Width / i, s.Height / i);
		#endregion


		public bool Equals (Size other) => Width == other.Width && Height == other.Height;
		public bool Equals (int other) => Width == other && Height == other;

		public override int GetHashCode () => HashCode.Combine (Width, Height);
		public override bool Equals (object obj) => obj is Size s ? Equals(s) : false;
		public override string ToString () => $"{Width},{Height}";
		public static Size Parse(string s)
		{							
			ReadOnlySpan<char> tmp = s.AsSpan ();
			if (tmp.Length == 0)
				return default (Size);
			int ioc = tmp.IndexOf (',');
			return ioc < 0 ? new Size (int.Parse (tmp)) : new Size (
				int.Parse (tmp.Slice (0, ioc)),
				int.Parse (tmp.Slice (ioc + 1)));
		}

    }
}

// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crow
{
    public struct Point
    {
		public int X;
		public int Y;

		public Point (int x, int y)
		{
			X = x;
			Y = y;
		}

		public int Length => (int)Math.Sqrt (Math.Pow (X, 2) + Math.Pow (Y, 2));
		public double LengthD => Math.Sqrt (Math.Pow (X, 2) + Math.Pow (Y, 2));
		public Point Normalized {
			get {
				int l = Length;
				return new Point (X / l, Y / l);
			}
		}
		public static implicit operator PointD (Point p) => new PointD (p.X, p.Y);
		public static implicit operator Point (int i) => new Point (i, i);

		public static Point operator + (Point p1, Point p2) => new Point (p1.X + p2.X, p1.Y + p2.Y);
		public static Point operator + (Point p, int i) => new Point (p.X + i, p.Y + i);
		public static Point operator - (Point p1, Point p2) => new Point (p1.X - p2.X, p1.Y - p2.Y);
		public static Point operator - (Point p, int i) => new Point (p.X - i, p.Y - i);
		public static Point operator * (Point p1, Point p2) => new Point (p1.X * p2.X, p1.Y * p2.Y);
		public static Point operator * (Point p, int d) => new Point (p.X * d, p.Y * d);
		public static Point operator / (Point p1, Point p2) => new Point (p1.X / p2.X, p1.Y / p2.Y);
		public static Point operator / (Point p, int d) => new Point (p.X / d, p.Y / d);

		public static bool operator == (Point s1, Point s2) => s1.X == s2.X && s1.Y == s2.Y;
		public static bool operator == (Point s, int i) => s.X == i && s.Y == i;
		public static bool operator != (Point s1, Point s2) => !(s1.X == s2.X && s1.Y == s2.Y);
		public static bool operator != (Point s, int i) => !(s.X == i && s.Y == i);
		public static bool operator > (Point p1, Point p2) => p1.X > p2.X && p1.Y > p2.Y;
		public static bool operator > (Point s, int i) => s.X > i && s.Y > i;
		public static bool operator < (Point p1, Point p2) => p1.X < p2.X && p1.Y < p2.Y;
		public static bool operator < (Point s, int i) => s.X < i && s.Y < i;
		public static bool operator >= (Point p1, Point p2) => p1.X >= p2.X && p1.Y >= p2.Y;
		public static bool operator >= (Point s, int i) => s.X >= i && s.Y >= i;
		public static bool operator <= (Point p1, Point p2) => p1.X <= p2.X && p1.Y <= p2.Y;
		public static bool operator <= (Point s, int i) => s.X <= i && s.Y <= i;

		public override string ToString () => string.Format ("({0},{1})", X, Y);
		public override bool Equals (object obj) => obj is Point ? this == (Point)obj :
			obj is Point && (Point)this == (Point)obj;
		public static Point Parse (string s)
		{
			if (string.IsNullOrEmpty (s))
				return default (Point);
			string [] d = s.Trim ().Split (',');
			if (d.Length == 2)
				return new Point (int.Parse (d [0]), int.Parse (d [1]));
			else if (d.Length == 1) {
				int tmp = int.Parse (d [0]);
				return new Point (tmp, tmp);
			}
			throw new Exception ("Crow.PointD Parsing Error: " + s);
		}

		public override int GetHashCode ()
		{
#pragma warning disable RECS0025 // Champ autre qu’en lecture seule référencé dans « GetHashCode() »
			unchecked {
				var hashCode = 1861411795;
				hashCode = hashCode * -1521134295 + X.GetHashCode ();

				hashCode = hashCode * -1521134295 + Y.GetHashCode ();
				return hashCode;
			}
#pragma warning restore RECS0025 // Champ autre qu’en lecture seule référencé dans « GetHashCode() »	}
		}
	}
}

// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crow
{
    public struct Point : IEquatable<Point>, IEquatable<int>
    {
		public int X, Y;

		#region CTOR
		public Point (int x, int y) {
			X = x;
			Y = y;
		}
		public Point (int pos) {
			X = pos;
			Y = pos;
		}
		#endregion

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

		public static bool operator == (Point s1, Point s2) => s1.Equals (s2);
		public static bool operator != (Point s1, Point s2) => !s1.Equals (s2);
		public static bool operator == (Point s, int i) => s.Equals(i);
		public static bool operator != (Point s, int i) => !s.Equals (i);
		public static bool operator > (Point p1, Point p2) => p1.X > p2.X && p1.Y > p2.Y;
		public static bool operator > (Point s, int i) => s.X > i && s.Y > i;
		public static bool operator < (Point p1, Point p2) => p1.X < p2.X && p1.Y < p2.Y;
		public static bool operator < (Point s, int i) => s.X < i && s.Y < i;
		public static bool operator >= (Point p1, Point p2) => p1.X >= p2.X && p1.Y >= p2.Y;
		public static bool operator >= (Point s, int i) => s.X >= i && s.Y >= i;
		public static bool operator <= (Point p1, Point p2) => p1.X <= p2.X && p1.Y <= p2.Y;
		public static bool operator <= (Point s, int i) => s.X <= i && s.Y <= i;

		public bool Equals (Point other) => X == other.X && Y == other.Y;
		public bool Equals (int other) => X == other && Y == other;


		public override int GetHashCode () => HashCode.Combine (X, Y);
		public override bool Equals (object obj) => obj is Point s ? Equals (s) : false;
		public override string ToString () => $"{X},{Y}";
		public static Point Parse (string s) {
			ReadOnlySpan<char> tmp = s.AsSpan ();
			if (tmp.Length == 0)
				return default (Point);
			int ioc = tmp.IndexOf (',');
			return ioc < 0 ? new Point (int.Parse (tmp)) : new Point (
				int.Parse (tmp.Slice (0, ioc)),
				int.Parse (tmp.Slice (ioc + 1)));
		}
    }
}

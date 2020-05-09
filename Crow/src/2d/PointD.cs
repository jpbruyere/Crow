// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow {
	public struct PointD {
		public double X;
		public double Y;
		public PointD (double x, double y)
		{
			X = x;
			Y = y;
		}

		public double Length => Math.Sqrt (Math.Pow (X, 2) + Math.Pow (Y, 2));
		public PointD Normalized {
			get {
				double l = Length;
				return new PointD (X / l, Y / l);
			}
		}
		public static implicit operator Point (PointD p) => new Point ((int)Math.Round (p.X), (int)Math.Round (p.Y));
		public static implicit operator PointD (double i) => new PointD (i, i);

		public static PointD operator + (PointD p1, PointD p2) => new PointD (p1.X + p2.X, p1.Y + p2.Y);
		public static PointD operator + (PointD p, double i) => new PointD (p.X + i, p.Y + i);
		public static PointD operator - (PointD p1, PointD p2) => new PointD (p1.X - p2.X, p1.Y - p2.Y);
		public static PointD operator - (PointD p, double i) => new PointD (p.X - i, p.Y - i);
		public static PointD operator * (PointD p1, PointD p2) => new PointD (p1.X * p2.X, p1.Y * p2.Y);
		public static PointD operator * (PointD p, double d) => new PointD (p.X * d, p.Y * d);
		public static PointD operator / (PointD p1, PointD p2) => new PointD (p1.X / p2.X, p1.Y / p2.Y);
		public static PointD operator / (PointD p, double d) => new PointD (p.X / d, p.Y / d);

		public static bool operator == (PointD s1, PointD s2) => s1.X == s2.X && s1.Y == s2.Y;
		public static bool operator == (PointD s, double i) => s.X == i && s.Y == i;
		public static bool operator != (PointD s1, PointD s2) => !(s1.X == s2.X && s1.Y == s2.Y);
		public static bool operator != (PointD s, double i) => !(s.X == i && s.Y == i);
		public static bool operator > (PointD p1, PointD p2) => p1.X > p2.X && p1.Y > p2.Y;
		public static bool operator > (PointD s, double i) => s.X > i && s.Y > i;
		public static bool operator < (PointD p1, PointD p2) => p1.X < p2.X && p1.Y < p2.Y;
		public static bool operator < (PointD s, double i) => s.X < i && s.Y < i;
		public static bool operator >= (PointD p1, PointD p2) => p1.X >= p2.X && p1.Y >= p2.Y;
		public static bool operator >= (PointD s, double i) => s.X >= i && s.Y >= i;
		public static bool operator <= (PointD p1, PointD p2) => p1.X <= p2.X && p1.Y <= p2.Y;
		public static bool operator <= (PointD s, double i) => s.X <= i && s.Y <= i;

		public override string ToString () => string.Format ("{0},{1}", X, Y);
		public override bool Equals (object obj) => obj is PointD ? this == (PointD)obj :
			obj is Point && (Point)this == (Point)obj;
		public static PointD Parse (string s)
		{
			if (string.IsNullOrEmpty (s))
				return default (PointD);
			string [] d = s.Trim ().Split (',');
			if (d.Length == 2)
				return new PointD (double.Parse (d [0]), double.Parse (d [1]));
			else if (d.Length == 1) {
				double tmp = double.Parse (d [0]);
				return new PointD (tmp, tmp);
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
// Copyright (c) 2013-2022  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.InteropServices;

namespace Drawing2D {
	[StructLayout(LayoutKind.Sequential)]
	public struct RectangleD
	{
		public static readonly RectangleD Zero = new RectangleD (0, 0, 0, 0);

		public double X, Y, Width, Height;

		#region ctor
		public RectangleD(PointD p, Size s): this (p.X, p.Y, s.Width, s.Height) { }
		public RectangleD(SizeD s) : this (0, 0, s.Width, s.Height) { }
		public RectangleD(double x, double y, double width, double height) {
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}
		#endregion

		#region PROPERTIES
		public double Left{
			get => X;
			set { X = value; }
		}
		public double Top{
			get => Y;
			set { Y = value; }
		}
		 public double Right => X + Width;
		public double Bottom => Y + Height;
		public SizeD Size{
			get => new SizeD (Width, Height);
			set {
				Width = value.Width;
				Height = value.Height;
			}
		}
		public PointD Position{
			get => new PointD (X, Y);
			set {
				X = value.X;
				Y = value.Y;
			}
		}
		public PointD TopLeft{
			get => new PointD (X, Y);
			set {
				X = value.X;
				Y = value.Y;
			}
		}
		public PointD TopRight => new PointD (Right, Y);
		public PointD BottomLeft => new PointD (X, Bottom);
		public PointD BottomRight => new PointD (Right, Bottom);
		public PointD Center => new PointD (Left + Width / 2, Top + Height / 2);
		public PointD CenterD => new PointD (Left + Width / 2.0, Top + Height / 2.0);

		#endregion

		#region FUNCTIONS
		public void Inflate(double xDelta, double yDelta)
		{
			this.X -= xDelta;
			this.Width += 2 * xDelta;
			this.Y -= yDelta;
			this.Height += 2 * yDelta;
		}
		public void Inflate(double delta)
		{
			Inflate (delta, delta);
		}
		public RectangleD Inflated (double delta) => Inflated (delta, delta);
		public RectangleD Inflated (double deltaX, double deltaY) {
			RectangleD r = this;
			r.Inflate (deltaX, deltaY);
			return r;
		}
		public bool ContainsOrIsEqual (PointD p) => (p.X >= X && p.X <= X + Width && p.Y >= Y && p.Y <= Y + Height);
		public bool ContainsOrIsEqual (RectangleD r) => r.TopLeft >= this.TopLeft && r.BottomRight <= this.BottomRight;
		public bool Intersect(RectangleD r)
		{
			double maxLeft = Math.Max(this.Left, r.Left);
			double minRight = Math.Min(this.Right, r.Right);
			double maxTop = Math.Max(this.Top, r.Top);
			double minBottom = Math.Min(this.Bottom, r.Bottom);

			return (maxLeft < minRight) && (maxTop < minBottom);
		}
		public RectangleD Intersection(RectangleD r)
		{
			RectangleD result = new RectangleD();

			if (r.Left >= Left)
				result.Left = r.Left;
			else
				result.TopLeft = TopLeft;

			if (r.Right >= Right)
				result.Width = Right - result.Left;
			else
				result.Width = r.Right - result.Left;

			if (r.Top >= Top)
				result.Top = r.Top;
			else
				result.Top = Top;

			if (r.Bottom >= Bottom)
				result.Height = Bottom - result.Top;
			else
				result.Height = r.Bottom - result.Top;

			return result;
		}
		#endregion

		#region operators
		public static RectangleD operator +(RectangleD r1, RectangleD r2)
		{
			double x = Math.Min(r1.X, r2.X);
			double y = Math.Min(r1.Y, r2.Y);
			double x2 = Math.Max(r1.Right, r2.Right);
			double y2 = Math.Max(r1.Bottom, r2.Bottom);
			return new RectangleD(x, y, x2 - x, y2 - y);
		}
		public static RectangleD operator + (RectangleD r, PointD p) => new RectangleD (r.X + p.X, r.Y + p.Y, r.Width, r.Height);
		public static RectangleD operator - (RectangleD r, PointD p) => new RectangleD (r.X - p.X, r.Y - p.Y, r.Width, r.Height);
		public static bool operator == (RectangleD r1, RectangleD r2) => r1.Equals (r2);
		public static bool operator != (RectangleD r1, RectangleD r2) => !r1.Equals (r2);

		public static implicit operator RectangleD(Rectangle r) => new RectangleD(r.X,r.Y,r.Width,r.Height);
		#endregion

		public override int GetHashCode () => HashCode.Combine (X, Y, Width, Height);
		public override bool Equals (object obj) => obj is RectangleD r ? Equals (r) : false;
		public bool Equals (RectangleD other) =>
			X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

		public override string ToString () => $"{X},{Y},{Width},{Height}";
		public static RectangleD Parse(string s)
		{
			string[] d = s.Split(new char[] { ',' });
			return new RectangleD(
				double.Parse(d[0]),
				double.Parse(d[1]),
				double.Parse(d[2]),
				double.Parse(d[3]));
		}
	}
}

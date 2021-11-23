// Copyright (c) 2013-2022  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.InteropServices;

namespace Drawing2D {
	[StructLayout(LayoutKind.Sequential)]
	public struct Rectangle : IEquatable<Rectangle>
	{
		public static readonly Rectangle Zero = new Rectangle (0, 0, 0, 0);

		public int X, Y, Width, Height;

		#region ctor
		public Rectangle(Point p, Size s): this (p.X, p.Y, s.Width, s.Height) { }
		public Rectangle(Size s) : this (0, 0, s.Width, s.Height) { }
		public Rectangle(int x, int y, int width, int height) {
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}
		#endregion

		#region PROPERTIES
		public int Left{
			get => X;
			set { X = value; }
		}
		public int Top{
			get => Y;
			set { Y = value; }
		}
		public int Right => X + Width;
		public int Bottom => Y + Height;
		public Size Size{
			get => new Size (Width, Height);
			set {
				Width = value.Width;
				Height = value.Height;
			}
		}
		public SizeD SizeD => new SizeD (Width, Height);
		public Point Position{
			get => new Point (X, Y);
			set {
				X = value.X;
				Y = value.Y;
			}
		}
		public Point TopLeft{
			get => new Point (X, Y);
			set {
				X = value.X;
				Y = value.Y;
			}
		}
		public Point TopRight => new Point (Right, Y);
		public Point BottomLeft => new Point (X, Bottom);
		public Point BottomRight => new Point (Right, Bottom);
		public Point Center => new Point (Left + Width / 2, Top + Height / 2);
		public Point CenterD => new PointD (Left + Width / 2.0, Top + Height / 2.0);

		#endregion

		#region FUNCTIONS
		public void Inflate(int xDelta, int yDelta)
		{
			this.X -= xDelta;
			this.Width += 2 * xDelta;
			this.Y -= yDelta;
			this.Height += 2 * yDelta;
		}
		public void Inflate(int delta)
		{
			Inflate (delta, delta);
		}
		public Rectangle Inflated (int delta) => Inflated (delta, delta);
		public Rectangle Inflated (int deltaX, int deltaY) {
			Rectangle r = this;
			r.Inflate (deltaX, deltaY);
			return r;
		}
		public void Scale (double factor) {
			X = (int)Math.Round(factor * X);
			Y = (int)Math.Round(factor * Y);
			Width = (int)Math.Round(factor * Width);
			Height = (int)Math.Round(factor * Height);
		}
		public Rectangle Scaled (double factor) {
			return new Rectangle (
				(int)Math.Round(factor * X),
				(int)Math.Round(factor * Y),
				(int)Math.Round(factor * Width),
				(int)Math.Round(factor * Height));
		}
		public RectangleD ScaledD (double factor) {
			return new RectangleD (
				factor * X,
				factor * Y,
				factor * Width,
				factor * Height);
		}
		public bool ContainsOrIsEqual (Point p) => (p.X >= X && p.X <= X + Width && p.Y >= Y && p.Y <= Y + Height);
		public bool ContainsOrIsEqual (Rectangle r) => r.TopLeft >= this.TopLeft && r.BottomRight <= this.BottomRight;
		public bool Intersect(Rectangle r)
		{
			int maxLeft = Math.Max(this.Left, r.Left);
			int minRight = Math.Min(this.Right, r.Right);
			int maxTop = Math.Max(this.Top, r.Top);
			int minBottom = Math.Min(this.Bottom, r.Bottom);

			return (maxLeft < minRight) && (maxTop < minBottom);
		}
		public Rectangle Intersection(Rectangle r)
		{
			Rectangle result = new Rectangle();

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
		public static Rectangle operator +(Rectangle r1, Rectangle r2)
		{
			int x = Math.Min(r1.X, r2.X);
			int y = Math.Min(r1.Y, r2.Y);
			int x2 = Math.Max(r1.Right, r2.Right);
			int y2 = Math.Max(r1.Bottom, r2.Bottom);
			return new Rectangle(x, y, x2 - x, y2 - y);
		}
		public static Rectangle operator + (Rectangle r, Point p) => new Rectangle (r.X + p.X, r.Y + p.Y, r.Width, r.Height);
		public static Rectangle operator - (Rectangle r, Point p) => new Rectangle (r.X - p.X, r.Y - p.Y, r.Width, r.Height);
		public static bool operator == (Rectangle r1, Rectangle r2) => r1.Equals (r2);
		public static bool operator != (Rectangle r1, Rectangle r2) => !r1.Equals (r2);

		public static implicit operator Rectangle (RectangleD r) => new Rectangle ((int)Math.Round(r.X), (int)Math.Round (r.Y),
			(int)Math.Round (r.Width), (int)Math.Round (r.Height));
		#endregion

		public override int GetHashCode () => HashCode.Combine (X, Y, Width, Height);
		public override bool Equals (object obj) => obj is Rectangle r ? Equals (r) : false;
		public bool Equals(Rectangle other) =>
			X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

		public override string ToString () => $"{X},{Y},{Width},{Height}";
		public static Rectangle Parse(string s)
		{
			string[] d = s.Split(new char[] { ',' });
			return new Rectangle(
				int.Parse(d[0]),
				int.Parse(d[1]),
				int.Parse(d[2]),
				int.Parse(d[3]));
		}
	}
}

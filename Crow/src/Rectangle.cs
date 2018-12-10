﻿//
// Rectangle.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

namespace Crow
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Rectangle
    {
		internal static Type TRectangle = typeof(Rectangle);

		public int X;
		public int Y;
		public int Width;
		public int Height;

		#region ctor
		public Rectangle(Point p, Size s): this (p.X, p.Y, s.Width, s.Height)
		{
		}
		public Rectangle(Size s) : this (0, 0, s.Width, s.Height)
        {
        }
        public Rectangle(int x, int y, int width, int height)
        {
			X = x;
			Y = y;
			Width = width;
			Height = height;
        }
		#endregion

		#region PROPERTIES
		[XmlIgnore]public int Left{
            get { return X; }
            set { X = value; }
        }
		[XmlIgnore]public int Top{
            get { return Y; }
            set { Y = value; }
        }
		[XmlIgnore]public int Right{
            get { return X + Width; }
        }
		[XmlIgnore]public int Bottom{
            get { return Y + Height; }
        }
		[XmlIgnore]public Size Size{
            get { return new Size(Width, Height); }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }
		[XmlIgnore]public Point Position{
			get { return new Point(X, Y); }
			set
			{
				X = value.X;
				Y = value.Y;
			}
		}
		[XmlIgnore]public Point TopLeft{
            set
            {
                X = value.X;
                Y = value.Y;
            }
            get { return new Point(X, Y); }
        }
		[XmlIgnore]public Point TopRight{
            get { return new Point(Right, Y); }
        }
		[XmlIgnore]public Point BottomLeft{
            get { return new Point(X, Bottom); }
        }
		[XmlIgnore]public Point BottomRight{
            get { return new Point(Right, Bottom); }
        }
		[XmlIgnore]public Point Center
        {
            get { return new Point(Left + Width / 2, Top + Height / 2); }
        }
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
		public Rectangle Inflated (int delta) {
			return Inflated (delta,delta);
		}
		public Rectangle Inflated (int deltaX, int deltaY) {
			Rectangle r = this;
			r.Inflate (deltaX, deltaY);
			return r;
		}
        public bool ContainsOrIsEqual(Point p)
        {
            return (p.X >= X && p.X <= X + Width && p.Y >= Y && p.Y <= Y + Height) ?
                true : false;
        }
        public bool ContainsOrIsEqual(Rectangle r)
        {
            return r.TopLeft >= this.TopLeft && r.BottomRight <= this.BottomRight ? true : false;
        }
        public bool Intersect(Rectangle r)
        {
            int maxLeft = Math.Max(this.Left, r.Left);
            int minRight = Math.Min(this.Right, r.Right);
            int maxTop = Math.Max(this.Top, r.Top);
            int minBottom = Math.Min(this.Bottom, r.Bottom);

			return (maxLeft < minRight) && (maxTop < minBottom) ?
				true : false;
        }
        public Rectangle Intersection(Rectangle r)
        {
            Rectangle result = new Rectangle();
            
            if (r.Left >= this.Left)
                result.Left = r.Left;
            else
                result.TopLeft = this.TopLeft;

            if (r.Right >= this.Right)
                result.Width = this.Right - result.Left;
            else
                result.Width = r.Right - result.Left;

            if (r.Top >= this.Top)
                result.Top = r.Top;
            else
                result.Top = this.Top;

            if (r.Bottom >= this.Bottom)
                result.Height = this.Bottom - result.Top;
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
		public static Rectangle operator +(Rectangle r, Point p)
		{
			return new Rectangle(r.X + p.X, r.Y + p.Y, r.Width, r.Height);
		}
		public static Rectangle operator -(Rectangle r, Point p)
		{
			return new Rectangle(r.X - p.X, r.Y - p.Y, r.Width, r.Height);
		}
		public static bool operator ==(Rectangle r1, Rectangle r2)
        {
            return r1.TopLeft == r2.TopLeft && r1.Size == r2.Size ? true : false;
        }
        public static bool operator !=(Rectangle r1, Rectangle r2)
        {
            return r1.TopLeft == r2.TopLeft && r1.Size == r2.Size ? false : true;
        }
        #endregion        

		public static readonly Rectangle Zero = new Rectangle(0, 0, 0, 0);
        public static Rectangle Empty
        {
            get { return Zero; }
        }
        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3}", X, Y, Width, Height);
        }
        public static Rectangle Parse(string s)
        {
            string[] d = s.Split(new char[] { ',' });
            return new Rectangle(
                int.Parse(d[0]),
                int.Parse(d[1]),
                int.Parse(d[2]),
                int.Parse(d[3]));
        }
		public override int GetHashCode ()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + X.GetHashCode();
				hash = hash * 23 + Y.GetHashCode();
				hash = hash * 23 + Width.GetHashCode();
				hash = hash * 23 + Height.GetHashCode();
				return hash;
			}
		}
		public override bool Equals (object obj)
		{
			return (obj == null || obj.GetType() != TRectangle) ?
				false :
				this == (Rectangle)obj;
		}
    }
}

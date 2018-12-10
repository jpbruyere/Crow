//
// Point.cs
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
using System.Linq;
using System.Text;

namespace Crow
{
    public struct Point
    {
        int _x;
        int _y;

        public int X
        {
            get { return _x; }
            set { _x = value; }
        }
        public int Y
        {
            get { return _y; }
            set { _y = value; }
        }
        public Point(int x, int y)
        {
            _x = x;
            _y = y;
        }

		public int Length {
			get { return (int)Math.Sqrt (Math.Pow (_x, 2) + Math.Pow (_y, 2)); }
		}
		public double LengthD {
			get { return Math.Sqrt (Math.Pow (_x, 2) + Math.Pow (_y, 2)); }
		}
        public static implicit operator Cairo.Point(Point p)
        {
            return new Cairo.Point(p.X, p.Y);
        }
        public static implicit operator Cairo.PointD(Point p)
        {
            return new Cairo.PointD(p.X, p.Y);
        }
        public static implicit operator Point(int i)
        {
            return new Point(i, i);
        }
        public static Point operator /(Point p, int d)
        {
            return new Point(p.X / d, p.Y / d);
        }
        public static Point operator *(Point p, int d)
        {
            return new Point(p.X * d, p.Y * d);
        }
        public static Point operator /(Point p, double d)
        {
            return new Point((int)(p.X / d), (int)(p.Y / d));
        }
        public static Point operator *(Point p, double d)
        {
            return new Point((int)(p.X * d), (int)(p.Y * d));
        }
        public static Point operator +(Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }
		public static Point operator +(Point p, int i)
		{
			return new Point(p.X + i, p.Y + i);
		}
        public static Point operator -(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }
        public static bool operator >=(Point p1, Point p2)
        {
            return p1.X >= p2.X && p1.Y >= p2.Y ? true : false;
        }
        public static bool operator <=(Point p1, Point p2)
        {
            return p1.X <= p2.X && p1.Y <= p2.Y ? true : false;
        }
        public static bool operator ==(Point s, int i)
        {
            if (s.X == i && s.Y == i)
                return true;
            else
                return false;
        }
        public static bool operator !=(Point s, int i)
        {
            if (s.X == i && s.Y == i)
                return false;
            else
                return true;
        }
        public static bool operator >(Point s, int i)
        {
            if (s.X > i && s.Y > i)
                return true;
            else
                return false;
        }
        public static bool operator <(Point s, int i)
        {
            if (s.X < i && s.Y < i)
                return true;
            else
                return false;
        }
        public static bool operator ==(Point s1, Point s2)
        {
            if (s1.X == s2.X  && s1.Y == s2.Y)
                return true;
            else
                return false;
        }
        public static bool operator !=(Point s1, Point s2)
        {
            if (s1.X == s2.X && s1.Y == s2.Y)
                return false;
            else
                return true;
        }

        public override string ToString()
        {
			return string.Format("({0},{1})", X, Y);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
		public static Point Parse(string s)
		{
			if (string.IsNullOrEmpty (s))
				return default(Point);
			string[] d = s.Trim().Split(',');
			if (d.Length == 2)
				return new Point (int.Parse (d [0]), int.Parse (d [1]));
			else if (d.Length == 1) {
				int tmp = int.Parse (d [0]);
				return new Point (tmp, tmp);
			}
			throw new Exception ("Crow.Point Parsing Error: " + s);
		}
    }

}

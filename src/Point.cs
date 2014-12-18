using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
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

        public static implicit operator Cairo.Point(Point p)
        {
            return new Cairo.Point(p.X, p.Y);
        }
        public static implicit operator Cairo.PointD(Point p)
        {
            return new Cairo.PointD(p.X, p.Y);
        }
        public static implicit operator System.Drawing.Point(Point p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }
        public static implicit operator Point(System.Drawing.Point p)
        {
            return new Point(p.X, p.Y);
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
    }

}

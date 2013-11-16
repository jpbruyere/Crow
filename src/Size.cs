using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
{
    public struct Size
    {
        public static Size Zero
        { get { return new Size(0, 0); } }
        int _width;
        int _height;

        //public Size()
        //{ }
        public Size(int width, int height)
        {
            _width = width;
            _height = height;
        }
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }
        public override string ToString()
        {
            return string.Format("({0},{1})", Width, Height);
        }
        public static implicit operator Size(int i)
        {
            return new Size(i, i);
        }
        public static bool operator ==(Size s1, Size s2)
        {
            if (s1.Width == s2.Width && s1.Height == s2.Height)
                return true;
            else
                return false;
        }
        public static bool operator !=(Size s1, Size s2)
        {
            if (s1.Width == s2.Width && s1.Height == s2.Height)
                return false;
            else
                return true;
        }
        public static bool operator >(Size s1, Size s2)
        {
            if (s1.Width > s2.Width && s1.Height > s2.Height)
                return true;
            else
                return false;
        }
        public static bool operator >=(Size s1, Size s2)
        {
            if (s1.Width >= s2.Width && s1.Height >= s2.Height)
                return true;
            else
                return false;
        }
        public static bool operator <(Size s1, Size s2)
        {
            if (s1.Width < s2.Width)
                if (s1.Height <= s2.Height)
                    return true;
                else
                    return false;
            else if (s1.Width == s2.Width && s1.Height < s2.Height)
                return true;

            return false;
        }
        public static bool operator <=(Size s1, Size s2)
        {
            if (s1.Width <= s2.Width && s1.Height <= s2.Height)
                return true;
            else
                return false;
        }
        public static bool operator ==(Size s, int i)
        {
            if (s.Width == i && s.Height == i)
                return true;
            else
                return false;
        }
        public static bool operator !=(Size s, int i)
        {
            if (s.Width == i && s.Height == i)
                return false;
            else
                return true;
        }
        public static Size operator +(Size s, int i)
        {
            return new Size(s.Width + i, s.Height + i);
        }
    }

}

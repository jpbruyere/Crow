using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crow
{
    public struct Size
    {
        public static Size Zero
        { get { return new Size(0, 0); } }

        int _width;
        int _height;

        public Size(int width, int height)
        {
            _width = width;
            _height = height;
        }
		public Size(int size)
		{
			_width = size;
			_height = size;
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

		#region operators
		public static implicit operator Rectangle(Size s)
		{
			return new Rectangle (s);
		}
        public static implicit operator Size(int i)
        {
            return new Size(i, i);
        }
		public static implicit operator string(Size s)
		{
			return s.ToString ();
		}
		public static implicit operator Size(string s)
		{
			return string.IsNullOrEmpty (s) ? Size.Zero : Parse (s);
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
		public static bool operator <(Size s, int i)
		{
			return s.Width < i && s.Height < i ? true : false;
		}
		public static bool operator <=(Size s, int i)
		{
			return s.Width <= i && s.Height <= i ? true : false;
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
		public static Size operator +(Size s1, Size s2)
		{
			return new Size(s1.Width + s2.Width, s1.Height + s2.Height);
		}
        public static Size operator +(Size s, int i)
        {
            return new Size(s.Width + i, s.Height + i);
        }
		#endregion

		public override int GetHashCode ()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + _width.GetHashCode();
				hash = hash * 23 + _height.GetHashCode();
				return hash;
			}
		}
		public override bool Equals (object obj)
		{
			return (obj == null || obj.GetType() != typeof(Size)) ?
				false :
				this == (Size)obj;
		}
		public override string ToString()
		{
			return string.Format("{0},{1}", Width, Height);
		}
		public static Size Parse(string s)
		{
			string[] d = s.Split(new char[] { ';' });
			return d.Length == 1 ? new Size(int.Parse(d[0])) : new Size(
				int.Parse(d[0]),
				int.Parse(d[1]));
		}
	}

}

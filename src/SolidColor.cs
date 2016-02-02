using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;



namespace Crow
{
	public class SolidColor : Fill
    {
		Color color = Color.Transparent;
		#region CTOR
		public SolidColor(Color c)
		{
			color = c;
		}
		#endregion

		#region implemented abstract members of Fill

		public override void SetAsSource (Cairo.Context ctx, Rectangle bounds = default(Rectangle))
		{
			ctx.SetSourceRGBA (color.R, color.G, color.B, color.A);
		}
		public static object Parse(string s)
		{
			return new SolidColor((Color)s);
		}
		#endregion

		#region Operators
        public static implicit operator Color(SolidColor c)
        {
			return c.color;
        }
		public static implicit operator SolidColor(Color c)
		{
			return new SolidColor (c);
		}
		public static bool operator ==(SolidColor left, SolidColor right)
		{
			return left.color == right.color ? true : false;
		}
		public static bool operator !=(SolidColor left, SolidColor right)
		{
			return left.color == right.color ? false : true;

		}
		public override bool Equals (object obj)
		{
			if (obj is Crow.Color)
				return color == (Color)obj;
			if (obj is SolidColor)
				return color == (obj as SolidColor).color;
			return false;			
		}
//		public static bool operator ==(SolidColor c, string n)
//		{
//			return c.color.Name == n ? true : false;
//		}
//		public static bool operator !=(SolidColor c, string n)
//		{
//			return c.color.Name == n ? false : true;
//		}
//		public static bool operator ==(string n, SolidColor c)
//		{
//			return c.color.Name == n ? true : false;
//		}
//		public static bool operator !=(string n, SolidColor c)
//		{
//			return c.color.Name == n ? false : true;
//		}
		public static SolidColor operator *(SolidColor c, Double f)
		{
			return new SolidColor(new Color(c.color.R,c.color.G,c.color.B,c.color.A * f));
		}
		public static SolidColor operator +(SolidColor c1, SolidColor c2)
		{
			return new SolidColor(new Color(c1.color.R + c2.color.R,c1.color.G + c2.color.G,c1.color.B + c2.color.B,c1.color.A + c2.color.A));
		}
		public static SolidColor operator -(SolidColor c1, SolidColor c2)
		{
			return new SolidColor(new Color(c1.color.R - c2.color.R,c1.color.G - c2.color.G,c1.color.B - c2.color.B,c1.color.A - c2.color.A));
		}
		#endregion				        

		public override string ToString()
		{
			return color.ToString ();
		}
    }
}

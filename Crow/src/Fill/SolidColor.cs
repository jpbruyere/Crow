//
// SolidColor.cs
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
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;
using Crow.Cairo;



namespace Crow
{
	public class SolidColor : Fill
    {
		public Color color = Color.Transparent;
		#region CTOR
		public SolidColor(Color c)
		{
			color = c;
		}
		#endregion

		#region implemented abstract members of Fill
		public override void SetAsSource (Context ctx, Rectangle bounds = default)
		{
			ctx.SetSourceRGBA (color.R, color.G, color.B, color.A);
		}
		public static new object Parse(string s)
		{
			return new SolidColor((Color)s);
		}
		#endregion

		#region Operators
        public static implicit operator Color(SolidColor c) => c.color;        
		public static implicit operator SolidColor(Color c) => new SolidColor (c);

		public static bool operator ==(SolidColor left, SolidColor right) => left?.color == right?.color;		
		public static bool operator !=(SolidColor left, SolidColor right) => left?.color != right?.color;

		public override int GetHashCode ()
		{
			return color.GetHashCode();
		}
		public override bool Equals (object obj)
		{
			if (obj is Color)
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

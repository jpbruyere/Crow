﻿// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;


using Drawing2D;

namespace Crow
{
	public class SolidColor : Fill
    {
		public Color color = Colors.Transparent;
		#region CTOR
		public SolidColor(Color c)
		{
			color = c;
		}
		#endregion

		#region implemented abstract members of Fill
		public override void SetAsSource (Interface iFace, IContext ctx, Rectangle bounds = default)
		{
			ctx.SetSource (color.R / 255.0, color.G / 255.0, color.B / 255.0, color.A / 255.0);
		}
		public static new object Parse(string s)
		{
			return new SolidColor((Color)Color.Parse(s));
		}
		#endregion

		#region Operators
        public static implicit operator Color(SolidColor c) => c.color;        
		public static implicit operator SolidColor(Color c) => new SolidColor (c);

		public override int GetHashCode () => color.GetHashCode();
		public override bool Equals (object obj)		
			=> obj is Color c ? color.Equals (c) : obj is Colors cl ? color.Equals(cl) : obj is SolidColor sc && color.Equals (sc.color);			
		public static SolidColor operator *(SolidColor c, Double f)
			=> new SolidColor(new Color(c.color.R,c.color.G,c.color.B,c.color.A * f));
		public static SolidColor operator +(SolidColor c1, SolidColor c2)
			=> new SolidColor(new Color(c1.color.R + c2.color.R,c1.color.G + c2.color.G,c1.color.B + c2.color.B,c1.color.A + c2.color.A));
		public static SolidColor operator -(SolidColor c1, SolidColor c2)
			=> new SolidColor(new Color(c1.color.R - c2.color.R,c1.color.G - c2.color.G,c1.color.B - c2.color.B,c1.color.A - c2.color.A));
		#endregion

		public override string ToString() => color.ToString ();
    }
}

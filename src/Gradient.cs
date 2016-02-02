//
//  Gradient.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;

namespace Crow
{
	public class Gradient : Fill
	{
		public enum Type
		{
			Vertical,
			Horizontal,
			Oblic,
			Radial
		}
		public class ColorStop
		{
			public double Offset;
			public Color Color;

			public ColorStop(double offset, Color color){ 
				Offset = offset;
				Color = color;
			}
			public static object Parse(string s)
			{
				if (string.IsNullOrEmpty (s))
					return null;
				
				string[] parts = s.Trim ().Split (':');

				if (parts.Length > 2)
					throw new Exception ("too many parameters in color stop: " + s);
				
				if (parts.Length == 2)
					return new ColorStop (double.Parse (parts [0]), (Color)parts [1]);

				return new ColorStop (-1, (Color)parts [0]);
			}
		}
		public Gradient.Type GradientType = Type.Vertical;
//		public double x0;
//		public double y0;
//		public double x1;
//		public double y1;
//		public double Radius1;
//		public double Radius2;
		public List<ColorStop> Stops = new List<ColorStop>();
		public Gradient(Gradient.Type _type)
		{
			GradientType = _type;
		}

		#region implemented abstract members of Fill

		public override void SetAsSource (Cairo.Context ctx, Rectangle bounds = default(Rectangle))
		{
			Cairo.Gradient grad = null;
			switch (GradientType) {
			case Type.Vertical:
				grad = new Cairo.LinearGradient (bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
				break;
			case Type.Horizontal:
				grad = new Cairo.LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Top);
				break;
			case Type.Oblic:
				grad = new Cairo.LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
				break;
			case Type.Radial:
				throw new NotImplementedException ();
			}

			foreach (ColorStop cs in Stops)
				grad.AddColorStop (cs.Offset, cs.Color);
			
			ctx.SetSource (grad);
			grad.Dispose ();
		}
		#endregion

		public static object Parse(string s)
		{
			if (string.IsNullOrEmpty (s))
				return Color.White;

			Crow.Gradient tmp;

			string[] stops = s.Trim ().Split ('|');

			switch (stops[0].Trim()) {
			case "vgradient":
				tmp = new Gradient (Type.Vertical);
				break;
			case "hgradient":
				tmp = new Gradient (Type.Horizontal);
				break;
			case "ogradient":
				tmp = new Gradient (Type.Oblic);
				break;
			default:
				throw new Exception ("Unknown gradient type: " + stops [0]);
			}

			for (int i = 1; i < stops.Length; i++)
				tmp.Stops.Add((ColorStop)ColorStop.Parse(stops[i]));

			return tmp;
		}
	}
}


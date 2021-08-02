// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;

using Crow.Drawing;

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
					return new ColorStop (double.Parse (parts [0]), (Color)Color.Parse (parts [1]));

				return new ColorStop (-1, Color.FromIml (parts [0]));
			}
		}
		public Type GradientType = Type.Vertical;
//		public double x0;
//		public double y0;
//		public double x1;
//		public double y1;
//		public double Radius1;
//		public double Radius2;
		public List<ColorStop> Stops = new List<ColorStop>();
		public Gradient(Type _type)
		{
			GradientType = _type;
		}

		#region implemented abstract members of Fill

		public override void SetAsSource (Interface iFace, Context ctx, Rectangle bounds = default(Rectangle))
		{
			/*Cairo.Gradient grad = null;
			switch (GradientType) {
			case Type.Vertical:
				grad = new LinearGradient (bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
				break;
			case Type.Horizontal:
				grad = new LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Top);
				break;
			case Type.Oblic:
				grad = new LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
				break;
			case Type.Radial:
				throw new NotImplementedException ();
			}

			foreach (ColorStop cs in Stops) {
				if (cs == null)
					continue;
				grad.AddColorStop (cs.Offset, cs.Color);
			}
			
			ctx.SetSource (grad);
			grad.Dispose ();*/
		}
		#endregion

		public static new object Parse(string s)
		{
			if (string.IsNullOrEmpty (s))
				return Colors.White;

			Gradient tmp;

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


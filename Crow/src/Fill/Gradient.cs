// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;


using Drawing2D;

namespace Crow
{
	public class Gradient : Fill
	{
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
		public GradientType Type = GradientType.Vertical;
//		public double x0;
//		public double y0;
//		public double x1;
//		public double y1;
//		public double Radius1;
//		public double Radius2;
		public List<ColorStop> Stops = new List<ColorStop>();
		public Gradient(GradientType _type)
		{
			Type = _type;
		}

		#region implemented abstract members of Fill

		public override void SetAsSource (Interface iFace, IContext ctx, Rectangle bounds = default(Rectangle))
		{
			IGradient grad = iFace.Device.CreateGradient (Type, bounds);
			foreach (ColorStop cs in Stops) {
				if (cs == null)
					continue;
				grad.AddColorStop (cs.Offset, cs.Color);
			}

			ctx.SetSource (grad);
			grad.Dispose ();
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
				tmp = new Gradient (GradientType.Vertical);
				break;
			case "hgradient":
				tmp = new Gradient (GradientType.Horizontal);
				break;
			case "ogradient":
				tmp = new Gradient (GradientType.Oblic);
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


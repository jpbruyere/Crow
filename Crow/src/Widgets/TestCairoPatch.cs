// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Cairo;

namespace Crow
{
	public class TestCairoPatch : Widget
	{
		void computeControlPoints (
			double xc, double yc,
			double x1, double y1,
			out double x2, out double y2,
			out double x3, out double y3,
			double x4, double y4){
			double ax = x1 - xc;
			double ay = y1 - yc;
			double bx = x4 - xc;
			double byy = y4 - yc;
			double q1 = ax * ax + ay * ay;
			double q2 = q1 + ax * bx + ay * byy;
			double k2 = 4.0/3.0 * (Math.Sqrt(2.0 * q1 * q2) - q2) / (ax * byy - ay * bx);


			x2 = xc + ax - k2 * ay;
			y2 = yc + ay + k2 * ax;
			x3 = xc + bx + k2 * byy;
			y3 = yc + byy - k2 * bx;
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			double radius = Math.Min (ClientRectangle.Width, ClientRectangle.Height) / 2;

			double pi3 = Math.PI / 3.0;

			MeshPattern mp = new MeshPattern ();

			double x1 = radius,y1 = 0,
			x2 = 0, y2 = 0, x3 = 0, y3 = 0, x4 = 0, y4 = 0,
			xc = radius,yc = radius;

			double dx = Math.Sin (pi3) * radius;
			double dy = Math.Cos (pi3) * radius;

			mp.BeginPatch ();
			mp.MoveTo (xc, yc);
			mp.LineTo (x1, y1);
			x4 = xc + dx;
			y4 = yc - dy;
			computeControlPoints (xc, yc, x1, y1, out x2, out y2, out x3, out y3, x4, y4);
			mp.CurveTo (x2, y2, x3, y3, x4, y4);

			mp.SetCornerColorRGB (0, 1, 1, 1);
			mp.SetCornerColorRGB (1, 1, 0, 0);
			mp.SetCornerColorRGB (2, 1, 1, 0);

			x1 = x4;
			y1 = y4;
			y4 = yc + dy;

			computeControlPoints (xc, yc, x1, y1, out x2, out y2, out x3, out y3, x4, y4);
			mp.CurveTo (x2, y2, x3, y3, x4, y4);

			mp.SetCornerColorRGB (3, 0, 1, 0);
			mp.EndPatch ();

			x1 = x4;
			y1 = y4;
			x4 = xc;
			y4 = yc * 2.0;

			mp.BeginPatch ();
			mp.MoveTo (xc, yc);
			mp.LineTo (x1, y1);
			computeControlPoints (xc, yc, x1, y1, out x2, out y2, out x3, out y3, x4, y4);
			mp.CurveTo (x2, y2, x3, y3, x4, y4);

			mp.SetCornerColorRGB (0, 1, 1, 1);
			mp.SetCornerColorRGB (1, 0, 1, 0);
			mp.SetCornerColorRGB (2, 0, 1, 1);

			x1 = x4;
			y1 = y4;
			x4 = xc-dx;
			y4 = yc+dy;

			computeControlPoints (xc, yc, x1, y1, out x2, out y2, out x3, out y3, x4, y4);
			mp.CurveTo (x2, y2, x3, y3, x4, y4);

			mp.SetCornerColorRGB (3, 0, 0, 1);
			mp.EndPatch ();

			x1 = x4;
			y1 = y4;
			y4 = yc - dy;

			mp.BeginPatch ();
			mp.MoveTo (xc, yc);
			mp.LineTo (x1, y1);
			computeControlPoints (xc, yc, x1, y1, out x2, out y2, out x3, out y3, x4, y4);
			mp.CurveTo (x2, y2, x3, y3, x4, y4);

			mp.SetCornerColorRGB (0, 1, 1, 1);
			mp.SetCornerColorRGB (1, 0, 0, 1);
			mp.SetCornerColorRGB (2, 1, 0, 1);

			x1 = x4;
			y1 = y4;
			x4 = radius;
			y4 = 0;

			computeControlPoints (xc, yc, x1, y1, out x2, out y2, out x3, out y3, x4, y4);
			mp.CurveTo (x2, y2, x3, y3, x4, y4);

			mp.SetCornerColorRGB (3, 1, 0, 0);
			mp.EndPatch ();

			gr.SetSource (mp);
			gr.Paint ();
		}
	}
}


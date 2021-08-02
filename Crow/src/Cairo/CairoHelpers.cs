// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Drawing;

namespace Crow
{
    public static class CairoHelpers
    {		
		public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
		{
		    if (val.CompareTo(min) < 0) return min;
		    else if(val.CompareTo(max) > 0) return max;
		    else return val;
		}
		/// <summary>
		/// Convert string to utf8 (extension method)
		/// </summary>
		/// <returns>byte array with utf8 encoding</returns>
		public static byte[] ToUtf8(this String str)
		{
			return System.Text.UTF8Encoding.UTF8.GetBytes (str);
		}

        public static double min(params double[] arr)
        {
            int minp = 0;
            for (int i = 1; i < arr.Length; i++)
                if (arr[i] < arr[minp])
                    minp = i;

            return arr[minp];
        }
		public static void CairoRectangle(Context gr, RectangleD r, double radius, double stroke = 0.0)
		{
			if (radius > 0)
				DrawRoundedRectangle (gr, r, radius, stroke);
			else
				gr.Rectangle (r, stroke);
		}
		public static void CairoCircle(Context gr, RectangleD r)
		{
			gr.Arc(r.X + r.Width/2.0, r.Y + r.Height/2.0, Math.Min(r.Width,r.Height)/2.0, 0, 2.0*Math.PI);
		}
		public static void DrawRoundedRectangle(Context gr, RectangleD r, double radius, double stroke = 0.0)
        {
			if (stroke>0.0) {
				gr.LineWidth = stroke;
				double hsw = stroke / 2.0;
				DrawRoundedRectangle (gr, hsw + r.X, hsw + r.Y, r.Width - stroke, r.Height - stroke, radius);
				gr.Stroke ();
			}else
				DrawRoundedRectangle(gr, r.X, r.Y, r.Width, r.Height, radius);
        }
        public static void DrawRoundedRectangle(Context gr, double x, double y, double width, double height, double radius)
        {
            //gr.Save();

            if ((radius > height / 2) || (radius > width / 2))
                radius = min(height / 2, width / 2);

            gr.MoveTo(x, y + radius);
            gr.Arc(x + radius, y + radius, radius, Math.PI, -Math.PI / 2);
            gr.LineTo(x + width - radius, y);
            gr.Arc(x + width - radius, y + radius, radius, -Math.PI / 2, 0);
            gr.LineTo(x + width, y + height - radius);
            gr.Arc(x + width - radius, y + height - radius, radius, 0, Math.PI / 2);
            gr.LineTo(x + radius, y + height);
            gr.Arc(x + radius, y + height - radius, radius, Math.PI / 2, Math.PI);
            gr.ClosePath();
            //gr.Restore();
        }
        public static void StrokeRaisedRectangle(Context gr, Rectangle r, double width = 1)
        {
            //gr.Save();
            r.Inflate((int)-width / 2, (int)-width / 2);
            gr.LineWidth = width;
			gr.SetSource(Colors.White);
            gr.MoveTo(r.BottomLeft);
            gr.LineTo(r.TopLeft);
            gr.LineTo(r.TopRight);
            gr.Stroke();

			gr.SetSource(Colors.DarkGrey);
            gr.MoveTo(r.TopRight);
            gr.LineTo(r.BottomRight);
            gr.LineTo(r.BottomLeft);
            gr.Stroke();

            //gr.Restore();
        }
        public static void StrokeLoweredRectangle(Context gr, Rectangle r, double width = 1)
        {
            //gr.Save();
            r.Inflate((int)-width / 2, (int)-width / 2);
            gr.LineWidth = width;
			gr.SetSource(Colors.DarkGrey);
            gr.MoveTo(r.BottomLeft);
            gr.LineTo(r.TopLeft);
            gr.LineTo(r.TopRight);
            gr.Stroke();
			gr.SetSource(Colors.White);
            gr.MoveTo(r.TopRight);
            gr.LineTo(r.BottomRight);
            gr.LineTo(r.BottomLeft);
            gr.Stroke();

            //gr.Restore();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
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
		public static void CairoRectangle(Cairo.Context gr, Rectangle r, double radius)
		{
			if (radius>0)
				CairoHelpers.DrawRoundedRectangle(gr,r,radius);
			else
				gr.Rectangle (r);
		}
		public static void CairoCircle(Cairo.Context gr, Rectangle r)
		{
			gr.Arc(r.X + r.Width/2, r.Y + r.Height/2, Math.Min(r.Width,r.Height)/2, 0, 2*Math.PI);
		}
        public static void DrawRoundedRectangle(Cairo.Context gr, Rectangle r, double radius)
        {
            DrawRoundedRectangle(gr, r.X, r.Y, r.Width, r.Height, radius);
        }
        public static void DrawCurvedRectangle(Cairo.Context gr, Rectangle r)
        {
            DrawCurvedRectangle(gr, r.X, r.Y, r.Width, r.Height);
        }
        public static void DrawRoundedRectangle(Cairo.Context gr, double x, double y, double width, double height, double radius)
        {
            gr.Save();

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
            gr.Restore();
        }
        public static void DrawCurvedRectangle(Cairo.Context gr, double x, double y, double width, double height)
        {
            gr.Save();
            gr.MoveTo(x, y + height / 2);
            gr.CurveTo(x, y, x, y, x + width / 2, y);
            gr.CurveTo(x + width, y, x + width, y, x + width, y + height / 2);
            gr.CurveTo(x + width, y + height, x + width, y + height, x + width / 2, y + height);
            gr.CurveTo(x, y + height, x, y + height, x, y + height / 2);
            gr.Restore();
        }
        public static void StrokeRaisedRectangle(Cairo.Context gr, Rectangle r, double width = 1)
        {
            gr.Save();
            r.Inflate((int)-width / 2, (int)-width / 2);
            gr.LineWidth = width;
            gr.Color = Color.White;
            gr.MoveTo(r.BottomLeft);
            gr.LineTo(r.TopLeft);
            gr.LineTo(r.TopRight);
            gr.Stroke();

            gr.Color = Color.DarkGray;
            gr.MoveTo(r.TopRight);
            gr.LineTo(r.BottomRight);
            gr.LineTo(r.BottomLeft);
            gr.Stroke();

            gr.Restore();
        }
        public static void StrokeLoweredRectangle(Cairo.Context gr, Rectangle r, double width = 1)
        {
            gr.Save();
            r.Inflate((int)-width / 2, (int)-width / 2);
            gr.LineWidth = width;
            gr.Color = Color.DarkGray;
            gr.MoveTo(r.BottomLeft);
            gr.LineTo(r.TopLeft);
            gr.LineTo(r.TopRight);
            gr.Stroke();
            gr.Color = Color.White;
            gr.MoveTo(r.TopRight);
            gr.LineTo(r.BottomRight);
            gr.LineTo(r.BottomLeft);
            gr.Stroke();

            gr.Restore();
        }
    }
}

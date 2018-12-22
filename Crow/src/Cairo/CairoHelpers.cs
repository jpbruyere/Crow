//
// CairoHelpers.cs
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
		public static void CairoRectangle(Crow.Cairo.Context gr, Rectangle r, double radius, double stroke = 0.0)
		{
			if (radius > 0)
				CairoHelpers.DrawRoundedRectangle (gr, r, radius, stroke);
			else
				gr.Rectangle (r, stroke);
		}
		public static void CairoCircle(Crow.Cairo.Context gr, Rectangle r)
		{
			gr.Arc(r.X + r.Width/2, r.Y + r.Height/2, Math.Min(r.Width,r.Height)/2, 0, 2*Math.PI);
		}
		public static void DrawRoundedRectangle(Crow.Cairo.Context gr, Rectangle r, double radius, double stroke = 0.0)
        {
			if (stroke>0.0) {
				gr.LineWidth = stroke;
				double hsw = stroke / 2.0;
				DrawRoundedRectangle (gr, hsw + r.X, hsw + r.Y, (double)r.Width - stroke, (double)r.Height - stroke, radius);
				gr.Stroke ();
			}else
				DrawRoundedRectangle(gr, r.X, r.Y, r.Width, r.Height, radius);
        }
        public static void DrawRoundedRectangle(Crow.Cairo.Context gr, double x, double y, double width, double height, double radius)
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
        public static void StrokeRaisedRectangle(Crow.Cairo.Context gr, Rectangle r, double width = 1)
        {
            gr.Save();
            r.Inflate((int)-width / 2, (int)-width / 2);
            gr.LineWidth = width;
			gr.SetSourceColor(Color.White);
            gr.MoveTo(r.BottomLeft);
            gr.LineTo(r.TopLeft);
            gr.LineTo(r.TopRight);
            gr.Stroke();

			gr.SetSourceColor(Color.DarkGrey);
            gr.MoveTo(r.TopRight);
            gr.LineTo(r.BottomRight);
            gr.LineTo(r.BottomLeft);
            gr.Stroke();

            gr.Restore();
        }
        public static void StrokeLoweredRectangle(Crow.Cairo.Context gr, Rectangle r, double width = 1)
        {
            gr.Save();
            r.Inflate((int)-width / 2, (int)-width / 2);
            gr.LineWidth = width;
			gr.SetSourceColor(Color.DarkGrey);
            gr.MoveTo(r.BottomLeft);
            gr.LineTo(r.TopLeft);
            gr.LineTo(r.TopRight);
            gr.Stroke();
			gr.SetSourceColor(Color.White);
            gr.MoveTo(r.TopRight);
            gr.LineTo(r.BottomRight);
            gr.LineTo(r.BottomLeft);
            gr.Stroke();

            gr.Restore();
        }
    }
}

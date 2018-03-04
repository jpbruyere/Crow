//
// ExtensionsMethods.cs
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
using System.Linq.Expressions;

namespace Crow
{
	public static class ExtensionsMethods
	{
		#region Cairo extensions

		public static void Rectangle(this Cairo.Context ctx, Rectangle r, double stroke = 0.0)
		{
			if (stroke > 0.0) {
				ctx.LineWidth = stroke;
				double shw = stroke / 2.0;
				ctx.Rectangle (r.X + shw, r.Y + shw, r.Width - stroke, r.Height - stroke);
				ctx.Stroke ();
			}else
				ctx.Rectangle (r.X, r.Y, r.Width, r.Height);
		}
		public static double GetLength(this Cairo.PointD p){
			return Math.Sqrt (Math.Pow (p.X, 2) + Math.Pow (p.Y, 2));
		}
		public static Cairo.PointD GetPerp(this Cairo.PointD p){
			return new Cairo.PointD(-p.Y, p.X);
		}
		public static Cairo.PointD GetNormalized(this Cairo.PointD p){
			double length = p.GetLength();
			p.X /= length;
			p.Y /= length;
			return p;
		}
		public static Cairo.PointD Substract(this Cairo.PointD p1, Cairo.PointD p2){
			return new Cairo.PointD(p1.X - p2.X, p1.Y - p2.Y);
		}
		public static Cairo.PointD Add(this Cairo.PointD p1, Cairo.PointD p2){
			return new Cairo.PointD(p1.X + p2.X, p1.Y + p2.Y);
		}
		public static Cairo.PointD Multiply(this Cairo.PointD p1, double v){
			return new Cairo.PointD(p1.X * v, p1.Y * v);
		}
		public static void DrawCote(this Cairo.Context ctx, Cairo.PointD p1, Cairo.PointD p2, double stroke = 1.0)
		{
			const double arrowWidth = 4.0;
			const double arrowLength = 10.0;

			Cairo.PointD vDir = p2.Substract(p1);
			vDir = vDir.GetNormalized ();
			Cairo.PointD vPerp = vDir.GetPerp ();

			Cairo.PointD pA0 = p1.Add(vDir.Multiply(arrowLength));
			Cairo.PointD pA1 = p2.Substract(vDir.Multiply(arrowLength));

			Cairo.PointD vA = vPerp.Multiply (arrowWidth);

			ctx.MoveTo (p1);
			ctx.LineTo (pA0.Add (vA));
			ctx.LineTo (pA0.Substract (vA));
			ctx.LineTo (p1);

			ctx.MoveTo (p2);
			ctx.LineTo (pA1.Add (vA));
			ctx.LineTo (pA1.Substract (vA));
			ctx.LineTo (p2);

			ctx.Fill ();

			ctx.MoveTo (p1);
			ctx.LineTo (p2);
			ctx.LineWidth = stroke;
			ctx.Stroke ();

		}
		public static void SetSourceColor(this Cairo.Context ctx, Color c)
		{
			ctx.SetSourceRGBA(c.R,c.G,c.B,c.A);
		}
		public static void AddColorStop(this Cairo.Gradient grad, double offset, Color c)
		{
			grad.AddColorStop (offset, new Cairo.Color (c.R, c.G, c.B, c.A));
		}
		#endregion

		public static Orientation GetOrientation(this Alignment a){
			return (a==Alignment.Left) ||(a==Alignment.Right) ? Orientation.Horizontal : Orientation.Vertical;
		}
		public static void Raise(this EventHandler handler, object sender, EventArgs e)
		{
			if(handler != null)
			{
				handler(sender, e);
			}
		}
		public static void Raise<T>(this EventHandler<T> handler, object sender, T e)
		{
			if(handler != null)
			{
				handler(sender, e);
			}
		}

		public static bool IsWhiteSpaceOrNewLine (this char c)
		{
			return c == '\t' || c == '\r' || c == '\n' || char.IsWhiteSpace (c);
		}
		public static object GetDefaultValue(this object obj)
		{			
			Type t = obj.GetType ();
			if (t.IsValueType)
				return Activator.CreateInstance (t);
			
			return null;
		}
	}
}


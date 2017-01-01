//
//  ExtensionsMethods.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
	}
}


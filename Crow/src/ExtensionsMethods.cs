// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Linq;
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
		public static double GetLength(this PointD p){
			return Math.Sqrt (Math.Pow (p.X, 2) + Math.Pow (p.Y, 2));
		}
		public static PointD GetPerp(this PointD p){
			return new PointD(-p.Y, p.X);
		}
		public static PointD GetNormalized(this PointD p){
			double length = p.GetLength();
			p.X /= length;
			p.Y /= length;
			return p;
		}
		public static PointD Substract(this PointD p1, PointD p2){
			return new PointD(p1.X - p2.X, p1.Y - p2.Y);
		}
		public static PointD Divide(this PointD p1, double d){
			return new PointD(p1.X / d, p1.Y / d);
		}
		public static PointD Add(this PointD p1, PointD p2){
			return new PointD(p1.X + p2.X, p1.Y + p2.Y);
		}
		public static PointD Multiply(this PointD p1, double v){
			return new PointD(p1.X * v, p1.Y * v);
		}
		public static void DrawCote(this Cairo.Context ctx, PointD p1, PointD p2,
			double stroke = 1.0, bool fill = false, double arrowWidth = 3.0, double arrowLength = 7.0)
		{			
			PointD vDir = p2.Substract(p1);
			vDir = vDir.GetNormalized ();
			PointD vPerp = vDir.GetPerp ();

			PointD pA0 = p1.Add(vDir.Multiply(arrowLength));
			PointD pA1 = p2.Substract(vDir.Multiply(arrowLength));

			PointD vA = vPerp.Multiply (arrowWidth);

			ctx.MoveTo (p1);
			ctx.LineTo (pA0.Add (vA));
			if (fill)
				ctx.LineTo (pA0.Substract (vA));
			else
				ctx.MoveTo (pA0.Substract (vA));
			
			ctx.LineTo (p1);

			ctx.MoveTo (p2);
			ctx.LineTo (pA1.Add (vA));
			if (fill)
				ctx.LineTo (pA1.Substract (vA));
			else
				ctx.MoveTo (pA1.Substract (vA));
			ctx.LineTo (p2);

			if (fill)
				ctx.Fill ();

			ctx.MoveTo (p1);
			ctx.LineTo (p2);
			ctx.LineWidth = stroke;
			ctx.Stroke ();
		}
		public static void DrawCoteInverse(this Cairo.Context ctx, PointD p1, PointD p2,
			double stroke = 1.0, bool fill = false, double arrowWidth = 3.0, double arrowLength = 7.0)
		{			
			PointD vDir = p2.Substract(p1);
			vDir = vDir.GetNormalized ();
			PointD vPerp = vDir.GetPerp ();

			PointD pA0 = p1.Add(vDir.Multiply(arrowLength));
			PointD pA1 = p2.Substract(vDir.Multiply(arrowLength));

			PointD vA = vPerp.Multiply (arrowWidth);

			ctx.MoveTo (p1.Add (vA));
			ctx.LineTo (pA0);
			ctx.LineTo (p1.Substract (vA));
			if (fill)
				ctx.LineTo (p1.Add (vA));

			ctx.MoveTo (p2.Add (vA));
			ctx.LineTo (pA1);
			ctx.LineTo (p2.Substract (vA));

			if (fill) {
				ctx.LineTo (p2.Add (vA));
				ctx.Fill ();
			}

			ctx.MoveTo (pA0);
			ctx.LineTo (pA1);
			ctx.LineWidth = stroke;
			ctx.Stroke ();
		}
		public static void DrawCoteFixed(this Cairo.Context ctx, PointD p1, PointD p2,
			double stroke = 1.0, double coteWidth = 3.0)
		{			
			PointD vDir = p2.Substract(p1);
			vDir = vDir.GetNormalized ();
			PointD vPerp = vDir.GetPerp ();
			PointD vA = vPerp.Multiply (coteWidth);

			ctx.MoveTo (p1.Add (vA));
			ctx.LineTo (p1.Substract (vA));
			ctx.MoveTo (p2.Add (vA));
			ctx.LineTo (p2.Substract (vA));
			ctx.MoveTo (p1);
			ctx.LineTo (p2);
			ctx.LineWidth = stroke;
			ctx.Stroke ();
		}

		public static void AddColorStop(this Cairo.Gradient grad, double offset, Color c)
		{
			grad.AddColorStop (offset, c);
		}
		#endregion

		public static Orientation GetOrientation(this Alignment a){
			return (a==Alignment.Left) ||(a==Alignment.Right) ? Orientation.Horizontal : Orientation.Vertical;
		}
		public static Alignment GetOpposite(this Alignment a){
			switch (a) {
			case Alignment.Left:
				return Alignment.Right;
			case Alignment.Right:
				return Alignment.Left;
			case Alignment.Top:
				return Alignment.Bottom;
			case Alignment.Bottom:
				return Alignment.Top;
			case Alignment.TopLeft:
				return Alignment.BottomRight;
			case Alignment.TopRight:
				return Alignment.BottomLeft;
			case Alignment.BottomLeft:
				return Alignment.TopRight;
			case Alignment.BottomRight:
				return Alignment.TopLeft;			
			}
			return Alignment.Center;
		}
		public static void Raise(this EventHandler handler, object sender, EventArgs e)
		{
			handler?.Invoke (sender, e);
		}
		public static void Raise<T>(this EventHandler<T> handler, object sender, T e)
		{
			handler?.Invoke (sender, e);
		}
		public static byte[] GetBytes(this string str)
		{
			byte[] bytes = new byte[str.Length * sizeof(char)];
			System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
			return bytes;
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

		public static FileSystemInfo [] GetFileSystemInfosOrdered (this DirectoryInfo di)
			=> di.GetFileSystemInfos ().OrderBy (f => f.Attributes).ThenBy (f => f.Name).ToArray ();
	}
}


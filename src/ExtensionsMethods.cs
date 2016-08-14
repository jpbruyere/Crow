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
		/// <summary>
		/// Type Extension method for Casting
		/// </summary>
		public static object Cast(this Type Type, object data)
		{
			var DataParam = Expression.Parameter(typeof(object), "data");
			var Body = Expression.Block(Expression.Convert(Expression.Convert(DataParam, data.GetType()), Type));

			var Run = Expression.Lambda(Body, DataParam).Compile();
			var ret = Run.DynamicInvoke(data);
			return ret;
		}
		public static bool IsWhiteSpaceOrNewLine (this char c)
		{
			return c == '\t' || c == '\r' || c == '\n' || char.IsWhiteSpace (c);
		}
	}
}


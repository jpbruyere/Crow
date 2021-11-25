// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Collections.Generic;
using System;
using Drawing2D;
using System.Diagnostics.CodeAnalysis;

namespace Crow.SkiaBackend {
	public class Region : IRegion
	{
		Rectangle _bounds;
		bool boundsUpToDate = true;
		public List<Rectangle> list = new List<Rectangle>();
		public int count => list.Count;

		public void AddRectangle(Rectangle r)
		{
			if (r == default)
				return;
			if (DoesNotContains (r)) {
				list.Add (r);
				boundsUpToDate = false;
			}
		}
		public bool DoesNotContains(Rectangle r)
		{
			foreach (Rectangle rInList in list)
				if (rInList.ContainsOrIsEqual(r))
					return false;
			return true;
		}
		public bool intersect(Rectangle r)
		{
			foreach (Rectangle rInList in list)
				if (rInList.Intersect(r))
					return true;
			return false;
		}
		public void stroke(IContext ctx, Color c)
		{
			foreach (Rectangle r in list)
				ctx.Rectangle(r);

			ctx.SetSource(c);

			ctx.LineWidth = 2;
			ctx.Stroke ();
		}
		public void clearAndClip(IContext ctx)
		{
			if (list.Count == 0)
				return;
			foreach (Rectangle r in list)
				ctx.Rectangle(r);

			ctx.ClipPreserve();
			ctx.Operator = Operator.Clear;
			ctx.Fill();
			ctx.Operator = Operator.Over;
		}

		public void clip(IContext ctx)
		{
			foreach (Rectangle r in list)
				ctx.Rectangle(r);

			ctx.Clip();
		}
		public Rectangle Bounds {
			get {
				if (!boundsUpToDate) {
					if (list.Count > 0) {
						_bounds = list [0];
						for (int i = 1; i < list.Count; i++) {
							_bounds += list [i];
						}
					} else
						_bounds = default;
					boundsUpToDate = true;
				}
				return _bounds;
			}
		}
		public void clear(IContext ctx)
		{
			foreach (Rectangle r in list)
				ctx.Rectangle(r);
			ctx.Operator = Operator.Clear;
			ctx.Fill();
			ctx.Operator = Operator.Over;
		}

		#region  IRegion implemenatation
		public bool IsEmpty => list.Count == 0;
		public int NumRectangles => list.Count;
		public Rectangle GetRectangle(int i) => list[i];
		public void UnionRectangle (Rectangle r) {
			/*if (r == default)
				System.Diagnostics.Debugger.Break ();*/
			AddRectangle (r);
		}
		public bool OverlapOut (Rectangle r) {
			foreach (Rectangle rInList in list)
				if (rInList.Intersect(r))
					return false;
			return true;
		}
		public RegionOverlap Contains(Rectangle rectangle)
		{
			throw new NotImplementedException();
		}
		public void Reset()
		{
			list = new List<Rectangle>();
			_bounds = default;
			boundsUpToDate = true;
		}

		public bool Equals([AllowNull] IRegion other)
			=> other is Region r ? Bounds.Equals (r.Bounds) : false;
		#endregion

		public override string ToString ()
		{
			string tmp = "";
			foreach (Rectangle r in list) {
				tmp += r.ToString ();
			}
			return tmp;
		}

		public void Dispose()
		{

		}

	}
}

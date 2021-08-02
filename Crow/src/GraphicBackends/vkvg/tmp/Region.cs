﻿// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Collections.Generic;
using System;
using Crow.Drawing;

namespace Crow {
	public enum RegionOverlap {
		In,
		Out,
		Part,
	}	
	public class Region : IDisposable
	{
		public List<Rectangle> list = new List<Rectangle>();
		public int count => list.Count;
		public int NumRectangles => list.Count;
		public bool IsEmpty => list.Count == 0;
		public Rectangle GetRectangle(int i) => list[i];

		public void AddRectangle(Rectangle r)
		{
			if (r == default)
				return;
			if (DoesNotContains (r)) {
				list.Add (r);
				boundsUpToDate = false;
			}
		}
		public void Reset()
		{
			list = new List<Rectangle>();
			_bounds = default;
			boundsUpToDate = true;
		}
		public bool DoesNotContains(Rectangle r)
		{
			foreach (Rectangle rInList in list)
				if (rInList.ContainsOrIsEqual(r))
					return false;
			return true;
		}
		public bool OverlapOut (Rectangle r) {
			foreach (Rectangle rInList in list)
				if (rInList.Intersect(r))
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
		public void stroke(Context ctx, Color c)
		{
			foreach (Rectangle r in list)
				ctx.Rectangle(r);

			ctx.SetSource(c);

			ctx.LineWidth = 2;
			ctx.Stroke ();
		}
		public void clearAndClip(Context ctx)
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

		public void clip(Context ctx)
		{
			foreach (Rectangle r in list)
				ctx.Rectangle(r);

			ctx.Clip();
		}
		public void UnionRectangle (Rectangle r) {
			/*if (r == default)
				System.Diagnostics.Debugger.Break ();*/
			AddRectangle (r);
		}
		Rectangle _bounds;
		bool boundsUpToDate = true;
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
		public void clear(Context ctx)
		{
			foreach (Rectangle r in list)
				ctx.Rectangle(r);
			ctx.Operator = Operator.Clear;
			ctx.Fill();
			ctx.Operator = Operator.Over;
		}
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
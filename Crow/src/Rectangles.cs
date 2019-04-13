//
// Rectangles.cs
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

using System.Collections.Generic;
using Crow.Cairo;

namespace Crow {
	public class Rectangles
    {
        public List<Rectangle> list = new List<Rectangle>();
        public int count
        {
            get { return list.Count; }
        }
		public bool IsEmpty => list.Count == 0;

        public void AddRectangle(Rectangle r)
        {
			if (doesNotContain (r)) {
				list.Add (r);
				boundsUpToDate = false;
			}
        }
        public void Reset()
        {
            list = new List<Rectangle>();
			_bounds = Rectangle.Empty;
			boundsUpToDate = true;
        }
        bool doesNotContain(Rectangle r)
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
		public void stroke(Context ctx, Color c)
		{
			foreach (Rectangle r in list)
				ctx.Rectangle(r);

			ctx.SetSourceColor(c);

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
						_bounds = Rectangle.Empty;
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
    }
}

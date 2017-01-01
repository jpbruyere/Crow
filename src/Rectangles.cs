using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.Diagnostics;

namespace Crow
{
    public class Rectangles
    {
        public List<Rectangle> list = new List<Rectangle>();
        public int count
        {
            get { return list.Count; }
        }

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
            //ctx.Save();

            foreach (Rectangle r in list)
            {
                ctx.Rectangle(r);
            }
            ctx.Operator = Operator.Clear;
            ctx.Fill();
            ctx.Operator = Operator.Over;
            //ctx.Restore();
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

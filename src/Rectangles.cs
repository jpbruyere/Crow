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
			if (rectIsNotContainedInRectangles (r)) {
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
        bool rectIsNotContainedInRectangles(Rectangle r)
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
        
        public Rectangles intersectingRects(Rectangle r)
        {
            Rectangles tmp = new Rectangles();

            foreach (Rectangle rInList in list)
                if (rInList.Intersect(r))
                    tmp.list.Add(rInList);//on bypass le test déjà fait a l'ajout intial du rect dans la liste

            return tmp;
        }
        public Rectangles SmallerContainedRects(Rectangle r)
        {
            Rectangles tmp = new Rectangles();

            foreach (Rectangle rInList in list)
                if (r.ContainsOrIsEqual(rInList) && rInList.Size < r.Size)
                    tmp.list.Add(rInList);

            return tmp;
        }
		/// <summary>
		/// Return rectangles with size smaller than r.size
		/// </summary>
        public Rectangles SmallerRects(Rectangle r)
        {
            Rectangles tmp = new Rectangles();

            foreach (Rectangle rInList in list)
                if (rInList.Size < r.Size)
                    tmp.list.Add(rInList);

            return tmp;
        }
        public Rectangles containedOrEqualRects(Rectangle r)
        {
            Rectangles tmp = new Rectangles();

            foreach (Rectangle rInList in list)
				if (r.ContainsOrIsEqual(rInList))// && rInList.Size <= r.Size)
                    tmp.list.Add(rInList);

            return tmp;
        }
		public void Srcoll(GraphicObject w)
		{
			Scroller sw = w as Scroller;
			if (sw == null)
				return;

			List<Rectangle> newList = new List<Rectangle>();

			foreach (Rectangle rInList in list)
			{
				Rectangle r = rInList;

				if (sw.VerticalScrolling)
					r.Top -= (int)sw.ScrollY;
				if (sw.HorizontalScrolling)
					r.Left -= (int)sw.ScrollX;

				newList.Add(r);
			}
			list = newList;        
		}
        public void Rebase(GraphicObject w)
        {
			Rectangle r = w.Parent.ContextCoordinates(w.Slot);
            List<Rectangle> newList = new List<Rectangle>();

            foreach (Rectangle rInList in list)
            {
                Rectangle rebasedR = rInList;
                rebasedR.TopLeft-= r.TopLeft;

				Scroller sw = w as Scroller;
                if (sw != null)
                {
					if (sw.VerticalScrolling) {
						rebasedR.Top -= (int)sw.ScrollY;
//						if (sw.scrollY < 0)
//							Debug.WriteLine ("..");
					}if (sw.HorizontalScrolling)
						rebasedR.Left -= (int)sw.ScrollX;
                }

                newList.Add(rebasedR);
            }
			list = newList;        
        }
		public void stroke(Context ctx, Color c)
		{
			foreach (Rectangle r in list)
			{
				ctx.Rectangle(r);
			}

			ctx.Color = c;

			ctx.LineWidth = 2;
			ctx.Stroke ();
		}
        public void clearAndClip(Context ctx)
        {
            foreach (Rectangle r in list)
            {
                ctx.Rectangle(r);
            }
				
			ctx.ClipPreserve();

			//if (Interface.Background == Color.Transparent) {
				ctx.Operator = Operator.Clear; 
			//} else {
			//	ctx.Color = Interface.Background;
			//}

            ctx.Fill();
            ctx.Operator = Operator.Over;            
        }

        public void clip(Context ctx)
        {
            foreach (Rectangle r in list)
            {
                ctx.Rectangle(r);
            }

            ctx.Clip();
        }

		Rectangle _bounds;
		bool boundsUpToDate = true;
		public Rectangle Bounds {
			get { 
				if (!boundsUpToDate) {
					_bounds = Rectangle.Empty;
					foreach (Rectangle rInList in list)
						_bounds += rInList;
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

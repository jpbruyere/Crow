using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.Diagnostics;

namespace go
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
            //if (r.Left == 0)
            //    Debugger.Break();
            if (addRect(r))
                list.Add(r);
        }
        public void Reset()
        {
            list = new List<Rectangle>();
        }
        bool addRect(Rectangle r)
        {
            foreach (Rectangle rInList in list)
                if (rInList.Contains(r))
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
                    tmp.list.Add(rInList.Clone);//on bypass le test déjà fait a l'ajout intial du rect dans la liste

            return tmp;
        }
        public Rectangles SmallerContainedRects(Rectangle r)
        {
            Rectangles tmp = new Rectangles();

            foreach (Rectangle rInList in list)
                if (r.Contains(rInList) && rInList.Size < r.Size)
                    tmp.list.Add(rInList.Clone);

            return tmp;
        }
        public Rectangles SmallerRects(Rectangle r)
        {
            Rectangles tmp = new Rectangles();

            foreach (Rectangle rInList in list)
                if (rInList.Size < r.Size)
                    tmp.list.Add(rInList.Clone);

            return tmp;
        }
        public Rectangles containedRects(Rectangle r)
        {
            Rectangles tmp = new Rectangles();

            foreach (Rectangle rInList in list)
                if (r.Contains(rInList) && rInList.Size <= r.Size)
                    tmp.list.Add(rInList.Clone);

            return tmp;
        }
        public void Rebase(GraphicObject w)
        {
            Rectangle r = w.renderBounds;
            List<Rectangle> newList = new List<Rectangle>();

            foreach (Rectangle rInList in list)
            {
                Rectangle rebasedR = rInList.Clone;
                rebasedR.TopLeft-= r.TopLeft;

                ScrollingWidget sw = w as ScrollingWidget;
                if (sw != null)
                {
                    if (sw.VerticalScrolling)
                        rebasedR.Top -= sw.scrollY;
                    if (sw.HorizontalScrolling)
                        rebasedR.Left -= sw.scrollX;
                }
                newList.Add(rebasedR);
            }
            list = newList;        
        }
        public void clearAndClip(Context ctx)
        {
            //ctx.Save();
            
            foreach (Rectangle r in list)
            {
                ctx.Rectangle(r);
            }

            ctx.ClipPreserve();
            ctx.Operator = Operator.Clear;
            ctx.Fill();
            ctx.Operator = Operator.Over;
            //ctx.Restore();
        }
        public void clip(Context ctx)
        {
            foreach (Rectangle r in list)
            {
                ctx.Rectangle(r);
            }

            ctx.Clip();
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
        //public RectanglesRelations test(Rectangle r)
        //{
        //    foreach (Rectangle rInList in list)
        //    {
        //        switch (rInList.test(r))
        //        {
        //            case RectanglesRelations.NoRelation:
        //                break;
        //            case RectanglesRelations.Intersect:
        //                break;
        //            case RectanglesRelations.Contains:
        //                break;
        //            case RectanglesRelations.Equal:
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}
    }
}

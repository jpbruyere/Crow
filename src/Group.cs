using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using Cairo;

namespace go
{
    public class Group : ScrollingWidget
    {
        public List<GraphicObject> Children = new List<GraphicObject>();

        public ImageSurface cairoCache;

        bool _cachingInProgress = false;
        public override bool cachingInProgress
        {
            get { return _cachingInProgress; }
            set { _cachingInProgress = value; }
        }

        public Group(int _width = 30, int _height = 30)
            : base(_width, _height)
        {
            init();
        }

        public Group()
            : base()
        {
            init();
        }
        void init()
        {
            focusable = true;
            borderWidth = 0;
            background = Color.Transparent;
        }

        //public Widget addChild(Widget child)
        //{
        //    Children.Add(child);
        //    child.Parent = this;
        //    layoutIsValid = false;
        //    return child;
        //}
        public T addChild<T>(T child)
        {
            Children.Add(child as GraphicObject);
            (child as GraphicObject).Parent = this as GraphicObject;
            layoutIsValid = false;
            return (T)child;
        }
        public void removeChild(GraphicObject child)
        {
            Children.Remove(child);
            child.Parent = null;
            layoutIsValid = false;
        }

        public GraphicObject activeWidget;
        public bool multiSelect = false;
        public override void invalidateLayout()
        {
            base.invalidateLayout();
            foreach (GraphicObject w in Children)
                w.invalidateLayout();
        }
        public override bool layoutIsValid
        {
            get
            {
                if (!isVisible)
                    return true;

                if (!base.layoutIsValid)
                    return false;
                else//le layout n'est valide que si tous les enfents sont validés aussi
                {

                        foreach (GraphicObject w in Children)
                            if (!w.layoutIsValid)
                                return false;
                    
                }

                return true;
            }
            set
            {
                base.layoutIsValid = value;
            }
        }

        ////limit to clientbounds of wg for drawing on cached imagesurface
        ////if called by wg itself, call base.BoundsInPanelCoordonate
        //public override Rectangle renderBoundsInContextCoordonate
        //{
        //    get
        //    {
        //        if (Parent == null)
        //            return new Rectangle(renderBounds.Size);

        //        //if (Parent is Panel && !(this is ScrollingWidget))
        //        //    return Parent.clientBounds;

        //        Rectangle tmp = Parent.renderBoundsInContextCoordonate;

        //        return new Rectangle(
        //                tmp.X + renderBounds.X,
        //                tmp.Y + renderBounds.Y,
        //                renderBounds.Width,
        //                renderBounds.Height);

        //        return cachingInProgress ? new Rectangle(
        //                                        clientBounds.X + scrollX,
        //                                        clientBounds.Y + scrollY,
        //                                        clientBounds.Width,
        //                                        clientBounds.Height)
        //                                : base.renderBoundsInContextCoordonate;
        //    }
        //}
        //public override bool needGraphicalUpdate
        //{
        //    get { return _needGraphicalUpdate; }
        //    set
        //    {
        //        if (value == _needGraphicalUpdate)
        //            return;

        //        base.needGraphicalUpdate = value;

        //        if (_needGraphicalUpdate)
        //        {

        //            Widget[] widgets = new Widget[Children.Count];
        //            Children.CopyTo(widgets);
        //            foreach (Widget w in widgets)
        //            {
        //                //if (w.isCached)
        //                //    w.needGraphicalUpdate = true;
        //                //else
        //                    w.needRedraw = true;
        //            }
        //        }
        //    }
        //}
        //nécessaire becose no cached of widgetgroup...TODO
        //public override bool needRedraw
        //{
        //    get
        //    {
        //        return base.needRedraw;
        //    }
        //    set
        //    {
        //        if (value == _needRedraw)
        //            return;

        //        base.needRedraw = value;
        //        if (_needRedraw && !isCached)
        //        {
        //            Widget[] widgets = new Widget[Children.Count];
        //            Children.CopyTo(widgets);
        //            foreach (Widget w in widgets)
        //                w.needRedraw = true;
        //        }
        //    }
        //}

        public override void ProcessMouseDown(Point mousePos)
        {
            if (!isVisible)
                return;

            if (activeWidget == null)
                return;

            //if (activeWidget.ScreenCoordBounds.Contains(mousePos))
            //{
            //    activeWidget.ProcessMouseDown(mousePos);

            //    //WrappedWidgetGroup wg = activeWidget as WrappedWidgetGroup;
            //    //if (wg != null)
            //    //{
            //    //    wg.ProcessMouseDown(mousePos);
            //    //}
            //}
        }
        public override bool ProcessMousePosition(Point mousePos)
        {
            if (!isVisible)
                return false;

            bool baseResult = base.ProcessMousePosition(mousePos);

            if (activeWidget != null)
            {
                if (activeWidget.ProcessMousePosition(mousePos))
                    return true;
                else
                    activeWidget = null;
            }

            foreach (GraphicObject w in Children)
            {
                if (w.isVisible)
                {
                    if (w.ProcessMousePosition(mousePos))
                    {
                        activeWidget = w;
                        return true;
                    }
                }
            }

            activeWidget = null;
            return baseResult;
        }
        public override bool isCached
        {
            get
            {
                return cairoCache == null ? false : true;
            }
        }
        public void putWidgetOnTop(GraphicObject w)
        {
            if (Children.Contains(w))
            {
                Children.Remove(w);
                Children.Add(w);
            }
        }
        public void putWidgetOnBottom(GraphicObject w)
        {
            if (Children.Contains(w))
            {
                Children.Remove(w);
                Children.Insert(0, w);
            }
        }

        #region widget overrides
        public override void updateLayout()
        {
            //while (!layoutIsValid)
            //{
            //le layout ne se fait à la base que si _LayoutIsValid = false
            if (!(sizeIsValid && positionIsValid))
                base.updateLayout();

            Rectangle contentBounds = Rectangle.Zero;

            GraphicObject[] widgets = new GraphicObject[Children.Count];
            Children.CopyTo(widgets);
            foreach (GraphicObject w in widgets)
            {
                if (!w.layoutIsValid)
                    w.updateLayout();

                contentBounds = contentBounds + w.renderBounds;
            }

            contentBounds.Width += borderWidth + margin;
            contentBounds.Height += borderWidth + margin;

            if (sizeToContent || VerticalScrolling || HorizontalScrolling)
            {
                sizeIsValid = true;

                foreach (GraphicObject w in widgets)
                {
                    if (!w.sizeIsValid && w.isVisible)
                    {
                        sizeIsValid = false;
                        break;
                    }
                }

                //                contentBounds.Width += borderWidth + margin;
                //contentBounds.Height += borderWidth + margin;
                if (sizeIsValid)
                {
                    if (sizeToContent)
                        renderBounds.Size = contentBounds.Size;
                    else if (VerticalScrolling)
                        renderBounds.Size = new Size(renderBounds.Size.Width, contentBounds.Size.Height);
                    else if (HorizontalScrolling)
                        renderBounds.Size = new Size(contentBounds.Size.Width, renderBounds.Size.Height);
                }
            }
            //if (sizeToContent)
            //    renderBounds.Size = contentBounds.Size;
            //else if (VerticalScrolling)
            //    renderBounds.Size = new Size(renderBounds.Size.Width, contentBounds.Size.Height);
            //else if (HorizontalScrolling)
            //    renderBounds.Size = new Size(contentBounds.Size.Width, renderBounds.Size.Height);

            //}
            if (layoutIsValid)
                registerForRedraw();
        }
        internal override void updateGraphic()
        {
            if (cairoCache != null)
                cairoCache.Dispose();

            cairoCache = null;

            base.updateGraphic();
        }
        public override void cairoDraw(ref Context ctx, Rectangles clip = null)
        {
            if (!isVisible)//check if necessary??
                return;

            if (bmp == null)    //update graphic before caching because UG reset cache
                updateGraphic();

            Rectangle rBoundsInContext = null;
            Rectangles containedRects = null;
            Rectangles smallerContainedRect = null;

            //if (isCached)
            //    rBoundsInContext = new Rectangle(rBoundsInContext.Size);

            bool rectsInBounds = false;
            bool rebuildCache = false;

            if (cairoCache == null)
            {
                cairoCache =
                    new ImageSurface(Format.Argb32, renderBounds.Width, renderBounds.Height);

                rebuildCache = true;
            }

            cachingInProgress = true;

            if (clip != null)
            {
                clip.Rebase(this);


                if (!rebuildCache)
                {
                    rBoundsInContext = this.renderBoundsInContextCoordonate;

                    containedRects = clip.containedRects(rBoundsInContext);
                    smallerContainedRect = containedRects.SmallerRects(rBoundsInContext);

                    if (smallerContainedRect.count > 0)
                        rectsInBounds = true;
                }
            }

            if (rectsInBounds || rebuildCache)
            {
                Context gr = new Context(cairoCache);

                if (rectsInBounds)
                    smallerContainedRect.clearAndClip(gr);

                //gr.Target.WriteToPng(directories.rootDir + @"test.png");

                base.cairoDraw(ref gr);

                //gr.Target.WriteToPng(directories.rootDir + @"test.png");

                GraphicObject[] widgets = new GraphicObject[Children.Count];
                Children.CopyTo(widgets);
                foreach (GraphicObject w in widgets)
                {
                    if (rebuildCache)
                        w.cairoDraw(ref gr);
                    else
                    {
                        Rectangle r = w.renderBoundsInContextCoordonate;
                        Rectangles clipRects = smallerContainedRect.intersectingRects(r);
                        //gr.Rectangle(r);
                        //gr.LineWidth = 1;
                        //gr.Color = new Cairo.Color(1, 0, 1, 1);
                        //gr.Stroke();

                        if (clipRects.count > 0)
                            w.cairoDraw(ref gr, clipRects);
                        //gr.Target.WriteToPng(directories.rootDir + @"test.png");
                    }
                }

                cairoCache.Flush();
                
                //cairoCache.WriteToPng(directories.rootDir + @"test.png");
            }

            cachingInProgress = false;

            //draw cache
            //Rectangle rCBPC = Parent.ClientBoundsInPanelCoordonate;
            //cachingInProgress change context coordonate system
            rBoundsInContext = this.renderBoundsInContextCoordonate;
            //ctx.ResetClip();
            ctx.SetSourceSurface(cairoCache, rBoundsInContext.X, rBoundsInContext.Y);
            //ctx.Rectangle(r);
            ctx.Paint();

            //ctx.ResetClip();
            //ctx.Rectangle(rBoundsInContext);
            //ctx.Color = Color.Red;
            //ctx.Fill();
            //ctx.Target.WriteToPng(directories.rootDir + @"test.png");

        }
        #endregion

        public override string ToString()
        {
            string tmp = base.ToString();
            foreach (GraphicObject w in Children)
            {
                tmp += "\n" + w.ToString();
            }
            return tmp;
        }
    }
}

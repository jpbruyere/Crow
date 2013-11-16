using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Cairo;

namespace go
{
    public class ScrollingWidget : GraphicObject
    {
        public ScrollingWidget(int _width = 30, int _height = 30)
            : base(new Rectangle(0, 0, _width, _height))
        {

        }

        public ScrollingWidget()
            : base()
        {

        }
        public bool VerticalScrolling = false;
        public bool HorizontalScrolling = false;

        public int scrollX = 0;
        public int scrollY = 0;


        public override Rectangle ScreenCoordBounds
        {
            get
            {
                return Parent == null ? bounds :
                    new Rectangle(
                        Parent.ScreenCoordBounds.X + renderBounds.X + scrollX,
                        Parent.ScreenCoordBounds.Y + renderBounds.Y + scrollY,
                        renderBounds.Width,
                        renderBounds.Height);
            }
        }

        public override Rectangle renderBoundsInContextCoordonate
        {
            get
            {
                //should be in go with cache, scrolling widget have no cache
                if (cachingInProgress)
                    return new Rectangle(renderBounds.Size);
                //if (Parent.isCached)
                //    return renderBounds.Clone;

                Rectangle tmp = Parent.renderBoundsInContextCoordonate;

                return new Rectangle(
                        tmp.X + renderBounds.X + scrollX,
                        tmp.Y + renderBounds.Y + scrollY,
                        renderBounds.Width,
                        renderBounds.Height);

            }
        }

        public override Rectangle ClientBoundsInContextCoordonate
        {
            get
            {
                if (cachingInProgress)
                    return new Rectangle(
                        clientBounds.X,
                        clientBounds.Y,
                        clientBounds.Width,
                        clientBounds.Height);

                //if (Parent is Panel && !(this is ScrollingWidget))
                //    return Parent.clientBounds;

                Rectangle tmp = Parent.renderBoundsInContextCoordonate;

                return new Rectangle(
                        tmp.X + renderBounds.X + clientBounds.X + scrollX,
                        tmp.Y + renderBounds.Y + clientBounds.Y + scrollY,
                        clientBounds.Width,
                        clientBounds.Height);
            }
        }
        public override Rectangle renderBoundsInBackendSurfaceCoordonate
        {
            get
            {
                Rectangle tmp = Parent.renderBoundsInBackendSurfaceCoordonate;

                return new Rectangle(
                        tmp.X + renderBounds.X +  scrollX,
                        tmp.Y + renderBounds.Y +  scrollY,
                        renderBounds.Width,
                        renderBounds.Height);
            }
        }
        
        public override bool ProcessMousePosition(Point mousePos)
        {
            return base.ProcessMousePosition(mousePos);
        }
        public override void ProcessMouseWeel(int delta)
        {
            if (!isVisible)
                return;


            if (VerticalScrolling && renderBounds.Height > Parent.clientBounds.Height)
            {
                //add redraw call with old bounds to errase old position
                registerForRedraw();

                scrollY += delta;

                if (scrollY > 0)
                    scrollY = 0;
                else if (scrollY < -renderBounds.Height + Parent.clientBounds.Height)
                    scrollY = -renderBounds.Height + Parent.clientBounds.Height;

            }
            if (HorizontalScrolling && renderBounds.Width > Parent.clientBounds.Width)
            {
                //add redraw call with old bounds to errase old position
                registerForRedraw();

                scrollX += delta;
            }


            //renderBounds.Y = -scrollY;
            registerForRedraw();
        }

    }
}

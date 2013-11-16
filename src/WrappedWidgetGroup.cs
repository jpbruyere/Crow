using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace go
{
    public class WrappedWidgetGroup : Group
    {

        public int widgetSpacing = 2;

        public Orientation Orientation = Orientation.Horizontal;


        public WrappedWidgetGroup()
            : base()
        {
            borderWidth = 0;
        }

        int currentXForWidget = 0;
        int currentYForWidget = 0;

        int highestWidget = 0;
        int largestWidget = 0;


        public override void updateLayout()
        {
            //while (!layoutIsValid)
            //{
            //if (!(sizeIsValid && positionIsValid))
            base.updateLayout();

            //if (!base.layoutIsValid)
            //    return;

            currentXForWidget = clientBounds.X;
            currentYForWidget = clientBounds.Y;

            highestWidget = 0;
            largestWidget = 0;

            Rectangle contentBounds = Rectangle.Zero;

            GraphicObject[] widgets = new GraphicObject[Children.Count];
            Children.CopyTo(widgets);
            foreach (GraphicObject w in widgets)
            {
                if (w.renderBounds.Width > largestWidget)
                    largestWidget = w.renderBounds.Width;
                if (w.renderBounds.Height > highestWidget)
                    highestWidget = w.renderBounds.Height;

                if (!enoughtSpaceForWidget(w))
                    advance(w);

                if (enoughtSpaceForWidget(w))
                {
                    w.renderBounds.X = currentXForWidget;
                    w.renderBounds.Y = currentYForWidget;

                    w.positionIsValid = true;

                    contentBounds += w.renderBounds;

                    advance(w);
                }
                else
                    break;
            }

            contentBounds.Width += borderWidth + margin;
            contentBounds.Height += borderWidth + margin;

            if (sizeToContent)
                renderBounds.Size = contentBounds.Size;
            else if (VerticalScrolling)
                renderBounds.Size = new Size(renderBounds.Size.Width, contentBounds.Size.Height);
            else if (HorizontalScrolling)
                renderBounds.Size = new Size(contentBounds.Size.Width, renderBounds.Size.Height);

            if (layoutIsValid)
                registerForRedraw();
        }


        bool enoughtSpaceForWidget(GraphicObject w)
        {
            int nextXForWidget = 0;
            int nextYForWidget = 0;

            if (Orientation == Orientation.Horizontal)
                nextXForWidget = currentXForWidget + w.renderBounds.Width;
            else
                nextYForWidget = nextYForWidget + w.renderBounds.Height;

            if (!sizeToContent)
            {
                if (nextXForWidget > clientBounds.Right && !HorizontalScrolling)
                    return false;
                if (currentYForWidget > clientBounds.Bottom && !VerticalScrolling)
                    return false;
            }
            return true;
        }
        void advance(GraphicObject w)
        {
            if (Orientation == Orientation.Horizontal)
            {
                //if (w is LabelWidget)
                //    Debugger.Break();
                currentXForWidget = currentXForWidget + widgetSpacing + w.renderBounds.Width;
            }
            else
                currentYForWidget = currentYForWidget + widgetSpacing + w.renderBounds.Height;

            if (!sizeToContent)
            {
                if (currentXForWidget > clientBounds.Right && !HorizontalScrolling)
                {
                    if (Orientation == Orientation.Vertical)
                    {
                        //not scrolling
                    }
                    else
                    {
                        currentXForWidget = clientBounds.X;
                        currentYForWidget += widgetSpacing + highestWidget;
                        highestWidget = 0;
                    }
                }
                if (currentYForWidget > clientBounds.Bottom && !VerticalScrolling)
                {
                    if (Orientation == Orientation.Horizontal)
                    {
                        //not scrolling
                    }
                    else
                    {
                        currentXForWidget += widgetSpacing + largestWidget;
                        currentYForWidget = clientBounds.Y;
                        largestWidget = 0;
                    }

                }
            }
        }
    }
}

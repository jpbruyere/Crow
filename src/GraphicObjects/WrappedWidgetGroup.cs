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
            BorderWidth = 0;
        }

        int currentXForWidget = 0;
        int currentYForWidget = 0;

        int highestWidget = 0;
        int largestWidget = 0;


        public override void UpdateLayout()
        {
            //while (!layoutIsValid)
            //{
            //if (!(sizeIsValid && positionIsValid))
            base.UpdateLayout();

            //if (!base.layoutIsValid)
            //    return;

            currentXForWidget = ClientRectangle.X;
            currentYForWidget = ClientRectangle.Y;

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

            contentBounds.Width += BorderWidth + Margin;
            contentBounds.Height += BorderWidth + Margin;

            if (SizeToContent)
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

            if (!SizeToContent)
            {
                if (nextXForWidget > ClientRectangle.Right && !HorizontalScrolling)
                    return false;
                if (currentYForWidget > ClientRectangle.Bottom && !VerticalScrolling)
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

            if (!SizeToContent)
            {
                if (currentXForWidget > ClientRectangle.Right && !HorizontalScrolling)
                {
                    if (Orientation == Orientation.Vertical)
                    {
                        //not scrolling
                    }
                    else
                    {
                        currentXForWidget = ClientRectangle.X;
                        currentYForWidget += widgetSpacing + highestWidget;
                        highestWidget = 0;
                    }
                }
                if (currentYForWidget > ClientRectangle.Bottom && !VerticalScrolling)
                {
                    if (Orientation == Orientation.Horizontal)
                    {
                        //not scrolling
                    }
                    else
                    {
                        currentXForWidget += widgetSpacing + largestWidget;
                        currentYForWidget = ClientRectangle.Y;
                        largestWidget = 0;
                    }

                }
            }
        }
    }
}

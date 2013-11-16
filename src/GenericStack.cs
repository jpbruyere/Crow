using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
{
    public class GenericStack : Group
    {        
        public int widgetSpacing = 5;
        public Orientation Orientation = Orientation.Horizontal;

        public GenericStack()
            : base()
        {
            borderWidth = 0;
            sizeToContent = true;

        }

        int currentXForWidget = 0;
        int currentYForWidget = 0;

        public override void updateLayout()
        {
            //if (!(sizeIsValid && positionIsValid))
            base.updateLayout();

            currentXForWidget = clientBounds.X;
            currentYForWidget = clientBounds.Y;


            Rectangle contentBounds = Rectangle.Zero;

            GraphicObject[] widgets = new GraphicObject[Children.Count];
            Children.CopyTo(widgets);
            foreach (GraphicObject w in widgets)
            {
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
            if (!sizeToContent)
            {
                int nextXForWidget = 0;
                int nextYForWidget = 0;

                if (Orientation == Orientation.Horizontal)
                    nextXForWidget = currentXForWidget + w.renderBounds.Width;
                else
                    nextYForWidget = nextYForWidget + w.renderBounds.Height;


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
                currentXForWidget = currentXForWidget + widgetSpacing + w.renderBounds.Width;
            else
                currentYForWidget = currentYForWidget + widgetSpacing + w.renderBounds.Height;

        }
    }
}

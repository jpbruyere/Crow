using System;

namespace go
{
    public class Container : GraphicObject
    {
        public Container()
            : base()
        {
        }
        public Container(Rectangle _bounds)
            : base(_bounds)
        {
        }

        public GraphicObject child;
        public T setChild<T>(T _child)
        {

            if (child != null)
                child.Parent = null;

            child = _child as GraphicObject;

            if (child != null)
                child.Parent = this;

            return (T)_child;
        }
        public override void invalidateLayout()
        {
            base.invalidateLayout();

            if (child != null)
                child.invalidateLayout();
        }
        public override bool layoutIsValid
        {
            get
            {
                if (!isVisible)
                    return true;

                if (!base.layoutIsValid)
                    return false;
                else if (child != null)
                    if (!child.layoutIsValid)
                        return false;

                return true;
            }
            set
            {
                base.layoutIsValid = value;
            }
        }

        public override void updateLayout()
        {
            if (!isVisible)
                return;

            if (!(sizeIsValid && positionIsValid))
                base.updateLayout();

            if (child != null)
            {
                child.updateLayout();
                if (sizeToContent && child.sizeIsValid)
                {
                    renderBounds.Size = child.renderBounds.Size + 2 * margin + 2 * borderWidth;
                    child.renderBounds.TopLeft = clientBounds.TopLeft;
                    sizeIsValid = true;
                }
            }

            if (layoutIsValid)
                registerForRedraw();
        }

        public override bool ProcessMousePosition(Point mousePos)
        {
            if (!isVisible)
                return false;

            bool result = base.ProcessMousePosition(mousePos);

            //if (this is Panel)
            //    return result;
            //else

                if (result)
                    if (child != null)
                        child.ProcessMousePosition(mousePos);

            return result;
        }
        public override void ProcessMouseWeel(int delta)
        {
            if (!isVisible)
                return;

            if (child != null)
                child.ProcessMouseWeel(delta);
        }

        public override void cairoDraw(ref Cairo.Context ctx, Rectangles clip = null)
        {
            if (!isVisible)//check if necessary??
                return;
            
            ctx.Save();

            ctx.Rectangle(renderBoundsInContextCoordonate);
            ctx.Clip();

            if (clip != null)
                clip.clip(ctx);

            base.cairoDraw(ref ctx, clip);

            //clip to client zone

            ctx.Rectangle(ClientBoundsInContextCoordonate);
            ctx.Clip();

            if (clip != null)
                clip.Rebase(this);

            if (child != null)
                child.cairoDraw(ref ctx, clip);

            ctx.Restore();
            //ctx.Target.WriteToPng(@"/home/jp/test.png");
        
        }
    }
}


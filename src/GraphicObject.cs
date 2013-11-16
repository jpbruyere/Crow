using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.Diagnostics;
using OpenTK.Input;

using Cairo;


namespace go
{
    public class GraphicObject
    {
        public GraphicObject()
        {
            init();
            registerForGraphicUpdate();
        }
        public GraphicObject(Rectangle _bounds)
        {
            bounds = _bounds;
            init();
            registerForGraphicUpdate();
        }

        void init()
        {
            background = Theme.background;
            foreground = Theme.foreground;
            borderColor = Theme.borderColor;
            borderWidth = Theme.borderWidth;
            margin = Theme.margin;
            horizontalAlignment = Theme.horizontalAlignment;
            verticalAlignment = Theme.verticalAlignment;
            sizeToContent = Theme.sizeToContent;
        }

        public virtual int x
        {
            get { return bounds.X; }
            set
            {
                if (bounds.X == value)
                    return;

                bounds.X = value;

                layoutIsValid = false;
                registerForGraphicUpdate();
            }
        }
        public virtual int y
        {
            get { return bounds.Y; }
            set
            {
                if (bounds.Y == value)
                    return;

                bounds.Y = value;

                layoutIsValid = false;
                registerForGraphicUpdate();
            }
        }
        public int width
        {
            get { return bounds.Width; }
            set
            {
                if (bounds.Width == value)
                    return;

                bounds.Width = value;

                invalidateLayout();
            }
        }
        public int height
        {
            get { return bounds.Height; }
            set
            {
                if (bounds.Height == value)
                    return;

                bounds.Height = value;

                invalidateLayout();
            }
        }

        public virtual Rectangle renderBoundsInContextCoordonate
        {
            get
            {
                if (Parent == null)
                    return renderBounds.Clone;
                //if (Parent.isCached)
                //    return renderBounds.Clone;

                Rectangle tmp = Parent.renderBoundsInContextCoordonate;

                return new Rectangle(
                        tmp.X + renderBounds.X,
                        tmp.Y + renderBounds.Y,
                        renderBounds.Width,
                        renderBounds.Height);
            }
        }
        public virtual Rectangle ClientBoundsInContextCoordonate
        {
            get
            {
                if (Parent == null)
                    return new Rectangle(
                        renderBounds.X + clientBounds.X,
                        renderBounds.Y + clientBounds.Y,
                        clientBounds.Width,
                        clientBounds.Height); 

                //if (Parent is Panel && !(this is ScrollingWidget))
                //    return Parent.clientBounds;

                Rectangle tmp = Parent.renderBoundsInContextCoordonate;

                return new Rectangle(
                        tmp.X + renderBounds.X + clientBounds.X,
                        tmp.Y + renderBounds.Y + clientBounds.Y,
                        clientBounds.Width,
                        clientBounds.Height);
            }

        }

        //public virtual Rectangle rectangleInContextCoordonate(Rectangle r)
        //{
        //    if (Parent == null)
        //        return r;

        //    Rectangle tmp = Parent.rectangleInContextCoordonate(r);

        //    return new Rectangle(
        //            tmp.X + r.X,
        //            tmp.Y + r.Y,
        //            r.Width,
        //            r.Height);        
        //}
        public virtual Rectangle renderBoundsInBackendSurfaceCoordonate
        {
            get
            {
                Rectangle tmp = Parent.renderBoundsInBackendSurfaceCoordonate;

                return new Rectangle(
                        tmp.X + renderBounds.X,
                        tmp.Y + renderBounds.Y,
                        renderBounds.Width,
                        renderBounds.Height);
            }
        }
        public virtual Rectangle ClientBoundsInBackendSurfaceCoordonate
        {
            get
            {
                if (Parent == null)
                    return new Rectangle(
                        renderBounds.X + clientBounds.X,
                        renderBounds.Y + clientBounds.Y,
                        clientBounds.Width,
                        clientBounds.Height); ;

                //if (Parent is Panel && !(this is ScrollingWidget))
                //    return Parent.clientBounds;

                Rectangle tmp = Parent.ClientBoundsInBackendSurfaceCoordonate;

                return new Rectangle(
                        tmp.X + renderBounds.X + clientBounds.X,
                        tmp.Y + renderBounds.Y + clientBounds.Y,
                        clientBounds.Width,
                        clientBounds.Height);
            }

        }
        public virtual Rectangle rectInScreenCoord(Rectangle r)
        {
            return
                new Rectangle(
                    ScreenCoordBounds.X + r.X,
                    ScreenCoordBounds.Y + r.Y,
                    r.Width,
                    r.Height);
        }
        public virtual Rectangle ScreenCoordBounds
        {
            get
            {
                return Parent == null ? bounds :
                    new Rectangle(
                        Parent.ScreenCoordBounds.X + renderBounds.X,
                        Parent.ScreenCoordBounds.Y + renderBounds.Y,
                        renderBounds.Width,
                        renderBounds.Height);
            }
        }
        public virtual Rectangle ScreenCoordClientBounds
        {
            get
            {
                return Parent == null ?
                    new Rectangle(
                        renderBounds.X + clientBounds.X,
                        renderBounds.Y + clientBounds.Y,
                        clientBounds.Width,
                        clientBounds.Height) :
                    new Rectangle(
                        Parent.ScreenCoordClientBounds.X + clientBounds.X,
                        Parent.ScreenCoordClientBounds.Y + clientBounds.Y,
                        clientBounds.Width,
                        clientBounds.Height);
            }
        }

        public Rectangle bounds = new Rectangle();
        public Rectangle renderBounds = new Rectangle();

        public virtual Rectangle clientBounds
        {
            get
            {
                //if (renderBounds == Rectangle.Empty)
                //    Debugger.Break();
                Rectangle cb = renderBounds.Clone;
                cb.X = 0;
                cb.Y = 0;
                cb.Inflate(-(borderWidth + margin), -(borderWidth + margin));
                return cb;
            }
        }

        //static copy of themable proporties, 
        //set default value for all newly created item
        public static class Theme
        {
            public static Color background = Color.DimGray;
            public static Color foreground = Color.White;
            public static Color borderColor = Color.Gray;
            public static int margin = 0;
            public static int borderWidth = 0;
            public static VerticalAlignment verticalAlignment = VerticalAlignment.Stretch;
            public static HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch;
            public static bool sizeToContent = false;
        }

        Color _background;
        Color _foreground;
        Color _borderColor;
        int _borderWidth;
        int _margin;

        public VerticalAlignment verticalAlignment;
        public HorizontalAlignment horizontalAlignment;
        public bool sizeToContent;

        public Color background
        {
            get { return _background; }
            set
            {
                _background = value;
                registerForGraphicUpdate();
            }
        }
        public Color foreground
        {
            get { return _foreground; }
            set
            {
                _foreground = value;
                registerForGraphicUpdate();
            }
        }
        public Color borderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                registerForGraphicUpdate();
            }
        }
        public int borderWidth
        {
            get { return _borderWidth; }
            set
            {
                _borderWidth = value;
                registerForGraphicUpdate();
            }
        }
        public int margin
        {
            get { return _margin; }
            set
            {
                _margin = value;
                registerForGraphicUpdate();
            }
        }

        public object Tag;

        public GraphicObject Parent;

        public bool focusable = false;

        private bool _hasFocus = false;
        protected bool _isVisible = true;

        public virtual bool hasFocus
        {
            get { return _hasFocus; }
            set
            {
                _hasFocus = value;
            }
        }
        public virtual bool isVisible
        {
            get { return _isVisible; }
            set
            {
                if (value == _isVisible)
                    return;

                _isVisible = value;
                if (Parent != null)
                    Parent.invalidateLayout();
                //else
                //    registerForRedraw();
            }
        }


        internal bool sizeIsValid = false;
        internal bool positionIsValid = false;

        public virtual void invalidateLayout()
        {
            bmp = null;
            layoutIsValid = false;
        }
        public virtual bool layoutIsValid
        {
            get { return sizeIsValid & positionIsValid; }
            set
            {
                if (value == sizeIsValid && value == positionIsValid)
                    return;

                //_layoutIsValid = value;

                sizeIsValid = value;
                positionIsValid = value;

                //if (!layoutIsValid && Parent != null)
                //    Parent.layoutIsValid = false;
            }
        }

        public virtual bool isCached
        {
            get { return false; }
        }
        public virtual bool cachingInProgress
        {
            get { return false; }
            set { return; }
        }


        public byte[] bmp;



        Panel _panel;
        public Panel panel
        {
            get
            {
                if (_panel == null)
                {
                    GraphicObject w = Parent;

                    while (w != null)
                    {
                        Panel p = w as Panel;
                        if (p != null)
                        {
                            _panel = p;
                            break;
                        }
                        w = w.Parent;
                    }
                }

                return _panel;
            }
        }

        public virtual void registerForGraphicUpdate()
        {
            bmp = null;
            registerForRedraw();
            //Interface.registerForGraphicUpdate(this);
        }
        public virtual void registerForRedraw()
        {
            if (layoutIsValid && isVisible)
                Interface.redrawClip.AddRectangle(this.renderBoundsInBackendSurfaceCoordonate);
        }
        public virtual void updatePosition()
        {
            renderBounds.X = bounds.X;
            renderBounds.Y = bounds.Y;
        }
        public virtual Size measureRawSize()
        {
            return bounds.Size;
        }

        public virtual void computeSize()
        {
            Size rawSize = measureRawSize();

            float vRatio = 1f;
            float hRatio = 1f;

            sizeIsValid = true;

            if (bounds.Width == 0)
                if (rawSize.Width == 0)
                {
                    if (horizontalAlignment != go.HorizontalAlignment.Stretch)
                    {
                        Debug.WriteLine("Not able to find width for item");
                        sizeIsValid = false;
                    }
                }
                else
                    renderBounds.Width = rawSize.Width;
            else
                renderBounds.Width = bounds.Width;

            if (bounds.Height == 0)
                if (rawSize.Height == 0)
                {
                    if (verticalAlignment != go.VerticalAlignment.Stretch)
                    {
                        Debug.WriteLine("Not able to find height for item");
                        sizeIsValid = false;
                    }
                }
                else
                    renderBounds.Height = (int)rawSize.Height;
            else
                renderBounds.Height = bounds.Height;

            if (verticalAlignment == VerticalAlignment.Stretch)
            {
                if (Parent != null)
                {
                    if (Parent.sizeIsValid)
                    {
                        Rectangle pcb = Parent.clientBounds;
                        //vRatio = (float)pcb.Height / renderBounds.Height;
                        renderBounds.Height = pcb.Height;
                        //renderBounds.Width = (int)(vRatio * renderBounds.Width);
                    }
                }
                else
                {
                    Debug.WriteLine("parent can't be null for streched item");
                    sizeIsValid = false;
                }
            }

            if (horizontalAlignment == HorizontalAlignment.Stretch)
            {
                if (Parent != null)
                {
                    if (Parent.sizeIsValid)
                    {
                        Rectangle pcb = Parent.clientBounds;
                        //hRatio = (float)pcb.Width / renderBounds.Width;
                        renderBounds.Width = pcb.Width;
                        //renderBounds.Height = (int)(hRatio * renderBounds.Height);
                    }
                }
                else
                {
                    Debug.WriteLine("parent can't be null for streched item");
                    sizeIsValid = false;
                }
            }

        }
        public virtual void updateLayout()
        {
            if (layoutIsValid)
                return;

            Rectangle oldRenderBounds = renderBounds.Clone;



            if (!sizeIsValid)
                computeSize();

            if (!positionIsValid)
            {
                //on aligne par rapport aux parent que si le parent contient une taille
                if (Parent != null)
                {
                    //if (Parent is WrappedWidgetGroup)
                    //    positionIsValid = false;
                    //else
                    {
                        if (Parent.sizeIsValid)
                        {
                            positionIsValid = true;

                            Rectangle pcb = Parent.clientBounds;

                            switch (horizontalAlignment)
                            {
                                case HorizontalAlignment.Stretch:
                                case HorizontalAlignment.Left:
                                    renderBounds.X = pcb.Left;
                                    break;
                                case HorizontalAlignment.Right:
                                    if (sizeIsValid)
                                        renderBounds.X = pcb.Right - renderBounds.Width;
                                    else
                                        positionIsValid = false;
                                    break;
                                case HorizontalAlignment.Center:
                                    if (sizeIsValid)
                                        renderBounds.X = pcb.X + pcb.Width / 2 - renderBounds.Width / 2;
                                    else
                                        positionIsValid = false;
                                    break;
                                case HorizontalAlignment.None:
                                    //if (bounds.X == 0)
                                    //{
                                    //    Debug.WriteLine("Not able to set X position for item");
                                    //    positionIsValid = false;
                                    //}
                                    //else
                                    renderBounds.X = bounds.X;
                                    break;
                            }

                            switch (verticalAlignment)
                            {
                                case VerticalAlignment.Stretch:
                                case VerticalAlignment.Top:
                                    renderBounds.Y = pcb.Top;
                                    break;
                                case VerticalAlignment.Bottom:
                                    if (sizeIsValid)
                                        renderBounds.Y = pcb.Bottom - renderBounds.Height;
                                    else
                                        positionIsValid = false;
                                    break;
                                case VerticalAlignment.Center:
                                    if (sizeIsValid)
                                        renderBounds.Y = pcb.Y + pcb.Height / 2 - renderBounds.Height / 2;
                                    else
                                        positionIsValid = false;
                                    break;
                                case VerticalAlignment.None:
                                    //if (bounds.Y == 0)
                                    //{
                                    //    Debug.WriteLine("Not able to set Y position for item");
                                    //    positionIsValid = false;
                                    //}
                                    //else
                                    renderBounds.Y = bounds.Y;
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    positionIsValid = true;
                    renderBounds.TopLeft = bounds.TopLeft;
                }
            }

            if (layoutIsValid)
                registerForRedraw();
        }
        internal virtual void updateGraphic()
        {
            int stride = 4 * renderBounds.Width;

            int bmpSize = Math.Abs(stride) * renderBounds.Height;
            bmp = new byte[bmpSize];

            using (ImageSurface draw =
                new ImageSurface(bmp, Format.Argb32, renderBounds.Width, renderBounds.Height, stride))
            {
                using (Context gr = new Context(draw))
                {
                    gr.Antialias = Antialias.Subpixel;
                    Rectangle rBack = new Rectangle(renderBounds.Size);// renderBoundsInContextCoordonate.Clone;
                    gr.Color = background;
                    gr.Rectangle(rBack);
                    gr.Fill();

                    if (borderWidth > 0)
                    {
                        rBack.Inflate(-borderWidth / 2, -borderWidth / 2);
                        gr.LineWidth = borderWidth;
                        gr.Color = borderColor;
                        gr.Rectangle(rBack);
                        gr.Stroke();
                    }
                }
                draw.Flush();
                //draw.WriteToPng(directories.rootDir + @"test.png");
            }

            //if (layoutIsValid)
            //    registerForRedraw();
        }
        public virtual void cairoDraw(ref Context ctx, Rectangles clip = null)
        {
            if (!isVisible)
                return;

            if (bmp == null)
                updateGraphic();

            Rectangle tmp;

            tmp = renderBoundsInContextCoordonate;//.Clone;

            int stride = 4 * renderBounds.Width;
            using (ImageSurface source = new ImageSurface(bmp, Format.Argb32, tmp.Width, tmp.Height, stride))
            {

                //ctx.Save();
                //if (Parent != null)
                //{
                //ctx.ResetClip();
                //    //ctx.Rectangle(Parent.clientBounds);
                //    ctx.Rectangle(tmp);
                //    ctx.Clip();
                //}
                ctx.SetSourceSurface(source, tmp.X, tmp.Y);
                ctx.Paint();
                //ctx.Restore();
                //source.WriteToPng(directories.rootDir + @"test.png");
            }
            //ctx.Target.WriteToPng(directories.rootDir + @"test.png");

        }

        #region Keyboard handling
        public virtual void ProcessKeyboard(Key key)
        { }
        #endregion

        #region Mouse handling
        public virtual bool ProcessMousePosition(Point mousePos)
        {
            if (!isVisible)
                return false;

            if (ScreenCoordBounds.Contains(mousePos))
            {
                if (focusable)
                    Interface.hoverWidget = this;
                return true;
            }
            else
            {
                //if (focusable)
                //    Interface.hoverWidget = null;
                return false;
            }
        }
        public virtual void ProcessMouseDown(Point mousePos)
        {
            if (!isVisible)
                return;
            Panel.activeWidget = this;
        }
        public virtual void ProcessMouseUp(Point mousePos)
        {
            //Interface.activeWidget = null;

            if (!isVisible)
                return;
        }

        public virtual void ProcessMouseWeel(int delta)
        {
            if (!isVisible)
                return;
        }
        #endregion

        public override string ToString()
        {
            string tmp = this.GetType().ToString().Split(new char[] { '.' }).Last() + ":-";
            if (!layoutIsValid)
                tmp += "L-";
            //if (Interface.graphicUpdateList.Contains(this))
            //    tmp += "GU-";
            if (Interface.redrawClip.intersect(this.renderBoundsInBackendSurfaceCoordonate))
                tmp += "D-";
            return tmp + string.Format("rb:{0}", renderBounds);
        }
    }
}

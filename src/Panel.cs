using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;

using Cairo;
using OpenTK;

namespace go
{
    public class Panel : Container
    {
        private static GraphicObject _activeWidget;

        public static GraphicObject activeWidget
        {
            get { return _activeWidget; }
            set
            {
                if (_activeWidget != null)
                    _activeWidget.hasFocus = false;
                _activeWidget = value;
                if (_activeWidget != null)
                    _activeWidget.hasFocus = true;
            }
        }

        public override int x
        {
            get { return bounds.X; }
            set
            {
                if (value < 0)
                    bounds.X = 0;
                else if (value > Interface.renderBounds.Right)
                    value = (int)Interface.renderBounds.Right - bounds.Width;
                else
                    bounds.X = value;
            }
        }
        public override int y
        {
            get { return bounds.Y; }
            set
            {
                if (value < 0)
                    bounds.Y = 0;
                else if (value > Interface.renderBounds.Bottom - bounds.Height)
                    bounds.Y = (int)Interface.renderBounds.Bottom - bounds.Height;
                else
                    bounds.Y = value;
            }
        }
        public override bool isVisible
        {
            get { return _isVisible; }
            set
            {
                if (value == _isVisible)
                    return;

                if (!value)//register old clip while visible for backend clearing
                    registerForRedraw();

                _isVisible = value;

                invalidateLayout();

                //callForRedraw();
            }
        }
        bool _cachingInProgress = false;
        public override bool cachingInProgress
        {
            get { return _cachingInProgress; }
            set { _cachingInProgress = value; }
        }

        public override void registerForRedraw()
        {
            base.registerForRedraw();
        }
        //smart array of clipping rectangles
        //public Rectangles redrawClip = new Rectangles();


        public PanelBorderPosition mouseBorderPosition = PanelBorderPosition.ClientArea;

        public override Rectangle renderBoundsInBackendSurfaceCoordonate
        { get { return renderBounds.Clone; } }

        public Panel(Rectangle _bounds)
            : base(_bounds)
        {
            borderWidth = 1;
            margin = 5;
            verticalAlignment = go.VerticalAlignment.None;
            horizontalAlignment = go.HorizontalAlignment.None;
        }


        public void processkLayouting()
        {
            if (!isVisible)
                return;

            while (!this.layoutIsValid)
            {
                
                this.updateLayout();

            }
        }

        public void processDrawing(Context ctx)
        {
            //if (bmp == null)
            //{
            //    updateGraphic();
            //    cachingInProgress = true;
            //}
            //if (redrawClip.count > 0)
            //{
            //    if (!cachingInProgress)  //not full redraw of cache
            //    {
            //        //clip to panel client zone;
            //        ctx.Rectangle(renderBoundsInBackSurfaceCoordonate);
            //        ctx.Clip();
            //        redrawClip.clearAndClip(ctx);
            //    }

            //    cairoDraw(ref ctx);

            //    //clip to panel client zone;
            //    ctx.Rectangle(renderBoundsInBackSurfaceCoordonate);
            //    ctx.Clip();
            //    //ctx.Target.WriteToPng(directories.rootDir + @"test.png");


            //    if (child != null)
            //    {
            //        Rectangle r = child.renderBoundsInBackSurfaceCoordonate;

            //        Rectangles clip = redrawClip.intersectingRects(r);

            //        if (clip.count > 0 || cachingInProgress)
            //            child.cairoDraw(ref ctx, clip);
            //    }

            //    cachingInProgress = false;

            //    ctx.ResetClip();
            //    redrawClip.Reset();
            //}            
        }

        #region widget overides

        //public override void registerForRedraw()
        //{
        //    Interface.redrawClip.AddRectangle(this.renderBoundsInContextCoordonate);
        //}


        #region Mouse handling
        public override void ProcessMouseDown(Point mousePos)
        {
            if (!isVisible)
                return;

            if (mouseBorderPosition == PanelBorderPosition.Closing)
                isVisible = false;

            //base.ProcessMouseDown(mousePos);

            //if (child != null)
            //    child.ProcessMouseDown(mousePos);


        }
        public override bool ProcessMousePosition(Point mousePos)
        {
            if (!isVisible)
                return false;

            if (base.ProcessMousePosition(mousePos))
            {
                if (ScreenCoordClientBounds.Contains(mousePos))
                {
                    mouseBorderPosition = PanelBorderPosition.ClientArea;

                    if (child != null)
                        child.ProcessMousePosition(mousePos);
                }
                else
                {
                    Point m = mousePos;

                    if (this is PanelWithTitle)
                    {
                        if (rectInScreenCoord((this as PanelWithTitle).rClose).Contains(m))
                        {
                            mouseBorderPosition = PanelBorderPosition.Closing;
                            return true;
                        }
                    }

                    Rectangle r = ScreenCoordClientBounds.Clone;

                    if (m.X <= r.X)
                    {
                        if (m.Y <= r.Y)
                            mouseBorderPosition = PanelBorderPosition.TopLeft;
                        else if (m.Y >= r.Bottom)
                            mouseBorderPosition = PanelBorderPosition.BottomLeft;
                        else
                            mouseBorderPosition = PanelBorderPosition.Left;
                    }
                    else if (m.X >= r.Right)
                    {
                        if (m.Y <= r.Y)
                            mouseBorderPosition = PanelBorderPosition.TopRight;
                        else if (m.Y >= r.Bottom)
                            mouseBorderPosition = PanelBorderPosition.BottomRight;
                        else
                            mouseBorderPosition = PanelBorderPosition.Right;
                    }
                    else
                    {
                        if (m.Y <= r.Y)
                        {
                            if (this is PanelWithTitle)
                            {
                                if (m.Y <= r.Y - ((this as PanelWithTitle).titleSize().Height + 2 * borderWidth))
                                    mouseBorderPosition = PanelBorderPosition.Top;
                                else
                                    mouseBorderPosition = PanelBorderPosition.Moving;


                                return true;
                            }
                            else
                                mouseBorderPosition = PanelBorderPosition.Top;
                        }
                        else if (m.Y >= r.Bottom)
                            mouseBorderPosition = PanelBorderPosition.Bottom;
                        else
                            mouseBorderPosition = PanelBorderPosition.ClientArea;
                    }
                }

                return true;
            }
            else
                return false;
        }

        #endregion

        #endregion

        public void updateMouseCursor()
        {
            switch (mouseBorderPosition)
            {
                case PanelBorderPosition.Top:
                    Win32.SetCursor(Cursors.SizeNS.Handle);
                    break;
                case PanelBorderPosition.Left:
                    Win32.SetCursor(Cursors.SizeWE.Handle);
                    break;
                case PanelBorderPosition.Right:
                    Win32.SetCursor(Cursors.SizeWE.Handle);
                    break;
                case PanelBorderPosition.Bottom:
                    Win32.SetCursor(Cursors.SizeNS.Handle);
                    break;
                case PanelBorderPosition.TopLeft:
                    Win32.SetCursor(Cursors.SizeNWSE.Handle);
                    break;
                case PanelBorderPosition.TopRight:
                    Win32.SetCursor(Cursors.SizeNESW.Handle);
                    break;
                case PanelBorderPosition.BottomLeft:
                    Win32.SetCursor(Cursors.SizeNESW.Handle);
                    break;
                case PanelBorderPosition.BottomRight:
                    Win32.SetCursor(Cursors.SizeNWSE.Handle);
                    break;
                case PanelBorderPosition.Moving:
                    Win32.SetCursor(Cursors.SizeAll.Handle);
                    break;
                case PanelBorderPosition.Closing:
                    Win32.SetCursor(Cursors.Default.Handle);
                    break;
                case PanelBorderPosition.ClientArea:
                    //Interface.SetCursor(Cursors.Hand.Handle);
                    break;
                default:
                    break;
            }
        }
        public void putOnTop()
        {
            if (Interface.panels.IndexOf(this) > 0)
            {
                Interface.panels.Remove(this);
                Interface.panels.Insert(0, this);
                this.registerForRedraw();
            }
        }



        public override string ToString()
        {
            string tmp = this.GetType().ToString().Split(new char[] { '.' }).Last() + ":-";
            return tmp + string.Format("rb:{0}", renderBounds);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;

namespace go
{
    public class GroupBox : Container
    {
        public GroupBox(Rectangle _bounds, string _title = "GroupBox")
            : base(_bounds)
        {
            title = _title;
            init();
        }
        public GroupBox(string _title = "GroupBox")
            : base()
        {
            title = _title;
            sizeToContent = true;
            init();
        }
        void init()
        {
            horizontalAlignment = go.HorizontalAlignment.None;
            verticalAlignment = go.VerticalAlignment.None;
            borderColor = Color.White;
            borderWidth = 1;
            margin = 5;        
        }
        string _title;
        public string title
        {
            get { return _title; }
            set
            {
                _title = value;
                registerForGraphicUpdate();
            }
        }

        int _fontSize = 10;
        public int fontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                registerForGraphicUpdate();
            }
        }
        public override Rectangle ClientBoundsInContextCoordonate
        {
            get
            {
                return base.ClientBoundsInContextCoordonate;
            }
        }
        public override Rectangle clientBounds
        {
            get
            {
                Size ts = titleSize();

                Rectangle cb = renderBounds.Clone;
                cb.X = 0;
                cb.Y = ts.Height;
                cb.Height -= ts.Height;
                cb.Inflate(-borderWidth - margin, -borderWidth - margin);
                
                return cb;
            }
        }
        public Size titleSize()
        {
#if _WIN32 || _WIN64
            byte[] txt = System.Text.UTF8Encoding.UTF8.GetBytes(_title);
#endif

            Size s;

            using (Context gr = new Context(new ImageSurface(Format.Argb32, 1, 1)))
            {
                gr.SetFontSize(fontSize);
                TextExtents te;
#if _WIN32 || _WIN64
                te = gr.TextExtents(txt);
#elif __linux__
                te =  gr.TextExtents(title);
#endif
                FontExtents fe = gr.FontExtents;
                s = new Size((int)Math.Ceiling(te.XAdvance), (int)Math.Ceiling(fe.Height));
            }
            return s;// +borderWidth;
        }

        public override void updateLayout()
        {
            if (!layoutIsValid)
            {
                base.updateLayout();

                if (layoutIsValid)
                    renderBounds.Height += titleSize().Height;//??????????
            }
        }
        internal override void updateGraphic()
        {
            int stride = 4 * renderBounds.Width;

            int bmpSize = Math.Abs(stride) * renderBounds.Height;
            bmp = new byte[bmpSize];

            byte[] txt = System.Text.UTF8Encoding.UTF8.GetBytes(title);// utf = new System.Text.Decoder();

            using (ImageSurface draw = new ImageSurface(bmp, Format.Argb32, renderBounds.Width, renderBounds.Height, stride))
            {
                using (Context gr = new Context(draw))
                {


                    Rectangle r = new Rectangle(renderBounds.Size);
                    //gr.Rotate(Math.PI);
                    gr.SetFontSize(fontSize);
                    FontExtents fe = gr.FontExtents;
                    TextExtents te = gr.TextExtents(title);
                    //  double a = Math.PI;
                    //gr.Transform(new c.Matrix(Math.Cos(a),-Math.Sin(a),Math.Sin(a),Math.Cos(a),renderBounds.Width,renderBounds.Height));
                    gr.Antialias = Antialias.Subpixel;
                    gr.LineWidth = borderWidth;
                    gr.Color = background;
                    //gr.MoveTo(renderBounds.X+1,renderBounds.Y+1);


                    gr.Rectangle(r);
                    gr.Fill();

                    Rectangle rTitle = r.Clone;

                    int th = (int)Math.Ceiling(fe.Height / 2);
                    r.Y += th;
                    r.Height -= th;

                    const int titleGap = 5;

                    if (borderWidth > 0)
                    {
                        gr.Color = borderColor;
                        gr.LineWidth = borderWidth;

                        r.Inflate(-borderWidth / 2, -borderWidth / 2);

                        
                        rTitle.X = r.X + titleGap;
                        rTitle.Width = (int)Math.Ceiling(te.XAdvance) + 2 * titleGap;
                        rTitle.Height = (int)Math.Ceiling(fe.Height);
                        gr.Save();
                        gr.FillRule = FillRule.EvenOdd;
                        gr.Rectangle(new Rectangle(renderBounds.Size));
                        gr.Rectangle(rTitle);
                        gr.Clip();


                        gr.Rectangle(r);
                        gr.Stroke();
                        gr.Restore();
                    }

                    gr.MoveTo(rTitle.X + titleGap, rTitle.Y + (int)Math.Ceiling(fe.Height - fe.Descent));

#if _WIN32 || _WIN64
                    gr.ShowText(txt);
#elif __linux__
					gr.ShowText(title);
#endif
                    gr.Color = borderColor;
                    gr.FillExtents();

                }
                //draw.Flush();
                //draw.WriteToPng(directories.rootDir + @"test.png");
            }

            //registerForRedraw();
        }
        public override void cairoDraw(ref Context ctx, Rectangles clip = null)
        {
            base.cairoDraw(ref ctx, clip);
        }
        //public override void cairoDraw(ref Context ctx, Rectangles clip = null)
        //{
        //    if (!isVisible)//check if necessary??
        //        return;

        //    base.cairoDraw(ref ctx, clip);

        //    if (clip != null)
        //        clip.Rebase(this);

        //    if (child != null)
        //        child.cairoDraw(ref ctx, clip);

        //    //ctx.Target.WriteToPng(@"/home/jp/test.png");
        //}

        public override string ToString()
        {
            return this.title + ":" + base.ToString();
        }
    }
}


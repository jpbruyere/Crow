using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;

namespace go
{
    public class PanelWithTitle : Panel
    {
        public PanelWithTitle(Rectangle _bounds)
            : base(_bounds)
        {                        
            background = Color.DimGray;
        }

        string _title = "Panel";
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
        public Color titleBackground = new Color(0.5, 0.5, 0.7, 0.8);
        public Color titleTextColor = new Color(0.9, 0.9, 0.9, 1);
		public Size titleSize ()
		{
#if _WIN32 || _WIN64
            byte[] txt = System.Text.UTF8Encoding.UTF8.GetBytes(_title);
#endif
            
            Size s;

            using (Context gr = new Context(new ImageSurface(Format.Argb32,1,1)))
            {
                gr.SetFontSize(fontSize);
				TextExtents te;
#if _WIN32 || _WIN64
                te = gr.TextExtents(txt);
#elif __linux__
                te =  gr.TextExtents(title);
#endif
                FontExtents fe = gr.FontExtents;
                s = new Size((int)Math.Ceiling(te.XAdvance),(int)Math.Ceiling(fe.Height));
            }
            return s;// +borderWidth;
        }
        public override Rectangle clientBounds
        {
            get
            {
                Size st = titleSize() + borderWidth*2;
                Rectangle cb = bounds.Clone;
                cb.X = 0;
                cb.Y = st.Height;
                cb.Height -= st.Height;
                cb.Inflate(-borderWidth-margin, -borderWidth-margin);

                //cb.Y += titleHeight;
                return cb;
            }
        }

        internal Rectangle rClose = new Rectangle();//close button
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
                    
                    
                    Rectangle r = new Rectangle(0, 0, renderBounds.Width, renderBounds.Height);
                    //gr.Rotate(Math.PI);
                    gr.SetFontSize(fontSize);
					FontExtents fe = gr.FontExtents;
                    //  double a = Math.PI;
                    //gr.Transform(new c.Matrix(Math.Cos(a),-Math.Sin(a),Math.Sin(a),Math.Cos(a),renderBounds.Width,renderBounds.Height));
                    gr.Antialias = Antialias.Subpixel;                    
                    gr.LineWidth = borderWidth;
                    gr.Color = background;
                    //gr.MoveTo(renderBounds.X+1,renderBounds.Y+1);
                    gr.Rectangle(r);
                    gr.Fill();
                    draw.Flush();
                    
                    Rectangle rTitle = r.Clone;
                    rTitle.Height = titleSize().Height + borderWidth * 2;                    
                    gr.Color = Color.blue1;
                    gr.Rectangle(rTitle);
                    gr.Fill();
                    rTitle.Inflate(-borderWidth / 2, -borderWidth / 2);
                    gr.Rectangle(rTitle);
                    gr.Color = borderColor;
                    gr.Stroke();

                    r.Inflate(-borderWidth / 2, -borderWidth / 2);
                    gr.Rectangle(r);
                    gr.Color = borderColor;
                    gr.Stroke();
                    //gr.TextExtents(txt);


                    gr.MoveTo(borderWidth + 1, fe.Height - fe.Descent + borderWidth);
#if _WIN32 || _WIN64
                    gr.ShowText(txt);
#elif __linux__
					gr.ShowText(title);
#endif
                    gr.Fill();
                    //gr.Stroke();  
                    gr.LineWidth = 1;

                    rClose = rTitle.Clone;
                    rClose.Width = rClose.Height;                                        
                    rClose.Left = rTitle.Right - rClose.Width;
                    rClose.Inflate(-6, -6);
                    gr.MoveTo(rClose.X, rClose.Y);
                    gr.LineTo(rClose.Right, rClose.Bottom);
                    gr.MoveTo(rClose.X, rClose.Bottom);
                    gr.LineTo(rClose.Right, rClose.Top);
                    gr.Stroke();
                    rClose.Inflate(3, 3);
                    gr.Rectangle(rClose);
                    gr.Stroke();
                }
                draw.Flush();
                //draw.WriteToPng(directories.rootDir + @"test.png");
            }

            //registerForRedraw();
        }
        public override string ToString()
        {
            return this.title + ":" + base.ToString();
        }
    }    
}

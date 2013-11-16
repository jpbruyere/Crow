using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Cairo;

namespace go
{
    public class Label : GraphicObject
    {
        //static constructor to init label specific theme value
        public static class Theme
        {
            public static Color background = Color.Transparent;
            public static Color borderColor = Color.White;
            public static int margin = 0;
            public static int borderWidth = 0;
            public static VerticalAlignment verticalAlignment = VerticalAlignment.None;
            public static HorizontalAlignment horizontalAlignment = HorizontalAlignment.None;
            public static bool sizeToContent = false;
            //label specific
            public static int fontSize = 10;
            public static Color fontColor = Color.White;
        }

        public Label(string _text)
            : base()
        {
            init();
            updateFont();
            text = _text;

        }

        void init()
        {
            background = Theme.background;
            borderColor = Theme.borderColor;
            borderWidth = Theme.borderWidth;
            margin = Theme.margin;
            horizontalAlignment = Theme.horizontalAlignment;
            verticalAlignment = Theme.verticalAlignment;
            sizeToContent = Theme.sizeToContent;
            fontSize = Theme.fontSize;
            fontColor = Theme.fontColor;
        }



        protected string _text = "label";
        int _fontSize;
        Color _fontColor;

        public string text
        {
            get { return _text; }
            set
            {
                if (_text == value)
                    return;

                registerForGraphicUpdate();

                //if (value.Length != _text.Length)
                //    layoutIsValid = false;

                _text = value;                
            }
        }
        public int fontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                registerForGraphicUpdate();
            }
        }        
        public Color fontColor
        {
            get { return _fontColor; }
            set 
            { 
                _fontColor = value;
                registerForGraphicUpdate();
            }
        }

        public byte[] utf8Text
        {
            get { return System.Text.UTF8Encoding.UTF8.GetBytes(text); }
        }
        
        protected Rectangle rText;
		
        //public FontStyle fontStyle
        //{
        //    get { return _fontStyle; }
        //    set
        //    {
        //        _fontStyle = value;
        //        updateFont();
        //    }
        //}
        //public FontFamily fontFamily
        //{
        //    get { return _fontFamily; }
        //    set
        //    {
        //        _fontFamily = value;
        //        updateFont();
        //    }
        //}

        

        //Font TextFont;

        public Alignment textAlignment = Alignment.LeftCenter;



        void updateFont()
        {
            //TextFont = new Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Pixel);
            bmp = null;
        }

        public override Size measureRawSize()
        {
            byte[] txt = utf8Text;
            
            Size s;

            using (Context gr = new Context(new ImageSurface(Format.Argb32,1,1)))
            {
                gr.SetFontSize(fontSize);
				TextExtents te;
#if _WIN32 || _WIN64
                te =  gr.TextExtents(utf8Text);
#elif __linux__
                te =  gr.TextExtents(text);
#endif
                FontExtents fe = gr.FontExtents;
                s = new Size((int)Math.Ceiling(te.XAdvance),(int)Math.Ceiling(fe.Height));
            }
            return s;// +borderWidth;
        }

        internal override void updateGraphic()
        {
            byte[] txt = utf8Text;
            int stride = 4 * renderBounds.Width;


            //init  bmp with widget background and border
            base.updateGraphic();

            using (ImageSurface bitmap =
                new ImageSurface(bmp, Format.Argb32, renderBounds.Width, renderBounds.Height, stride))
            {
                using (Context gr = new Context(bitmap))
                {
                    gr.FontOptions.Antialias = Antialias.Subpixel;
                    gr.FontOptions.HintMetrics = HintMetrics.On;
                    gr.SetFontSize(fontSize);
                    FontExtents fe = gr.FontExtents;
                    rText = new Rectangle(new Point(0,0), measureRawSize());
                    //gr.Rotate(Math.PI);
                    
                    //  double a = Math.PI;
                    //gr.Transform(new c.Matrix(Math.Cos(a),-Math.Sin(a),Math.Sin(a),Math.Cos(a),renderBounds.Width,renderBounds.Height));
                    gr.Antialias = Antialias.Subpixel;
                    gr.LineWidth = borderWidth;

                    float widthRatio = 1f;
                    float heightRatio = 1f;

                    Rectangle cb = clientBounds;

                    //ignore text alignment if size to content = true
                    if (!sizeToContent)
                    {
                        switch (textAlignment)
                        {
                            case Alignment.None:
                                break;
                            case Alignment.TopLeft:     //ok
                                rText.X = cb.X;
                                rText.Y = cb.Y;
                                break;
                            case Alignment.TopCenter:   //ok
                                rText.Y = cb.Y;
                                rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
                                break;
                            case Alignment.TopRight:    //ok
                                rText.X = cb.Right - rText.Width;
                                rText.Y = cb.Y;
                                break;
                            case Alignment.TopStretch://ok
                                heightRatio = widthRatio = (float)cb.Width / rText.Width;
                                rText.X = cb.X;
                                rText.Y = cb.Y;
                                rText.Width = cb.Width;
                                rText.Height = (int)(rText.Height * heightRatio);
                                break;
                            case Alignment.LeftCenter://ok
                                rText.X = cb.X;
                                rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
                                break;
                            case Alignment.LeftStretch://ok
                                heightRatio = widthRatio = (float)cb.Height / rText.Height;
                                rText.X = cb.X;
                                rText.Y = cb.Y;
                                rText.Height = cb.Height;
                                rText.Width = (int)(widthRatio * cb.Width);
                                break;
                            case Alignment.RightCenter://ok
                                rText.X = cb.X + cb.Width - rText.Width;
                                rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
                                break;
                            case Alignment.RightStretch://ok
                                heightRatio = widthRatio = (float)cb.Height / rText.Height;
                                rText.Height = cb.Height;
                                rText.Width = (int)(widthRatio * cb.Width);
                                rText.X = cb.X;
                                rText.Y = cb.Y;
                                break;
                            case Alignment.BottomCenter://ok
                                rText.X = cb.Width / 2 - rText.Width / 2;
                                rText.Y = cb.Height - rText.Height;
                                break;
                            case Alignment.BottomStretch://ok
                                heightRatio = widthRatio = (float)cb.Width / rText.Width;
                                rText.Width = cb.Width;
                                rText.Height = (int)(rText.Height * heightRatio);
                                rText.Y = cb.Bottom - rText.Height;
                                rText.X = cb.X;
                                break;
                            case Alignment.BottomLeft://ok
                                rText.X = cb.X;
                                rText.Y = cb.Bottom - rText.Height;
                                break;
                            case Alignment.BottomRight://ok
                                rText.Y = cb.Bottom - rText.Height;
                                rText.X = cb.Right - rText.Width;
                                break;
                            case Alignment.Center://ok
                                rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
                                rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
                                break;
                            case Alignment.Fit://ok, peut être mieu aligné                            
                                widthRatio = (float)cb.Width / rText.Width;
                                heightRatio = (float)cb.Height / rText.Height;
                                rText = cb.Clone;
                                break;
                            case Alignment.HorizontalStretch://ok
                                heightRatio = widthRatio = (float)cb.Width / rText.Width;
                                rText.Width = cb.Width;
                                rText.Height = (int)(heightRatio * rText.Height);
                                rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
                                rText.X = cb.X;
                                break;
                            case Alignment.VerticalStretch://ok
                                heightRatio = widthRatio = (float)cb.Height / rText.Height;
                                rText.Height = cb.Height;
                                rText.Width = (int)(widthRatio * rText.Width);
                                rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
                                rText.Y = cb.Y;
                                break;
                            default:
                                break;
                        }
                    }
                                      

                    gr.Color = fontColor;

                    gr.FontMatrix = new Matrix(widthRatio * fontSize, 0, 0, heightRatio * fontSize, 0, 0);
                    
                    fe = gr.FontExtents;  

                    gr.MoveTo(rText.X, rText.Y + fe.Ascent);
#if _WIN32 || _WIN64
                    gr.ShowText(txt);
#elif __linux__
                    gr.ShowText(text);
#endif

                    gr.Fill();
                    
                }
                bitmap.Flush();
                //bitmap.WriteToPng(directories.rootDir + @"test.png");
            }

            //registerForRedraw();
        }

    }
}

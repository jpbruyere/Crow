using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Cairo;
using System.Text.RegularExpressions;

namespace go
{
    [Serializable]
    public class Label : GraphicObject
    {
        //TODO:change protected to private
        
		protected string _text = "label";
        Alignment _textAlignment = Alignment.LeftCenter;
        int _fontSize = 12;
		bool _multiline = false;

        protected Rectangle rText;
		protected float widthRatio = 1f;
		protected float heightRatio = 1f;
		protected FontExtents fe;
		protected TextExtents te;

		//public string FontFace = "MagicMedieval";
		public string FontFace = "droid";


		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(-1)]
		public override int Width {
			get { return base.Width; }
			set { base.Width = value; }
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(-1)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(2)]
		public virtual int Margin {
			get { return base.Margin; }
			set { base.Margin = value; }
		}

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValue(Alignment.LeftCenter)]
		public Alignment TextAlignment
        {
            get { return _textAlignment; }
            set { _textAlignment = value; }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text == value)
                    return;

                
                registerForGraphicUpdate();
                InvalidateLayout();

                _text = value;
            }
        }
						
        [System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(10)]
		public int FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                registerForGraphicUpdate();
            }
        }

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(false)]
		public bool Multiline
		{
			get { return _multiline; }
			set
			{
				_multiline = value;
				registerForGraphicUpdate();
			}
		}

        public Label()
        { 

		}

        public Label(string _text)
            : base()
        {
			//updateFont();
            Text = _text;
        }

        void updateFont()
        {
            //TextFont = new Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Pixel);

            bmp = null;
        }
			
		string[] getLines {
			get {
				return _multiline ?
					Regex.Split (_text, "\r\n|\r|\n") :
					new string[] { _text };
			}
		}

        public override Size measureRawSize()
        {
			Size s;


			if (string.IsNullOrEmpty(_text))
				_text = "";
				
			string[] lines = getLines;
				
			using (ImageSurface img = new ImageSurface (Format.Argb32, 1, 1)) {
				using (Context gr = new Context (img)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();
					gr.SelectFontFace (FontFace, FontSlant.Normal, FontWeight.Normal);
					gr.SetFontSize (FontSize);

					te = new TextExtents();

					foreach (string str in lines) {
#if _WIN32 || _WIN64
					TextExtents tmp = gr.TextExtents(str.ToUtf8());
#elif __linux__
						TextExtents tmp = gr.TextExtents (str);
#endif
						if (tmp.XAdvance > te.XAdvance)
							te = tmp;
					}
					fe = gr.FontExtents;
					s = new Size ((int)Math.Ceiling (te.XAdvance) + (BorderWidth + Margin) * 2, (int)Math.Ceiling (fe.Height) * lines.Length + (BorderWidth + Margin) * 2);
				}
			}
            return s;// +borderWidth;
        }
		public override void onDraw (Context gr)
		{
			base.onDraw (gr);
			string[] lines = getLines;

			gr.SelectFontFace(FontFace, FontSlant.Normal, FontWeight.Normal);

			gr.LineWidth = BorderWidth;
			gr.Antialias = Antialias.Subpixel;
			//gr.FontOptions.Antialias = Antialias.Subpixel;
			//gr.FontOptions.HintMetrics = HintMetrics.On;
			gr.SetFontSize(FontSize);

			rText = new Rectangle(new Point(0, 0),new Size((int)te.Width,(int)fe.Height * lines.Length));

			widthRatio = 1f;
			heightRatio = 1f;

			Rectangle cb = clientBounds;

			//ignore text alignment if size to content = true
			if (Bounds.Size < 0)
			{
				rText.X = cb.X;
				rText.Y = cb.Y;
			}else{
				switch (TextAlignment)
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
					rText = cb;
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

			gr.Color = Foreground;

			gr.FontMatrix = new Matrix(widthRatio * FontSize, 0, 0, heightRatio * FontSize, 0, 0);

			for (int i = 0; i < lines.Length; i++) {
				gr.MoveTo(rText.X, rText.Y + fe.Ascent + fe.Height * i);
				#if _WIN32 || _WIN64
				gr.ShowText(lines[i].ToUtf8());
				#elif __linux__
				gr.ShowText(lines[i]);
				#endif
			}						
			gr.Fill();
		}
    }
}

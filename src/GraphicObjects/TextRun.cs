using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Cairo;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.ComponentModel;
using OpenTK.Input;

namespace Crow
{
    [Serializable]
    public class TextRun : GraphicObject
    {
		#region CTOR
		public TextRun()
		{ 

		}
		public TextRun(string _text)
			: base()
		{
			Text = _text;
		}
		#endregion

        //TODO:change protected to private
        
		#region private and protected fields
		protected string _text = "label";
        Alignment _textAlignment = Alignment.LeftCenter;        
		bool _multiline;
		bool wordWrap;
        protected Rectangle rText;
		protected float widthRatio = 1f;
		protected float heightRatio = 1f;
		protected FontExtents fe;
		protected TextExtents te;
		#endregion


        [XmlAttributeAttribute()][DefaultValue(Alignment.LeftCenter)]
		public Alignment TextAlignment
        {
            get { return _textAlignment; }
            set { _textAlignment = value; }
        }
		[XmlAttributeAttribute()][DefaultValue("label")]
        public string Text
        {
            get {				
				return lines == null ? 
					_text : lines.Aggregate((i, j) => i + Interface.LineBreak + j);
			}
            set
            {
                if (_text == value)
                    return;
					                
                registerForGraphicUpdate();
				this.RegisterForLayouting (LayoutingType.Sizing);


                _text = value;

				if (string.IsNullOrEmpty(_text))
					_text = "";

				lines = getLines;
            }
        }
		[XmlAttributeAttribute()][DefaultValue(false)]
		public bool Multiline
		{
			get { return _multiline; }
			set
			{
				_multiline = value;
				registerForGraphicUpdate();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public bool WordWrap {
			get {
				return wordWrap;
			}
			set {
				if (wordWrap == value)
					return;
				wordWrap = value;
				registerForGraphicUpdate();
			}
		}

		List<string> lines;
		List<string> getLines {
			get {				
				return _multiline ?
					Regex.Split (_text, "\r\n|\r|\n").ToList() :
					new List<string>(new string[] { _text });
			}
		}

		#region GraphicObject overrides
		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Width {
			get { return base.Width; }
			set { base.Width = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(2)]
		public override int Margin {
			get { return base.Margin; }
			set { base.Margin = value; }
		}

		protected override Size measureRawSize()
        {
			Size size;

			if (lines == null)
				lines = getLines;
				
			using (ImageSurface img = new ImageSurface (Format.Argb32, 10, 10)) {
				using (Context gr = new Context (img)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();

					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);

					te = new TextExtents();

					foreach (string s in lines) {
						string l = s.Replace("\t", new String (' ', Interface.TabSize));

#if _WIN32 || _WIN64
						TextExtents tmp = gr.TextExtents(str.ToUtf8());
#elif __linux__
						TextExtents tmp = gr.TextExtents (l);
#endif
						if (tmp.XAdvance > te.XAdvance)
							te = tmp;
					}
					fe = gr.FontExtents;
					int lc = lines.Count;
					//ensure minimal height = text line height
					if (lc == 0)
						lc = 1; 
					size = new Size ((int)Math.Ceiling (te.XAdvance) + Margin * 2, (int)(fe.Height * lc) + Margin*2);
				}
			}

            return size;;
        }
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);

			gr.Antialias = Antialias.Subpixel;
			//gr.FontOptions.Antialias = Antialias.Subpixel;
			//gr.FontOptions.HintMetrics = HintMetrics.On;

			rText = new Rectangle(measureRawSize());
			rText.Width -= 2 * Margin;
			rText.Height -= 2 * Margin;

			widthRatio = 1f;
			heightRatio = 1f;

			Rectangle cb = ClientRectangle;

			//ignore text alignment if size to content = true
			//or if text size is larger than client bounds
			if (Bounds.Size < 0 || rText.Width > cb.Width)
			{
				rText.X = cb.X;
				rText.Y = cb.Y;
			}else {
				
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

			gr.FontMatrix = new Matrix(widthRatio * Font.Size, 0, 0, heightRatio * Font.Size, 0, 0);


			int curLineCount = 0;
			for (int i = 0;i < lines.Count;i++) {				
				string l = lines [i].Replace ("\t", new String (' ', Interface.TabSize));
				List<string> wl = new List<string> ();
				int lineLength = (int)gr.TextExtents (l).XAdvance;

				if (wordWrap && lineLength > cb.Width) {
					string tmpLine = "";
					int curChar = 0;
					while (curChar < l.Length) {
						tmpLine += l [curChar];
						if ((int)gr.TextExtents (tmpLine).XAdvance > cb.Width) {
							tmpLine = tmpLine.Remove (tmpLine.Length - 1);
							wl.Add (tmpLine);
							tmpLine = "";
							continue;
						}
						curChar++;
					}
					wl.Add (tmpLine);
				} else
					wl.Add (l);

				foreach (string ll in wl) {
					lineLength = (int)gr.TextExtents (ll).XAdvance;
									

					if (string.IsNullOrWhiteSpace (ll)) {
						curLineCount++;
						continue;
					}

					Foreground.SetAsSource (gr);	
					gr.MoveTo (rText.X, rText.Y + fe.Ascent + fe.Height * curLineCount);

					#if _WIN32 || _WIN64
					gr.ShowText(ll.ToUtf8());
					#elif __linux__
					gr.ShowText (ll);
					#endif
					gr.Fill ();

					curLineCount++;
						
				}
			}						
		}
		#endregion
    }
}

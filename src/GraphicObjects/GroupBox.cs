using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
{

    public class GroupBox : Container
    {
        string _title;
        int _fontSize = 10;

        [XmlAttributeAttribute]
        [DefaultValue("GroupBox")]
        public string Text
        {
            get { return _title; }
            set
            {
                _title = value;
                registerForGraphicUpdate();
            }
        }
        
        [XmlAttributeAttribute]
        [DefaultValue(10)]
        public int fontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                registerForGraphicUpdate();
            }
        }

        public Size titleSize()
        {
#if _WIN32 || _WIN64
            byte[] txt = System.Text.UTF8Encoding.UTF8.GetBytes(_title);
#endif

            Size s;

			using (ImageSurface img = new ImageSurface (Format.Argb32, 1, 1)) {
				using (Context gr = new Context (img)) {
					gr.SetFontSize (fontSize);
					TextExtents te;
#if _WIN32 || _WIN64
                te = gr.TextExtents(txt);
#elif __linux__
					te = gr.TextExtents (Text);
#endif
					FontExtents fe = gr.FontExtents;
					s = new Size ((int)Math.Ceiling (te.XAdvance), (int)Math.Ceiling (fe.Height));
				}
			}
            return s;// +borderWidth;
        }

		public GroupBox() : base (){
		}
        public GroupBox(Rectangle _bounds, string _title = "GroupBox")
            : base(_bounds)
        {
            Text = _title;
        }
        
        public GroupBox(string _title = "GroupBox")
            : base()
        {
            Text = _title;            
        }
        
        public override Rectangle clientBounds
        {
            get
            {
                Size ts = titleSize();

                Rectangle cb = Slot;
                cb.X = 0;
                cb.Y = ts.Height;
                cb.Height -= ts.Height;
                cb.Inflate(-BorderWidth - Margin);
                
                return cb;
            }
        }        

		public override void onDraw (Context gr)
		{
			//base.onDraw (gr);

			byte[] txt = System.Text.UTF8Encoding.UTF8.GetBytes(Text);
			Rectangle r = new Rectangle(Slot.Size);
			//gr.Rotate(Math.PI);
			gr.SetFontSize(fontSize);
			FontExtents fe = gr.FontExtents;
			TextExtents te = gr.TextExtents(Text);

			gr.Antialias = Antialias.Subpixel;
			gr.LineWidth = BorderWidth;
			gr.Color = Background;

			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();

			Rectangle rTitle = r;

			int th = (int)Math.Ceiling(fe.Height / 2);
			r.Y += th;
			r.Height -= th;

			const int titleGap = 5;

			if (BorderWidth > 0)
			{
				gr.Color = BorderColor;
				gr.LineWidth = BorderWidth;

				r.Inflate(-BorderWidth / 2, -BorderWidth / 2);

				rTitle.X = r.X + titleGap;
				rTitle.Width = (int)Math.Ceiling(te.XAdvance) + 2 * titleGap;
				rTitle.Height = (int)Math.Ceiling(fe.Height);
				gr.Save();
				gr.FillRule = FillRule.EvenOdd;
				gr.Rectangle(new Rectangle(Slot.Size));
				gr.Rectangle(rTitle);
				gr.Clip();

				CairoHelpers.CairoRectangle (gr, r, CornerRadius);

				gr.Stroke();
				gr.Restore();
			}

			gr.MoveTo(rTitle.X + titleGap, rTitle.Y + (int)Math.Ceiling(fe.Height - fe.Descent));

			#if _WIN32 || _WIN64
			gr.ShowText(txt);
			#elif __linux__
			gr.ShowText(Text);
			#endif
			gr.Color = Foreground;
			gr.Fill();
		}

        public override string ToString()
        {
            return this.Text + ":" + base.ToString();
        }
    }
}


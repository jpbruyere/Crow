using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using Cairo;

namespace go
{

	public class GroupBox : Border
    {
		#region CTOR
		public GroupBox() : base (){
		}
		public GroupBox(string _title = "GroupBox")
			: base()
		{
			Text = _title;            
		}
		#endregion

		#region private fields
        string _title;
		Font _font;
		#endregion

		#region public properties
        [XmlAttributeAttribute][DefaultValue("GroupBox")]
        public string Text
        {
            get { return _title; }
            set
            {
                _title = value;
                registerForGraphicUpdate();
            }
        }        
		[XmlAttributeAttribute()][DefaultValue("droid,12")]
		public Font Font {
			get { return _font; }
			set { _font = value; }
		}
		#endregion

        Size titleSize {
			get {
#if _WIN32 || _WIN64
            byte[] txt = System.Text.UTF8Encoding.UTF8.GetBytes(_title);
#endif

				Size s;

				using (ImageSurface img = new ImageSurface (Format.Argb32, 1, 1)) {
					using (Context gr = new Context (img)) {
						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);
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
		}

		#region GraphicObject overrides
		public override Rectangle ClientRectangle {
			get {
				Size ts = titleSize;
				Rectangle cb = Slot.Size;
				cb.Y = ts.Height;
				cb.Height -= ts.Height;
				cb.Inflate ( - Margin);
				return cb;
			}
		}			
		protected override Size measureRawSize ()
		{
			Size raw = base.measureRawSize ();
			return raw > 0 ? raw + titleSize : raw;
		}
		protected override void onDraw (Context gr)
		{
			//base.onDraw (gr);

			byte[] txt = System.Text.UTF8Encoding.UTF8.GetBytes(Text);
			Rectangle r = new Rectangle(Slot.Size);
			if (BorderWidth > 0) 
				r.Inflate (-BorderWidth / 2);

			//gr.Rotate(Math.PI);
			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			FontExtents fe = gr.FontExtents;
			TextExtents te = gr.TextExtents(Text);

			gr.Antialias = Antialias.Subpixel;
			gr.LineWidth = BorderWidth;
			gr.Color = Background;



			Rectangle rTitle = r;

			int th = (int)Math.Ceiling(fe.Height / 2);
			r.Y += th;
			r.Height -= th;

			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();

			const int titleGap = 5;

			if (BorderWidth > 0)
			{
				gr.Color = BorderColor;

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
		#endregion

        public override string ToString()
        {
			return base.ToString() + ": " + this.Text;
        }
    }
}


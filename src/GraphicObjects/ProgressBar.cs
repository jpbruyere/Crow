using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.Diagnostics;

namespace go
{
	[Serializable]
	public class ProgressBar : GraphicObject
    {
		int _currentValue = 0;
		int _minimum = 0;
		int _maximum = 100;

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue("Gray")]
		public override Color Background {
			get { return base.Background; }
			set { base.Background = value; }
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
		public override int BorderWidth {
			get { return base.BorderWidth; }
			set { base.BorderWidth = value; }
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue("BlueCrayola")]
		public virtual Color Foreground {
			get { return base.Foreground; }
			set { base.Foreground = value; }
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
        public int Value
        {
            get { return _currentValue; }
            set
            {
                if (_currentValue == value)
                    return;

                _currentValue = value;

                if (_currentValue > Maximum)
                    _currentValue = Maximum;

                registerForGraphicUpdate();
            }
        }

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
		public int Minimum {
			get {return _minimum;}
			set {
				if (_minimum == value || _minimum >= Maximum)
					return;
				_minimum = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(100)]
		public int Maximum {
			get {return _maximum;}
			set {
				if (_maximum == value || _maximum <= _minimum)
					return;
				_maximum = value;
				registerForGraphicUpdate();
			}
		}
               
		public ProgressBar() : base(){}

		public ProgressBar(int _max) : base()
        {
            Maximum = _max;
        }

		public override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Rectangle rBack = Slot.Size;

			if (BorderWidth > 0)
				rBack.Inflate(-BorderWidth, -BorderWidth);
			rBack.Width = (int)((double)rBack.Width / Maximum * Value);

			gr.Color = Foreground;

			CairoHelpers.CairoRectangle(gr,rBack,CornerRadius);
			gr.Fill();
		}
		public override void Paint (ref Context ctx, Rectangles clip)
		{
			base.Paint (ref ctx, clip);
//			if (clip != null)
//				clip.stroke (ctx, Color.Amethyst);
		}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.Diagnostics;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
{
	[Serializable]
	public class ProgressBar : Border
    {
		#region CTOR
		public ProgressBar() : base(){}
		public ProgressBar(int _max) : base()
		{
			Maximum = _max;
		}
		#endregion

		#region private fields
		int _currentValue = 0;
		int _minimum = 0;
		int _maximum = 100;
		#endregion

		#region public properties
		[XmlAttributeAttribute()][DefaultValue(0)]
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
		[XmlAttributeAttribute()][DefaultValue(0)]
		public int Minimum {
			get {return _minimum;}
			set {
				if (_minimum == value || _minimum >= Maximum)
					return;
				_minimum = value;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(100)]
		public int Maximum {
			get {return _maximum;}
			set {
				if (_maximum == value || _maximum <= _minimum)
					return;
				_maximum = value;
				registerForGraphicUpdate();
			}
		}
		#endregion

		#region GraphicObject overrides
		[XmlAttributeAttribute()][DefaultValue("Gray")]
		public override Color Background {
			get { return base.Background; }
			set { base.Background = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public override int BorderWidth {
			get { return base.BorderWidth; }
			set { base.BorderWidth = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("BlueCrayola")]
		public virtual Color Foreground {
			get { return base.Foreground; }
			set { base.Foreground = value; }
		}

		protected override void onDraw (Context gr)
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
		#endregion
    }
}

using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
{
	public class Border : Container
	{
		#region CTOR
		public Border () : base(){}
		#endregion

		#region private fields
		Color _borderColor;
		int _borderWidth;
		#endregion

		#region public properties
		[XmlAttributeAttribute()][DefaultValue(1)]
		public virtual int BorderWidth {
			get { return _borderWidth; }
			set {
				_borderWidth = value;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("White")]
		public virtual Color BorderColor {
			get { return _borderColor; }
			set {
				_borderColor = value;
				registerForGraphicUpdate ();
			}
		}
		#endregion

		#region GraphicObject override
		[XmlIgnore]public override Rectangle ClientRectangle {
			get {
				Rectangle cb = base.ClientRectangle;
				cb.Inflate (- BorderWidth);
				return cb;
			}
		}

		protected override Size measureRawSize ()
		{
			Size raw = Bounds.Size;

			if (Child != null) {
				if (Bounds.Width < 0)
					raw.Width = Child.Slot.Width + 2 * (Margin+BorderWidth);
				if (Bounds.Height < 0)
					raw.Height = Child.Slot.Height + 2 * (Margin+BorderWidth);
			}

			return raw;
		}
		protected override void onDraw (Cairo.Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);

			if (BorderWidth > 0) 
				rBack.Inflate (-BorderWidth / 2);

			gr.Color = Background;
			CairoHelpers.CairoRectangle(gr,rBack,CornerRadius);
			gr.Fill ();

			if (BorderWidth > 0) {

				gr.LineWidth = BorderWidth;
				gr.Color = BorderColor;
				CairoHelpers.CairoRectangle(gr,rBack,CornerRadius);
				gr.Stroke ();
			}
		}		
		#endregion
	}
}


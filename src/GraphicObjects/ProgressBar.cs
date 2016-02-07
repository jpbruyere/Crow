using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.Diagnostics;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	[Serializable]
	public class ProgressBar : NumericControl
    {
		#region CTOR
		public ProgressBar() : base(){}
		#endregion

		protected override void loadTemplate (GraphicObject template)
		{
			
		}

		#region GraphicObject overrides
		[XmlAttributeAttribute()][DefaultValue("vgradient|0:BlueCrayola|0,5:SkyBlue|1:BlueCrayola")]
		public override Fill Foreground {
			get { return base.Foreground; }
			set { base.Foreground = value; }
		}

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Rectangle rBack = ClientRectangle;

			rBack.Width = (int)((double)rBack.Width / Maximum * Value);

			Foreground.SetAsSource (gr, rBack);

			CairoHelpers.CairoRectangle(gr,rBack,CornerRadius);
			gr.Fill();
		}
		#endregion
    }
}

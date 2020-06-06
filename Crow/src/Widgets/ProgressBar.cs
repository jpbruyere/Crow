// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Cairo;

namespace Crow
{
	//TODO:to be  removed, numeric control with template having Gauge child is enough
	public class ProgressBar : NumericControl
    {
		#region CTOR
		protected ProgressBar () {}
		public ProgressBar(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		protected override void loadTemplate (Widget template)
		{			
		}

		#region GraphicObject overrides
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			if (Maximum == 0)
				return;

			Rectangle rBack = ClientRectangle;
			rBack.Width = (int)((double)rBack.Width / Maximum * Value);
			Foreground.SetAsSource (gr, rBack);

			CairoHelpers.CairoRectangle(gr,rBack,CornerRadius);
			gr.Fill();
		}
		#endregion
    }
}

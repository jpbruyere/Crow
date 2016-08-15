using System;
using System.Xml.Serialization;
using System.ComponentModel;
using Cairo;

namespace Crow
{
	public class AnalogMeter : NumericControl
	{
		#region CTOR
		public AnalogMeter() : base()
		{}
		public AnalogMeter(double minimum, double maximum, double step)
			: base(minimum,maximum,step)
		{
		}
		#endregion

		#region GraphicObject Overrides
		protected override void onDraw (Context gr)
		{			
			base.onDraw (gr);

			Rectangle r = ClientRectangle;
			Point m = r.Center;

			gr.Save ();


			double aUnit = Math.PI*2.0 / (Maximum - Minimum);
			gr.Translate (m.X, r.Height *1.1);
			gr.Rotate (Value/4.0 * aUnit - Math.PI/4.0);
			gr.Translate (-m.X, -m.Y);

			gr.LineWidth = 2;
			Foreground.SetAsSource (gr);
			gr.MoveTo (m.X,0.0);
			gr.LineTo (m.X, -m.Y*0.5);
			gr.Stroke ();

			gr.Restore ();
		}
		#endregion
	}
}


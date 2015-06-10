using System;
using System.Xml.Serialization;
using System.ComponentModel;
using Cairo;

namespace go
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

		#region implemented abstract members of TemplatedControl

		protected override void loadTemplate (GraphicObject template = null)
		{
			throw new NotImplementedException ();
		}

		#endregion

		#region GraphicObject Overrides
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Rectangle r = ClientRectangle;
			Point m = r.Center;

			double aUnit = Math.PI*2.0 / (Maximum - Minimum);
			gr.Translate (m.X, m.Y);
			gr.Rotate (Value * aUnit);
			gr.Translate (-m.X, -m.Y);

			gr.LineWidth = 2;
			gr.Color = Foreground;
			gr.MoveTo (m);
			gr.LineTo (m.X, 0);
			gr.Stroke ();
		}
		#endregion
	}
}


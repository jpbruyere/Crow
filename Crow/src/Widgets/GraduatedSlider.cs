// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Cairo;

namespace Crow
{
	[DesignIgnore]
	public class GraduatedSlider : Slider
    {     
		#region CTOR
		protected GraduatedSlider() {}
		public GraduatedSlider(Interface iface, string style = null) : base (iface, style) { }
//		public GraduatedSlider(double minimum, double maximum, double step)
//            : base()
//        {
//			Minimum = minimum;
//			Maximum = maximum;
//			SmallIncrement = step;
//			LargeIncrement = step * 5;
//        }
		#endregion

		/*protected override void DrawGraduations(Context gr, PointD pStart, PointD pEnd)
		{
			Rectangle r = ClientRectangle;
			Foreground.SetAsSource (gr);

			gr.LineWidth = 2;
			gr.MoveTo(pStart);
			gr.LineTo(pEnd);

			gr.Stroke();
			gr.LineWidth = 1;

			double sst = unity * SmallIncrement;
			double bst = unity * LargeIncrement;


			PointD vBar = new PointD(0, sst);
			for (double x = Minimum; x <= Maximum - Minimum; x += SmallIncrement)
			{
				double lineLength = r.Height / 3;
				if (x % LargeIncrement != 0)
					lineLength /= 3;
				PointD p = new PointD(pStart.X + x * unity, pStart.Y);
				gr.MoveTo(p);
				gr.LineTo(new PointD(p.X, p.Y + lineLength));
			}
			gr.Stroke();
		}*/
    }
}

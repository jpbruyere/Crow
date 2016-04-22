using System;

namespace Crow.GLBackend
{
	public class FontExtents
	{
		double ascent;
		double descent;
		double height;
		double maxXAdvance;
		double maxYAdvance;

		public double Ascent {
			get { return ascent; }
			set { ascent = value; }
		}
		public double Descent {
			get { return descent; }
			set { descent = value; }
		}
		public double Height {
			get { return height; }
			set { height = value; }
		}
		public double MaxXAdvance {
			get { return maxXAdvance; }
			set { maxXAdvance = value; }
		}
		public double MaxYAdvance {
			get { return maxYAdvance; }
			set { maxYAdvance = value; }
		}

		public FontExtents ()
		{
		}
	}
}


//
// GraduatedSlider.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.Xml.Serialization;

namespace Crow
{
	public class GraduatedSlider : Slider
    {     
		#region CTOR
		public GraduatedSlider () : base(){}
		public GraduatedSlider(Interface iface) : base(iface)
		{}
//		public GraduatedSlider(double minimum, double maximum, double step)
//            : base()
//        {
//			Minimum = minimum;
//			Maximum = maximum;
//			SmallIncrement = step;
//			LargeIncrement = step * 5;
//        }
		#endregion

		protected override void DrawGraduations(Context gr, PointD pStart, PointD pEnd)
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
		}
    }
}

//
// AnalogMeter.cs
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


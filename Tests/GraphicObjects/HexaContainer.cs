//
// SimpleGauge.cs
//
// Author:
//       jp <>
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
using Crow;
using vkvg;

namespace Tutorials2
{	
	/// <summary>
	/// Hexa container.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class HexaContainer : Container
	{
		const double pi3 = Math.PI / 3.0;

		public HexaContainer () : base() {}
		public HexaContainer (Interface iface): base (iface){}

		protected override void onDraw (Context gr)
		{
			gr.Save ();

			if (ClipToClientRect) {
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			Rectangle r = ClientRectangle;
			double radius = 0,
			cx = r.Width / 2.0 + r.X,
			cy = r.Height / 2.0 + r.Y;

			if (r.Width > r.Height)
				radius = r.Height / 2.0;
			else
				radius = r.Width / 2.0;

			double dx = Math.Sin (pi3) * radius;
			double dy = Math.Cos (pi3) * radius;

			gr.MoveTo (cx - radius, cy);
			gr.LineTo (cx - dy, cy - dx);
			gr.LineTo (cx + dy, cy - dx);
			gr.LineTo (cx + radius, cy);
			gr.LineTo (cx + dy, cy + dx);
			gr.LineTo (cx - dy, cy + dx);
			gr.ClosePath ();

			gr.LineWidth = 1;
			Background.SetAsSource (gr);
			gr.FillPreserve ();
			Foreground.SetAsSource (gr);
			gr.Stroke ();

			if (child != null) {
				if (child.Visible)
					child.Paint (ref gr);
			}
			gr.Restore ();
		}
	}
}


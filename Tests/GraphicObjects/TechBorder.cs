//
// TechBorder.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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

namespace Tests
{
	public class TechBorder : Container
	{
		public TechBorder () : base() {}
		public TechBorder (Interface iface): base (iface){}

		protected override int measureRawSize (LayoutingType lt)
		{
			return base.measureRawSize (lt) + 6;
		}
		protected override void onDraw (Cairo.Context gr)
		{
			gr.Save ();

			drawTechBorder1 (gr);

			if (child != null) {
				if (child.Visible)
					child.Paint (ref gr);
			}

			gr.Restore ();
		}

		void drawTechBorder1 (Cairo.Context gr){
			Rectangle r = ClientRectangle;

			double l1 = Math.Round(0.2 * Math.Min (r.Width, r.Height)) + 0.5;

			Foreground.SetAsSource (gr);
			gr.LineWidth = 6.0;
			gr.MoveTo (r.Left + 1.5, r.Top + l1);
			gr.LineTo (r.Left + 1.5, r.Top + 1.5);
			gr.LineTo (r.Left + l1, r.Top + 1.5);
			gr.MoveTo (r.Left + r.Width * 0.65, r.Bottom - 1.5);
			gr.LineTo (r.Left + r.Width * 0.85, r.Bottom - 1.5);
			gr.Stroke ();

			gr.MoveTo (r.Left + 2.5, r.Top + 2.5);
			gr.LineTo (r.Left + 2.5, r.Bottom - l1);
			gr.LineTo (r.Left + l1, r.Bottom - 2.5);
			gr.LineTo (r.Right - 2.5, r.Bottom - 2.5);
			gr.LineTo (r.Right - 2.5, r.Top + l1);
			gr.LineTo (r.Right - l1, r.Top + 2.5);
			gr.ClosePath ();

			if (ClipToClientRect) //clip to client zone				
				gr.ClipPreserve ();			

			Background.SetAsSource (gr);
			gr.FillPreserve ();

			gr.LineWidth = 1.0;
			Foreground.SetAsSource (gr);
			gr.Stroke ();			
		}
	}
}


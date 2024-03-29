//
// Mono.Cairo.Gradient.cs
//
// Author: Jordi Mas (jordi@ximian.com)
//         Hisham Mardam Bey (hisham.mardambey@gmail.com)
// (C) Ximian Inc, 2004.
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Drawing2D;

namespace Crow.CairoBackend {

	public class Gradient : Pattern, IGradient
	{
		protected Gradient (IntPtr handle, bool owned) : base (handle, owned)
		{
		}

		public int ColorStopCount {
			get {
				int cnt;
				NativeMethods.cairo_pattern_get_color_stop_count (handle, out cnt);
				return cnt;
			}
		}
		#region IGradient implementation
		public void AddColorStop (double offset, Color c)
			=> NativeMethods.cairo_pattern_add_color_stop_rgba (handle, offset, c.R / 255.0, c.G / 255.0, c.B / 255.0, c.A / 255.0);
		public void AddColorStop(float offset, float r, float g, float b, float a = 1)
			=> NativeMethods.cairo_pattern_add_color_stop_rgba (handle, offset, r, g, b, a);
		#endregion

		public Status AddColorStopRgb (double offset, Color c)
		{
			NativeMethods.cairo_pattern_add_color_stop_rgb (handle, offset, c.R / 255.0, c.G / 255.0 / 255.0, c.B / 255.0);
			return Status;
		}
	}
}


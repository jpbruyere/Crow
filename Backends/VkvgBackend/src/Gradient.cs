// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Drawing2D;

namespace Crow.VkvgBackend
{
	public class Gradient : Pattern, IGradient
	{
		protected Gradient(IntPtr handle) : base (handle) {	}
		public void AddColorStop (double offset, Color c)
			=> NativeMethods.vkvg_pattern_add_color_stop(handle, (float)offset, c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
		public void AddColorStop(float offset, float r, float g, float b, float a = 1f)
			=> NativeMethods.vkvg_pattern_add_color_stop(handle, offset, r, g, b, a);
	}
	public class LinearGradient : Gradient {
		public LinearGradient (float x0, float y0, float x1, float y1)
		: base (NativeMethods.vkvg_pattern_create_linear(x0, y0, x1, y1)) {

		}
	}
	public class RadialGradient : Gradient {
		public RadialGradient (	float cx0, float cy0, float radius0,
								float cx1, float cy1, float radius1)
		: base (NativeMethods.vkvg_pattern_create_radial(cx0, cy0, radius0, cx1, cy1, radius1)) {

		}
	}
}
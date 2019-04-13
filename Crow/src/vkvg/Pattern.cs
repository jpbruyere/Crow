//
// Pattern.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2019 jp
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
namespace vkvg {
	public class Pattern : IDisposable {

		IntPtr handle = IntPtr.Zero;

		#region CTORS & DTOR
		protected Pattern (IntPtr handle) {
			this.handle = handle;
		}
		public Pattern () {
			handle = NativeMethods.vkvg_pattern_create ();
		}
		public Pattern (float r, float g, float b) {
			handle = NativeMethods.vkvg_pattern_create_rgb (r, g, b);
		}
		public Pattern (float r, float g, float b, float a) {
			handle = NativeMethods.vkvg_pattern_create_rgba (r, g, b, a);
		}
		public Pattern (Surface surf) {
			handle = NativeMethods.vkvg_pattern_create_for_surface (surf.Handle);
		}

		~Pattern () {
			Dispose (false);
		}
		#endregion

		public static Pattern CreateLinearGradient (float x0, float y0, float x1, float y1) {
			return new Pattern (NativeMethods.vkvg_pattern_create_linear (x0, y0, x1, y1));
		}
		public static Pattern CreateRadialGradient (float cx0, float cy0, float radius0,
													 float cx1, float cy1, float radius1) {
			return new Pattern (NativeMethods.vkvg_pattern_create_radial (cx0, cy0, radius0, cx1, cy1, radius1));
		}

		public void AddReference () {
			NativeMethods.vkvg_pattern_reference (handle);
		}
		public uint References () => NativeMethods.vkvg_pattern_get_reference_count (handle);

		public IntPtr Handle { get { return handle; } }

		public Extend Extend {
			set { NativeMethods.vkvg_pattern_set_extend (handle, value); }
			get { return NativeMethods.vkvg_pattern_get_extend (handle); }
		}
		public Filter Filter {
			set { NativeMethods.vkvg_pattern_set_filter (handle, value); }
			get { return NativeMethods.vkvg_pattern_get_filter (handle); }
		}

		public void AddColorStop (float offset, float r, float g, float b, float a = 1f) {
			NativeMethods.vkvg_patter_add_color_stop (handle, offset, r, g, b, a);
		}

		#region IDisposable implementation
		public void Dispose () {
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing) {
			if (!disposing || handle == IntPtr.Zero)
				return;

			NativeMethods.vkvg_pattern_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}
//
// Mono.Cairo.GLSurface.cs
//
// Authors:
//			JP Bruyère (jp_bruyere@hotmail.com)
//
// This is an OO wrapper API for the Cairo API
//
// Copyright (C) 2016 JP Bruyère
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

	public class DRMSurface : Surface
	{

		public DRMSurface (IntPtr ptr, bool own) : base (ptr, own)
		{}

		public DRMSurface (DRMDevice device, Format format, int width, int height)
			: base (NativeMethods.cairo_drm_surface_create (device.Handle, format, width, height), true)
		{}

		public DRMSurface (DRMDevice device, uint name, Format format, int width, int height, int stride)
			: base (NativeMethods.cairo_drm_surface_create_for_name (device.Handle, name, format, width, height, stride), true)
		{}

		public DRMSurface (DRMDevice device, IntPtr imageSurface)
			: base (NativeMethods.cairo_drm_surface_create_from_cacheable_image (device.Handle, imageSurface), true)
		{}
	}
}

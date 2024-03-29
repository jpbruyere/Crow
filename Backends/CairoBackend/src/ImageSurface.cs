﻿//
// Mono.Cairo.ImageSurface.cs
//
// Authors:
//    Duncan Mak
//    Miguel de Icaza.
//
// (C) Ximian Inc, 2003.
// (C) Novell, Inc. 2003.
//
// This is an OO wrapper API for the Cairo API
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
using System.Runtime.InteropServices;
using Drawing2D;

namespace Crow.CairoBackend {

	public class ImageSurface : Surface
	{
		internal ImageSurface (IntPtr handle, bool owns) : base (handle, owns)
		{
		}

		public ImageSurface (Format format, int width, int height)
			: base (NativeMethods.cairo_image_surface_create (format, width, height), true)
		{
		}

		[Obsolete ("Use ImageSurface (byte[] data, Cairo.Format format, int width, int height, int stride)")]
		public ImageSurface (ref byte[] data, Format format, int width, int height, int stride)
			: this (data, format, width, height, stride)
		{
		}

		public ImageSurface (byte[] data, Format format, int width, int height, int stride)
			: base (NativeMethods.cairo_image_surface_create_for_data (data, format, width, height, stride), true)
		{
		}

		public ImageSurface (IntPtr data, Format format, int width, int height, int stride)
			: base (NativeMethods.cairo_image_surface_create_for_data (data, format, width, height, stride), true)
		{
		}

		public ImageSurface (string filename)
			: base (NativeMethods.cairo_image_surface_create_from_png (filename), true)
		{
		}

		public override int Width => NativeMethods.cairo_image_surface_get_width (handle);
		public override int Height => NativeMethods.cairo_image_surface_get_height (handle);

		public byte[] Data {
			get {
				IntPtr ptr = NativeMethods.cairo_image_surface_get_data (handle);
				int length = Height * Stride;
				byte[] data = new byte[length];
				Marshal.Copy (ptr, data, 0, length);
				return data;
			}
		}

		public IntPtr DataPtr {
			get {
				return NativeMethods.cairo_image_surface_get_data (handle);
			}
		}

		public Format Format {
			get { return NativeMethods.cairo_image_surface_get_format (handle); }
		}

		public int Stride {
			get { return NativeMethods.cairo_image_surface_get_stride (handle); }
		}
		public override void Resize(int width, int height)
		{
			
		}
	}
}

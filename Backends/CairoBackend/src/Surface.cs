﻿//
// Mono.Cairo.Surface.cs
//
// Authors:
//    Duncan Mak
//    Miguel de Icaza.
//    Alp Toker
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
using Drawing2D;

namespace Crow.CairoBackend {

	public class Surface : ISurface
	{
		internal IntPtr handle = IntPtr.Zero;
		public IntPtr Handle => handle;


		protected Surface()
		{
		}

		[Obsolete]
		protected Surface (IntPtr ptr) : this (ptr, true)
		{
		}

		protected Surface (IntPtr handle, bool owner)
		{
			this.handle = handle;
			if (!owner)
				NativeMethods.cairo_surface_reference (handle);
			if (CairoDebug.Enabled)
				CairoDebug.OnAllocated (handle);
		}

		public static Surface Lookup (IntPtr surface, bool owned)
		{
			SurfaceType st = NativeMethods.cairo_surface_get_type (surface);
			switch (st) {
			case SurfaceType.Image:
				return new ImageSurface (surface, owned);
			case SurfaceType.Xlib:
				return new XlibSurface (surface, owned);
			case SurfaceType.Xcb:
				return new XcbSurface (surface, owned);
			case SurfaceType.Glitz:
				return new GlitzSurface (surface, owned);
			case SurfaceType.Win32:
				return new Win32Surface (surface, owned);
			case SurfaceType.Pdf:
				return new PdfSurface (surface, owned);
			case SurfaceType.PS:
				return new PSSurface (surface, owned);
			case SurfaceType.DirectFB:
				return new DirectFBSurface (surface, owned);
			case SurfaceType.Svg:
				return new SvgSurface (surface, owned);
			case SurfaceType.GL:
				return new GLSurface (surface, owned);
			default:
				return new Surface (surface, owned);
			}
		}

		[Obsolete ("Use an ImageSurface constructor instead.")]
		public static Surface CreateForImage (
			ref byte[] data, Format format, int width, int height, int stride)
		{
			IntPtr p = NativeMethods.cairo_image_surface_create_for_data (
				data, format, width, height, stride);

			return new Surface (p, true);
		}

		[Obsolete ("Use an ImageSurface constructor instead.")]
		public static Surface CreateForImage (
			Format format, int width, int height)
		{
			IntPtr p = NativeMethods.cairo_image_surface_create (
				format, width, height);

			return new Surface (p, true);
		}


		public ISurface CreateSimilar (int width, int height) {
			IntPtr p = NativeMethods.cairo_surface_create_similar (
				this.handle, Content.ColorAlpha, width, height);
			return Surface.Lookup(p, true);
		}

		~Surface ()
		{
			Dispose (false);
		}


		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing || CairoDebug.Enabled)
				CairoDebug.OnDisposed<Surface> (handle, disposing);

			if (!disposing|| handle == IntPtr.Zero)
				return;

			NativeMethods.cairo_surface_destroy (handle);
			handle = IntPtr.Zero;
		}
		public virtual void Resize (int width, int height) {
			throw new NotImplementedException();
		}

		public Status Finish ()
		{
			NativeMethods.cairo_surface_finish (handle);
			return Status;
		}

		public virtual void Flush ()
		{
			NativeMethods.cairo_surface_flush (handle);
		}

		public void MarkDirty ()
		{
			NativeMethods.cairo_surface_mark_dirty (handle);
		}

		public void MarkDirty (Rectangle rectangle)
		{
			NativeMethods.cairo_surface_mark_dirty_rectangle (handle, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
		}
		public virtual int Width => NativeMethods.cairo_image_surface_get_width (handle);
		public virtual int Height => NativeMethods.cairo_image_surface_get_height (handle);
		public PointD DeviceOffset {
			get {
				double x, y;
				NativeMethods.cairo_surface_get_device_offset (handle, out x, out y);
				return new PointD (x, y);
			}

			set {
				NativeMethods.cairo_surface_set_device_offset (handle, value.X, value.Y);
			}
		}

		[Obsolete ("Use Dispose()")]
		public void Destroy()
		{
			Dispose ();
		}

		public void SetFallbackResolution (double x, double y)
		{
			NativeMethods.cairo_surface_set_fallback_resolution (handle, x, y);
		}

		public void WriteToPng (string filename)
		{
			NativeMethods.cairo_surface_write_to_png (handle, filename);
		}

		[Obsolete ("Use Handle instead.")]
		public IntPtr Pointer {
			get {
				return handle;
			}
		}

		public Status Status {
			get { return NativeMethods.cairo_surface_status (handle); }
		}

		public Content Content {
			get { return NativeMethods.cairo_surface_get_content (handle); }
		}

		public SurfaceType SurfaceType {
			get { return NativeMethods.cairo_surface_get_type (handle); }
		}

		public uint ReferenceCount {
			get { return NativeMethods.cairo_surface_get_reference_count (handle); }
		}

		public void WriteTo(IntPtr bitmap)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}
	}
}

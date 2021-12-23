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
using OpenGL;
using static OpenGL.Gl;

namespace Crow.CairoBackend {

	public class GLTextureSurface : Surface
	{
		uint texId;
		internal GLTextureSurface (CairoDevice device, int width, int height)
			: base ()
		{
			texId = GenTexture ();
			BindTexture (TextureTarget.Texture2d, texId);
			TexImage2D (TextureTarget.Texture2d, 0, InternalFormat.Rgb, width, height,
				0, PixelFormat.Rgb, PixelType.UnsignedByte, 0);
			TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, NEAREST);
			TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, NEAREST);

			handle = NativeMethods.cairo_gl_surface_create_for_texture (device.Handle,
				(uint)Content.ColorAlpha, texId, width, height);
		}
		protected override void Dispose (bool disposing)
		{
			if (disposing && handle != IntPtr.Zero)
				OpenGL.Gl.DeleteTextures (texId);
			base.Dispose (disposing);
		}

	}
	public class GLSurface : Surface
	{
		public GLSurface (IntPtr ptr, bool own) : base (ptr, own)
		{}

		public GLSurface (CairoDevice device, Content content, uint tex, int width, int height)
			: base (NativeMethods.cairo_gl_surface_create_for_texture (device.Handle, (uint)content, tex, width, height), true)
		{}

		public GLSurface (EGLDevice device, IntPtr eglSurf, int width, int height)
			: base (NativeMethods.cairo_gl_surface_create_for_egl (device.Handle, eglSurf, width, height), true)
		{}

		public GLSurface (GLXDevice device, IntPtr window, int width, int height)
			: base (NativeMethods.cairo_gl_surface_create_for_window (device.Handle, window, width, height),true)
		{}

		public GLSurface (WGLDevice device, IntPtr hdc, int width, int height)
			: base (NativeMethods.cairo_gl_surface_create_for_dc (device.Handle, hdc, width, height), true)
		{}
		public override void Flush ()
		{
			//base.Flush ();
			SwapBuffers ();
		}
		public override int Width => NativeMethods.cairo_gl_surface_get_width (handle);
		public override int Height => NativeMethods.cairo_gl_surface_get_height (handle);
		public override void Resize(int width, int height)
			=> NativeMethods.cairo_gl_surface_set_size(handle, width, height);

		public void SwapBuffers(){
			NativeMethods.cairo_gl_surface_swapbuffers (this.handle);
		}
	}
}

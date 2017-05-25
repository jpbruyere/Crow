//
// Surface.cs
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
using System.Runtime.InteropServices;

namespace EGL
{
	using EGLNativeDisplayType = IntPtr;
	using EGLNativeWindowType = IntPtr;
	using EGLNativePixmapType = IntPtr;
	using EGLConfig = IntPtr;
	using EGLContext = IntPtr;
	using EGLDisplay = IntPtr;
	using EGLSurface = IntPtr;
	using EGLClientBuffer = IntPtr;

	public class Surface : IDisposable
	{
		#region pinvoke
		[DllImportAttribute("libEGL.dll")]
		internal static extern  EGLSurface eglCreateWindowSurface(EGLDisplay dpy, EGLConfig config, IntPtr win, IntPtr attrib_list);
		[DllImportAttribute("libEGL.dll")]
		internal static extern EGLSurface eglCreatePbufferSurface(EGLDisplay dpy, EGLConfig config, int[] attrib_list);
		[DllImportAttribute("libEGL.dll")]
		internal static extern EGLSurface eglCreatePixmapSurface(EGLDisplay dpy, EGLConfig config, EGLNativePixmapType pixmap, int[] attrib_list);
		[DllImportAttribute("libEGL.dll")][return: MarshalAsAttribute(UnmanagedType.I1)]
		internal static extern bool eglDestroySurface(EGLDisplay dpy, EGLSurface surface);
		[DllImportAttribute("libEGL.dll")][return: MarshalAsAttribute(UnmanagedType.I1)]
		internal static extern bool eglQuerySurface(EGLDisplay dpy, EGLSurface surface, int attribute, out int value);
		[DllImportAttribute("libEGL.dll")]
		internal static extern EGLSurface eglCreatePbufferFromClientBuffer(EGLDisplay dpy, int buftype, EGLClientBuffer buffer, EGLConfig config, int[] attrib_list);
		[DllImportAttribute("libEGL.dll")][return: MarshalAsAttribute(UnmanagedType.I1)]
		internal static extern bool eglSurfaceAttrib(EGLDisplay dpy, EGLSurface surface, int attribute, int value);
		[DllImportAttribute("libEGL.dll")][return: MarshalAsAttribute(UnmanagedType.I1)]
		internal static extern bool eglBindTexImage(EGLDisplay dpy, EGLSurface surface, int buffer);
		[DllImportAttribute("libEGL.dll")][return: MarshalAsAttribute(UnmanagedType.I1)]
		internal static extern bool eglReleaseTexImage(EGLDisplay dpy, EGLSurface surface, int buffer);
		[DllImportAttribute("libEGL.dll")][return: MarshalAsAttribute(UnmanagedType.I1)]
		internal static extern bool eglSwapBuffers(EGLDisplay dpy, EGLSurface surface);

		#endregion

		Context ctx;
		internal EGLSurface handle;

		Surface (Context _ctx, IntPtr config, int[] attrib_list){
			ctx = _ctx;
			handle = eglCreatePbufferSurface (ctx.dpy, config, attrib_list);
			if (handle == IntPtr.Zero)
				throw new NotSupportedException(String.Format("[EGL] Failed to create surface, error {0}.", EGL.Context.GetError()));			
		}
		public Surface (Context _ctx, Linux.GBM.Surface gbmSurf)
		{
			ctx = _ctx;
			handle = eglCreateWindowSurface(ctx.dpy, ctx.currentCfg, gbmSurf.handle, IntPtr.Zero);
			if (handle == IntPtr.Zero)
				throw new NotSupportedException(String.Format("[EGL] Failed to create surface, error {0}.", EGL.Context.GetError()));
		}
		public static Surface CreatePBuffer (Context _ctx, int _width, int _height){
			int[] config = new int[] 
			{				
				Egl.SURFACE_TYPE, Egl.PBUFFER_BIT,
//				Egl.RENDERABLE_TYPE, Egl.OPENGL_BIT,
//				Egl.RED_SIZE, 8, 
//				Egl.GREEN_SIZE, 8, 
//				Egl.BLUE_SIZE, 8,
				Egl.NONE
			};
			int[] attribs = new int[] 
			{				
				Egl.WIDTH, _width,
				Egl.HEIGHT, _height,
				Egl.NONE
			};
			IntPtr cfg = _ctx.GetConfig (config);
			Console.WriteLine ("pbuff cfg: {0}", cfg.ToString ());
			return new Surface (_ctx, cfg, attribs);
		}

		public void MakeCurrent (){
			if (!Context.MakeCurrent(ctx.dpy, handle, handle, ctx.ctx))
				throw new NotSupportedException(string.Format("eglMakeCurrent on surface Failed: {0}",Context.GetError()));			
		}
		public void SwapBuffers () {
			if (!eglSwapBuffers (ctx.dpy, handle))
				throw new NotSupportedException(string.Format("eglSwapBuffers Failed: {0}",Context.GetError()));			
		}

		#region IDisposable implementation
		~Surface(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (handle != IntPtr.Zero)
				eglDestroySurface (ctx.dpy, handle);
			handle = IntPtr.Zero;
		}
		#endregion

	}
}


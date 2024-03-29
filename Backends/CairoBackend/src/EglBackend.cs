﻿using System;
using System.IO;
using Drawing2D;
using Glfw;

namespace Crow.CairoBackend
{
	public class EglBackend : CairoBackendBase {
		EGLDevice device;
		GLSurface winSurf;
		/// <summary>
		/// Create a new generic backend bound to the application surface
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		/// <param name="nativeWindoPointer"></param>
		public EglBackend (int width, int height, IntPtr nativeWindoPointer)
		: base (width, height, nativeWindoPointer) {
			if (nativeWindoPointer == IntPtr.Zero) {
				Glfw3.Init ();
				Glfw3.WindowHint (WindowAttribute.ClientApi, Constants.OpenglEsApi);
				Glfw3.WindowHint (WindowAttribute.ContextVersionMajor, 3);
				Glfw3.WindowHint (WindowAttribute.ContextVersionMinor, 2);
				Glfw3.WindowHint (WindowAttribute.ContextCreationApi, Constants.EglContextApi);
				Glfw3.WindowHint (WindowAttribute.Resizable, 1);
				Glfw3.WindowHint (WindowAttribute.Decorated, 1);

				hWin = Glfw3.CreateWindow (width, height, "win name", MonitorHandle.Zero, IntPtr.Zero);
				if (hWin == IntPtr.Zero)
					throw new Exception ("[GLFW3] Unable to create Window");
			}

			Glfw3.MakeContextCurrent (hWin);
			Glfw3.SwapInterval (0);

			device = new EGLDevice (Glfw3.GetEGLDisplay (), Glfw3.GetEGLContext (hWin));
			//surf = new GLTextureSurface (device, width, height);
			surf = new ImageSurface (Format.ARGB32, width, height);
			winSurf = new GLSurface (device, Glfw3.GetEGLSurface (hWin), width, height);
		}
		/// <summary>
		/// Create a new offscreen backend, used in perfTests
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		public EglBackend (int width, int height)
		: base (width, height, IntPtr.Zero) {
			device = new EGLDevice (Glfw3.GetEGLDisplay (), IntPtr.Zero);
			surf = new GLTextureSurface (device, width, height);
		}
		public override ISurface CreateSurface(int width, int height)
			//=> new GLTextureSurface (device, width, height);
			=> new ImageSurface (Format.ARGB32, width, height);
		public override ISurface CreateSurface(byte[] data, int width, int height)
			=> new ImageSurface (data, Format.ARGB32, width, height, 4 * width);
		public override IContext PrepareUIFrame(IContext existingContext, IRegion clipping)
		{
			Glfw3.MakeContextCurrent (hWin);

			IContext ctx = base.PrepareUIFrame (existingContext, clipping);

			clear (ctx);

			return ctx;
		}
		public override void FlushUIFrame(IContext ctx)
		{

			base.FlushUIFrame (ctx);
			surf.Flush();
			using (Context gr = new Context (winSurf)) {
				gr.SetSource (surf);
				gr.Paint();
			}
			winSurf.SwapBuffers ();
		}
		public override void ResizeMainSurface(int width, int height)
		{
			base.ResizeMainSurface(width, height);
			winSurf?.Dispose();
			winSurf = new GLSurface (device, Glfw3.GetEGLSurface (hWin), width, height);
		}
	}
}


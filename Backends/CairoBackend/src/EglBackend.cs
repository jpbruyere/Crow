using System;
using System.IO;
using Drawing2D;
using Glfw;

namespace Crow.CairoBackend
{
	public class EglBackend : CairoBackendBase {
		IntPtr hWin;
		EGLDevice device;
		/// <summary>
		/// Create a new generic backend bound to the application surface
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		public EglBackend (IntPtr nativeWindoPointer, int width, int height) : base () {
			hWin = nativeWindoPointer;
			Glfw3.MakeContextCurrent (hWin);
			Glfw3.SwapInterval (0);

			device = new EGLDevice (Glfw3.GetEGLDisplay (), Glfw3.GetEGLContext (hWin));
			surf = new GLSurface (device, Glfw3.GetEGLSurface (hWin), width, height);
		}
		/// <summary>
		/// Create a new offscreen backend, used in perfTests
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		public EglBackend (int width, int height) : base () {
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

			(surf as GLSurface).SwapBuffers ();
		}
	}
}


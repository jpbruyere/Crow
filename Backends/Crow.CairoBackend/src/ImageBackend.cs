using System;
using System.IO;
using Drawing2D;
using Glfw;

namespace Crow.CairoBackend
{
	public class DefaultBackend : CairoBackendBase {
		/// <summary>
		/// Create a new generic backend bound to the application surface
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		/// <param name="nativeWindoPointer"></param>
		public DefaultBackend (int width, int height, IntPtr nativeWindoPointer)
		: base (width, height, nativeWindoPointer) {
			if (nativeWindoPointer == IntPtr.Zero) {
				Glfw3.Init ();
				Glfw3.WindowHint (WindowAttribute.ClientApi, 0);
				Glfw3.WindowHint (WindowAttribute.Resizable, 1);
				Glfw3.WindowHint (WindowAttribute.Decorated, 1);

				hWin = Glfw3.CreateWindow (width, height, "win name", MonitorHandle.Zero, IntPtr.Zero);
				if (hWin == IntPtr.Zero)
					throw new Exception ("[GLFW3] Unable to create Window");
			}
			switch (Environment.OSVersion.Platform) {
			case PlatformID.Unix:
				IntPtr disp = Glfw3.GetX11Display ();
				IntPtr nativeWin = Glfw3.GetX11Window (hWin);
				Int32 scr = Glfw3.GetX11DefaultScreen (disp);
				IntPtr visual = Glfw3.GetX11DefaultVisual (disp, scr);
				surf = new XlibSurface (disp, nativeWin, visual, width, height);
				break;
			case PlatformID.Win32NT:
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
				IntPtr hWin32 = Glfw3.GetWin32Window (hWin);
				IntPtr hdc = Glfw3.GetWin32DC (hWin32);
				surf = new Win32Surface (hdc);
				break;
			default:
				throw new PlatformNotSupportedException ("Unable to create cairo image backend.");
			}
		}
		/// <summary>
		/// Create a new offscreen backend, used in perfTests
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		public DefaultBackend (int width, int height)
		: base (width, height, IntPtr.Zero) {
			surf = new ImageSurface (Format.ARGB32, width, height);
		}
		public DefaultBackend (IntPtr surfaceBitmapData, int width, int height, int stride)
		 : base (width, height, IntPtr.Zero) {
			surf = new ImageSurface (surfaceBitmapData, Format.ARGB32, width, height, stride);
		}

		public override ISurface CreateSurface(int width, int height)
			=> new ImageSurface (Format.ARGB32, width, height);
		public override ISurface CreateSurface(byte[] data, int width, int height)
			=> new ImageSurface (data, Format.ARGB32, width, height, 4 * width);
		public override IContext PrepareUIFrame(IContext existingContext, IRegion clipping)
		{
			IContext ctx = base.PrepareUIFrame (existingContext, clipping);

			clear (ctx);
			ctx.PushGroup ();

			return ctx;
		}
		public override void FlushUIFrame(IContext ctx)
		{
			ctx.PopGroupToSource ();
			ctx.Paint ();

			surf.Flush ();

			base.FlushUIFrame (ctx);
		}
	}
}


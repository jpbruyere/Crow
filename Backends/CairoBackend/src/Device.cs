//
// Mono.Cairo.Device.cs
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
using System.IO;
using System.Runtime.InteropServices;
using Drawing2D;
using Glfw;

namespace Crow.CairoBackend
{
	public abstract class CairoDevice : Device {
		protected IntPtr handle = IntPtr.Zero;
		public IntPtr Handle => handle;
		protected CairoDevice (IntPtr handle, bool owner = true)
		{
			this.handle = handle;
			if (!owner)
				NativeMethods.cairo_device_reference (handle);
			if (CairoDebug.Enabled)
				CairoDebug.OnAllocated (handle);
		}
		public string Status {
			get {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(NativeMethods.cairo_status_to_string (NativeMethods.cairo_device_status (handle)));
			}
		}
		public Status Acquire()
		{
			return NativeMethods.cairo_device_acquire (handle);
		}
		public void Release()
		{
			NativeMethods.cairo_device_release (handle);
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			if (!disposing || CairoDebug.Enabled)
				CairoDebug.OnDisposed<Device> (handle, disposing);

			if (!disposing || handle == IntPtr.Zero)
				return;

			NativeMethods.cairo_device_destroy (handle);

			handle = IntPtr.Zero;
		}
	}
	public class Device : IDevice
	{
		/// <summary> Global font rendering settings for Cairo </summary>
		FontOptions FontRenderingOptions;
		/// <summary> Global font rendering settings for Cairo </summary>
		Antialias Antialias = Antialias.Subpixel;

		public Device()
		{
			FontRenderingOptions = new FontOptions ();
			FontRenderingOptions.Antialias = Antialias.Subpixel;
			FontRenderingOptions.HintMetrics = HintMetrics.On;
			FontRenderingOptions.HintStyle = HintStyle.Full;
			FontRenderingOptions.SubpixelOrder = SubpixelOrder.Default;
		}

		~Device ()
		{
			Dispose (false);
		}

		#region IDevice implementation
		public void GetDpy(out int hdpy, out int vdpy)
		{
			throw new NotImplementedException();
		}

		public void SetDpy(int hdpy, int vdpy)
		{
			throw new NotImplementedException();
		}
		public IRegion CreateRegion () => new Region ();
		public virtual ISurface CreateSurface(int width, int height)
			=> new ImageSurface (Format.ARGB32, width, height);
		public virtual ISurface CreateSurface(byte[] data, int width, int height)
			=> new ImageSurface (data, Format.ARGB32, width, height, 4 * width);

		public ISurface CreateSurface (IntPtr nativeWindoPointer, int width, int height) {
			switch (Environment.OSVersion.Platform) {
			case PlatformID.Unix:
				IntPtr disp = Glfw3.GetX11Display ();
				IntPtr nativeWin = Glfw3.GetX11Window (nativeWindoPointer);
				Int32 scr = Glfw3.GetX11DefaultScreen (disp);
				IntPtr visual = Glfw3.GetX11DefaultVisual (disp, scr);
				return new XlibSurface (disp, nativeWin, visual, width, height);
			case PlatformID.Win32NT:
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
				IntPtr hWin32 = Glfw3.GetWin32Window (nativeWindoPointer);
				IntPtr hdc = Glfw3.GetWin32DC (hWin32);
				return new Win32Surface (hdc);
			}
			throw new PlatformNotSupportedException ("Unable to create cairo surface.");
		}

		public virtual IContext CreateContext(ISurface surf)
		{
			Context gr = new Context (surf);
			gr.FontOptions = FontRenderingOptions;
			gr.Antialias = Antialias;
			return gr;
		}
		public byte[] LoadBitmap (Stream stream, out Size dimensions) {
			byte[] image;
#if STB_SHARP
			StbImageSharp.ImageResult stbi = StbImageSharp.ImageResult.FromStream (stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
			image = new byte[stbi.Data.Length];
			//rgba to argb for cairo.
			for (int i = 0; i < stbi.Data.Length; i += 4) {
				image[i] = stbi.Data[i + 2];
				image[i + 1] = stbi.Data[i + 1];
				image[i + 2] = stbi.Data[i];
				image[i + 3] = stbi.Data[i + 3];
			}
			dimensions = new Size (stbi.Width, stbi.Height);
#else
			using (StbImage stbi = new StbImage (stream)) {
				image = new byte [stbi.Size];
				for (int i = 0; i < stbi.Size; i+=4) {
					//rgba to argb for cairo. ???? looks like bgra to me.
					image [i] = Marshal.ReadByte (stbi.Handle, i + 2);
					image [i + 1] = Marshal.ReadByte (stbi.Handle, i + 1);
					image [i + 2] = Marshal.ReadByte (stbi.Handle, i);
					image [i + 3] = Marshal.ReadByte (stbi.Handle, i + 3);
				}
				dimensions = new Size (stbi.Width, stbi.Height);
			}
#endif
			return image;
		}
		public ISvgHandle LoadSvg(Stream stream)
		{
			using (BinaryReader sr = new BinaryReader (stream))
				return new SvgHandle (sr.ReadBytes ((int)stream.Length));
		}

		public ISvgHandle LoadSvg(string svgFragment) =>
			new SvgHandle (System.Text.Encoding.Unicode.GetBytes (svgFragment));

		public IGradient CreateGradient (GradientType gradientType, Rectangle bounds) {
			switch (gradientType) {
			case GradientType.Vertical:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
			case GradientType.Horizontal:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Top);
			case GradientType.Oblic:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
			case GradientType.Radial:
				throw new NotImplementedException ();
			}
			return null;
		}
		#endregion

		#region IDispose implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing)
				FontRenderingOptions.Dispose ();
		}
		#endregion
	}
}


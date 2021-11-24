// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using Drawing2D;

namespace Crow.VkvgBackend
{
	public class Device: IDevice
	{

		IntPtr handle = IntPtr.Zero;

		#region CTORS & DTOR
		public Device (IntPtr instance, IntPtr phy, IntPtr dev, uint qFamIdx, SampleCount samples = SampleCount.Sample_1, uint qIndex = 0)
		{
			handle = NativeMethods.vkvg_device_create_multisample (instance, phy, dev, qFamIdx, qIndex, samples, false);
		}
		~Device ()
		{
			Dispose (false);
		}
		#endregion

		public void AddReference () => NativeMethods.vkvg_device_reference (handle);
		public uint References () => NativeMethods.vkvg_device_get_reference_count (handle);

		public IntPtr Handle => handle;

		#region IDevice implementation
		public void GetDpy (out int hdpy, out int vdpy) => NativeMethods.vkvg_device_get_dpy (handle, out hdpy, out vdpy);
		public void SetDpy (int hdpy, int vdpy) => NativeMethods.vkvg_device_set_dpy (handle, hdpy, vdpy);

		public IRegion CreateRegion() => new Region ();

		public ISurface CreateSurface(int width, int height)
		{
			throw new NotImplementedException();
		}

		public ISurface CreateSurface(byte[] data, int width, int height)
		{
			throw new NotImplementedException();
		}
		public ISurface CreateSurface(IntPtr glfwWinHandle, int width, int height)
		{
			throw new NotImplementedException();
		}
		public IContext CreateContext(ISurface surf)
		{
			throw new NotImplementedException();
		}

		public IGradient CreateGradient(GradientType gradientType, Rectangle bounds)
		{
			throw new NotImplementedException();
		}
		public byte[] LoadBitmap (Stream stream, out Size dimensions) {
			byte[] image;
#if STB_SHARP
			StbImageSharp.ImageResult stbi = StbImageSharp.ImageResult.FromStream (stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
			image = new byte[stbi.Data.Length];

			Array.Copy (stbi.Data, image, stbi.Data.Length);
			dimensions = new Size (stbi.Width, stbi.Height);
#else
			using (StbImage stbi = new StbImage (stream)) {
				image = new byte [stbi.Size];
				Marshal.Copy (stbi.Handle, image, 0, stbi.Size);
				dimensions = new Size (stbi.Width, stbi.Height);
			}
#endif
			return image;
		}

		public ISvgHandle LoadSvg(Stream stream)
		{
			throw new NotImplementedException();
		}

		public ISvgHandle LoadSvg(string svgFragment)
		{
			throw new NotImplementedException();
		}
		#endregion


		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing || handle == IntPtr.Zero)
				return;

			NativeMethods.vkvg_device_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}


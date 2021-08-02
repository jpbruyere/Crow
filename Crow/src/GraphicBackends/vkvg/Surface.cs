// Copyright (c) 2018-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow.Drawing
{
	public class Surface: IDisposable
	{		
		IntPtr handle = IntPtr.Zero;
		Device vkvgDev;

		public Surface (Device device, int width, int height)
		{
			vkvgDev = device;
			if (width <= 0 || height <= 0)				
				handle = NativeMethods.vkvg_surface_create (device.Handle, 1, 1);
			else
				handle = NativeMethods.vkvg_surface_create (device.Handle, (uint)width, (uint)height);
		}
		public Surface (Device device, Span<byte> data, int width, int heigth)
		{
			vkvgDev = device;
			handle = NativeMethods.vkvg_surface_create_from_bitmap (device.Handle, ref data.GetPinnableReference(), (uint)width, (uint)heigth);
		}
		public Surface (Device device, string imgPath) {
			vkvgDev = device;
			handle = NativeMethods.vkvg_surface_create_from_image (device.Handle, imgPath);
		}


		Surface (IntPtr devHandle, int width, int heigth)
		{
			handle = NativeMethods.vkvg_surface_create (devHandle, (uint)width, (uint)heigth);
		}
		~Surface ()
		{
			Dispose (false);
		}

		public IntPtr Handle => handle;
		public IntPtr VkImage => NativeMethods.vkvg_surface_get_vk_image (handle);
		public int Width => NativeMethods.vkvg_surface_get_width (handle);
		public int Height => NativeMethods.vkvg_surface_get_height (handle);

		public void AddReference () => NativeMethods.vkvg_surface_reference (handle);
		public uint References () => NativeMethods.vkvg_surface_get_reference_count (handle);

//		public Surface CreateSimilar (uint width, uint height) {
//			return new Surface (handle, width, height);
//		}
//		public Surface CreateSimilar (int width, int height) {
//			return new Surface (handle, (uint)width, (uint)height);
//		}

		public void Flush () {
			//throw new NotImplementedException ();
		}

		public void WriteToPng (string path) {
			NativeMethods.vkvg_surface_write_to_png (handle, path);
		}
		public void Clear () {
			NativeMethods.vkvg_surface_clear (handle);
		}

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

			NativeMethods.vkvg_surface_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}


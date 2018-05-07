//
// Context.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2018 jp
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

namespace vkvg
{
	public class Surface: IDisposable
	{		
		IntPtr handle = IntPtr.Zero;
		Device vkvgDev;

		public Surface (Device device, int width, int heigth)
		{
			vkvgDev = device;
			handle = NativeMethods.vkvg_surface_create (device.Handle, (uint)width, (uint)heigth);
		}
		public Surface (Device device, ref byte[] data, int width, int heigth)
		{
			vkvgDev = device;
			handle = NativeMethods.vkvg_surface_create (device.Handle, (uint)width, (uint)heigth);
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

		public IntPtr Handle { get { return handle; }}
		public IntPtr VkImage { get { return NativeMethods.vkvg_surface_get_vk_image (handle); }}
		public int Width { get { return NativeMethods.vkvg_surface_get_width (handle); }}
		public int Height { get { return NativeMethods.vkvg_surface_get_height (handle); }}

//		public Surface CreateSimilar (uint width, uint height) {
//			return new Surface (handle, width, height);
//		}
//		public Surface CreateSimilar (int width, int height) {
//			return new Surface (handle, (uint)width, (uint)height);
//		}

		public void Flush () {}
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


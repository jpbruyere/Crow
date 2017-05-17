//
// Device.cs
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

namespace Linux.GBM
{
	unsafe public class Device : IDisposable
	{
		#region pinvoke
		[DllImport("gbm", EntryPoint = "gbm_create_device", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr CreateDevice(int fd);
		[DllImport("gbm", EntryPoint = "gbm_device_destroy", CallingConvention = CallingConvention.Cdecl)]
		static extern void DestroyDevice(IntPtr gbm);
		[DllImport("gbm", EntryPoint = "gbm_device_get_fd", CallingConvention = CallingConvention.Cdecl)]
		static extern int DeviceGetFD(IntPtr gbm);
		[DllImport("gbm", EntryPoint = "gbm_device_is_format_supported", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool IsFormatSupported(IntPtr gbm, SurfaceFormat format, SurfaceFlags usage);
		#endregion

		int fd_gpu;
		internal IntPtr handle;

		#region ctor
		public Device (int _fd_gpu)
		{
			fd_gpu = _fd_gpu;
			handle = CreateDevice(fd_gpu);

			if (handle == IntPtr.Zero)
				throw new NotSupportedException("[GBM] device creation failed.");
		}
		#endregion
			
		#region IDisposable implementation
		~Device(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (handle != IntPtr.Zero)
				DestroyDevice (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}


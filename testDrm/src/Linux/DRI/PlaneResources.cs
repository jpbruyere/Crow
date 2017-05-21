//
// PlaneResources.cs
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

namespace Linux.DRI
{
	[StructLayout(LayoutKind.Sequential)]
	unsafe internal struct drmPlaneRes {
		public uint count_planes;
		public uint *planes;
	}

	unsafe public class PlaneResources : IDisposable
	{
		#region pinvoke
		[DllImport("libdrm", CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern drmPlaneRes* drmModeGetPlaneResources(int fd);
		[DllImport("libdrm", CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void drmModeFreePlaneResources(drmPlaneRes* ptr);
		#endregion

		int gpu_fd;
		drmPlaneRes* handle;

		internal PlaneResources (int fd_gpu)
		{
			gpu_fd = fd_gpu;
			handle = drmModeGetPlaneResources (fd_gpu);

			if (handle == null)
				throw new NotSupportedException("[DRI] drmModeGetPlaneResources failed.");
		}

		#region IDisposable implementation
		~PlaneResources(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			unsafe {
				if (handle != null)
					drmModeFreePlaneResources (handle);
				handle = null;
			}
		}
		#endregion
	}
}


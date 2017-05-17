//
// Plane.cs
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
	unsafe public struct drmPlane {
		public uint count_formats;
		public uint *formats;
		public uint plane_id;

		public uint crtc_id;
		public uint fb_id;

		public uint crtc_x, crtc_y;
		public uint x, y;

		public uint possible_crtcs;
		public uint gamma_size;
	}

	unsafe public class Plane : IDisposable
	{
		#region pinvoke
		[DllImport("libdrm", EntryPoint = "drmModeGetPlane", CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern drmPlane* ModeGetPlane(int fd, uint id);
		[DllImport("libdrm", EntryPoint = "drmModeFreePlane", CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void ModeFreePlane(drmPlane* ptr);
		#endregion

		drmPlane* handle;

		internal Plane (drmPlane* _handle)
		{
			handle = _handle;
		}
			
		public uint Id { get { return handle->plane_id; }}

		#region IDisposable implementation
		~Plane(){
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
					ModeFreePlane (handle);
				handle = null;
			}
		}
		#endregion
	}
}


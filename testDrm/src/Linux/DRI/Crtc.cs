//
// Crtc.cs
//
// Author:
//		 Stefanos Apostolopoulos <stapostol@gmail.com>
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2006-2014 Stefanos Apostolopoulos
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
	internal struct drmCrtc
	{
		public uint crtc_id;
		public uint buffer_id;

		public uint x, y;
		public uint width, height;
		public int mode_valid;
		public ModeInfo mode;

		public int gamma_size;
	}

	unsafe public class Crtc : IDisposable
	{
		#region pinvoke
		[DllImport("libdrm", EntryPoint = "drmModeGetCrtc", CallingConvention = CallingConvention.Cdecl)]
		internal static extern drmCrtc* ModeGetCrtc(int fd, uint crtcId);
		[DllImport("libdrm", EntryPoint = "drmModeFreeCrtc", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ModeFreeCrtc(drmCrtc* ptr);
		#endregion

		int fd_gpu;
		internal drmCrtc* handle;

		#region ctor
		internal Crtc (int _fd_gpu, uint _id)
		{
			fd_gpu = _fd_gpu;
			handle = ModeGetCrtc (fd_gpu, _id);

			if (handle == null)
				throw new NotSupportedException("[DRI] drmModeGetCrtc failed.");
		}
		#endregion

		public uint Id { get { return handle->crtc_id; }}
		public ModeInfo CurrentMode { get { return handle->mode; }}
		public uint CurrentFbId { get { return handle->buffer_id; }}
		public bool ModeIsValid { get { return handle->mode_valid == 0 ? false : true; }}
		public uint X { get { return handle->x; }}
		public uint Y { get { return handle->x; }}
		public uint Height { get { return handle->height; }}
		public uint Width { get { return handle->width; }}
		public int GammaSize { get { return handle->gamma_size; }}

		#region IDisposable implementation
		~Crtc(){
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
					ModeFreeCrtc (handle);
				handle = null;
			}
		}
		#endregion

		public override string ToString ()
		{
			return string.Format ("[Crtc: Id={0}, CurrentMode={1}, CurrentFbId={2}, ModeIsValid={3}, X={4}, Y={5}, Height={6}, Width={7}, GammaSize={8}]", Id, CurrentMode, CurrentFbId, ModeIsValid, X, Y, Height, Width, GammaSize);
		}
	}
}


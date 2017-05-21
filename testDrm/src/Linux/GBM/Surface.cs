//
// Surface.cs
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

namespace Linux.GBM
{
	public enum SurfaceFormat : uint
	{
		BigEndian = 1u << 31,
		C8 = ((int)('C') | ((int)('8') << 8) | ((int)(' ') << 16) | ((int)(' ') << 24)),

		RGB332 = ((int)('R') | ((int)('G') << 8) | ((int)('B') << 16) | ((int)('8') << 24)),
		BGR233 = ((int)('B') | ((int)('G') << 8) | ((int)('R') << 16) | ((int)('8') << 24)),

		XRGB4444 = ((int)('X') | ((int)('R') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		XBGR4444 = ((int)('X') | ((int)('B') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		RGBX4444 = ((int)('R') | ((int)('X') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		BGRX4444 = ((int)('B') | ((int)('X') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),

		ARGB4444 = ((int)('A') | ((int)('R') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		ABGR4444 = ((int)('A') | ((int)('B') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		RGBA4444 = ((int)('R') | ((int)('A') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		BGRA4444 = ((int)('B') | ((int)('A') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),

		XRGB1555 = ((int)('X') | ((int)('R') << 8) | ((int)('1') << 16) | ((int)('5') << 24)),
		XBGR1555 = ((int)('X') | ((int)('B') << 8) | ((int)('1') << 16) | ((int)('5') << 24)),
		RGBX5551 = ((int)('R') | ((int)('X') << 8) | ((int)('1') << 16) | ((int)('5') << 24)),
		BGRX5551 = ((int)('B') | ((int)('X') << 8) | ((int)('1') << 16) | ((int)('5') << 24)),

		ARGB1555 = ((int)('A') | ((int)('R') << 8) | ((int)('1') << 16) | ((int)('5') << 24)),
		ABGR1555 = ((int)('A') | ((int)('B') << 8) | ((int)('1') << 16) | ((int)('5') << 24)),
		RGBA5551 = ((int)('R') | ((int)('A') << 8) | ((int)('1') << 16) | ((int)('5') << 24)),
		BGRA5551 = ((int)('B') | ((int)('A') << 8) | ((int)('1') << 16) | ((int)('5') << 24)),

		RGB565 = ((int)('R') | ((int)('G') << 8) | ((int)('1') << 16) | ((int)('6') << 24)),
		BGR565 = ((int)('B') | ((int)('G') << 8) | ((int)('1') << 16) | ((int)('6') << 24)),

		RGB888 = ((int)('R') | ((int)('G') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
		BGR888 = ((int)('B') | ((int)('G') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),

		XRGB8888 = ((int)('X') | ((int)('R') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
		XBGR8888 = ((int)('X') | ((int)('B') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
		RGBX8888 = ((int)('R') | ((int)('X') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
		BGRX8888 = ((int)('B') | ((int)('X') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),

		ARGB8888 = ((int)('A') | ((int)('R') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
		ABGR8888 = ((int)('A') | ((int)('B') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
		RGBA8888 = ((int)('R') | ((int)('A') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
		BGRA8888 = ((int)('B') | ((int)('A') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),

		XRGB2101010 = ((int)('X') | ((int)('R') << 8) | ((int)('3') << 16) | ((int)('0') << 24)),
		XBGR2101010 = ((int)('X') | ((int)('B') << 8) | ((int)('3') << 16) | ((int)('0') << 24)),
		RGBX1010102 = ((int)('R') | ((int)('X') << 8) | ((int)('3') << 16) | ((int)('0') << 24)),
		BGRX1010102 = ((int)('B') | ((int)('X') << 8) | ((int)('3') << 16) | ((int)('0') << 24)),

		ARGB2101010 = ((int)('A') | ((int)('R') << 8) | ((int)('3') << 16) | ((int)('0') << 24)),
		ABGR2101010 = ((int)('A') | ((int)('B') << 8) | ((int)('3') << 16) | ((int)('0') << 24)),
		RGBA1010102 = ((int)('R') | ((int)('A') << 8) | ((int)('3') << 16) | ((int)('0') << 24)),
		BGRA1010102 = ((int)('B') | ((int)('A') << 8) | ((int)('3') << 16) | ((int)('0') << 24)),

		YUYV = ((int)('Y') | ((int)('U') << 8) | ((int)('Y') << 16) | ((int)('V') << 24)),
		YVYU = ((int)('Y') | ((int)('V') << 8) | ((int)('Y') << 16) | ((int)('U') << 24)),
		UYVY = ((int)('U') | ((int)('Y') << 8) | ((int)('V') << 16) | ((int)('Y') << 24)),
		VYUY = ((int)('V') | ((int)('Y') << 8) | ((int)('U') << 16) | ((int)('Y') << 24)),

		AYUV = ((int)('A') | ((int)('Y') << 8) | ((int)('U') << 16) | ((int)('V') << 24)),

		NV12 = ((int)('N') | ((int)('V') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		NV21 = ((int)('N') | ((int)('V') << 8) | ((int)('2') << 16) | ((int)('1') << 24)),
		NV16 = ((int)('N') | ((int)('V') << 8) | ((int)('1') << 16) | ((int)('6') << 24)),
		NV61 = ((int)('N') | ((int)('V') << 8) | ((int)('6') << 16) | ((int)('1') << 24)),

		YUV410 = ((int)('Y') | ((int)('U') << 8) | ((int)('V') << 16) | ((int)('9') << 24)),
		YVU410 = ((int)('Y') | ((int)('V') << 8) | ((int)('U') << 16) | ((int)('9') << 24)),
		YUV411 = ((int)('Y') | ((int)('U') << 8) | ((int)('1') << 16) | ((int)('1') << 24)),
		YVU411 = ((int)('Y') | ((int)('V') << 8) | ((int)('1') << 16) | ((int)('1') << 24)),
		YUV420 = ((int)('Y') | ((int)('U') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		YVU420 = ((int)('Y') | ((int)('V') << 8) | ((int)('1') << 16) | ((int)('2') << 24)),
		YUV422 = ((int)('Y') | ((int)('U') << 8) | ((int)('1') << 16) | ((int)('6') << 24)),
		YVU422 = ((int)('Y') | ((int)('V') << 8) | ((int)('1') << 16) | ((int)('6') << 24)),
		YUV444 = ((int)('Y') | ((int)('U') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
		YVU444 = ((int)('Y') | ((int)('V') << 8) | ((int)('2') << 16) | ((int)('4') << 24)),
	}
	[Flags]public enum SurfaceFlags : uint
	{
		Scanout = (1 << 0),
		Cursor64x64 = (1 << 1),
		Rendering = (1 << 2),
		Write = (1 << 3),
		Linear = (1 << 4),
	}
	[Flags]public enum TransferFlags : uint {
		/// <summary> Buffer contents read back (or accessed directly) at transfer create time.</summary>
		Read  = 1 << 0,
		/// <summary> Buffer contents will be written back at unmap time (or modified as a result of being accessed directly).</summary>
		Write = 1 << 1,
		/// <summary>Read/modify/write</summary>
		ReadWrite = Read | Write,
	}

	unsafe public class Surface : IDisposable
	{
		#region pinvoke
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gbm_surface_create (IntPtr gbm, int width, int height, SurfaceFormat format, SurfaceFlags flags);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void gbm_surface_destroy (IntPtr surface);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		static extern gbm_bo* gbm_surface_lock_front_buffer (IntPtr surface);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		static extern void gbm_surface_release_buffer (IntPtr surface, gbm_bo* buffer);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		static extern int gbm_surface_has_free_buffers (IntPtr surface);
		#endregion

		internal IntPtr handle;

		#region ctor
		public Surface (Device gbmDev, int width, int height, SurfaceFlags flags, SurfaceFormat format = SurfaceFormat.ARGB8888)
		{
			handle =  gbm_surface_create(gbmDev.handle, width, height, format, flags);

			if (handle == IntPtr.Zero)
				throw new NotSupportedException("[GBM] Failed to create GBM surface");
		}
		#endregion

		public bool HasFreeBuffers { get { return gbm_surface_has_free_buffers (handle) > 0; }}

		public gbm_bo* Lock (){
			gbm_bo* bo = gbm_surface_lock_front_buffer (handle);
			if (bo == null)
				throw new Exception ("[GBM]: Failed to lock front buffer.");
			return bo;
		}
		public void Release (gbm_bo* bo) {
			gbm_surface_release_buffer (handle, bo);
		}

		#region IDisposable implementation
		~Surface(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (handle != IntPtr.Zero)
				gbm_surface_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}


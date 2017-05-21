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
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void DestroyUserDataCallback(ref GBM.gbm_bo bo, ref uint data);

	[StructLayout(LayoutKind.Sequential)]
	public struct gbm_bo {
		IntPtr device;
		public uint Width, Height;
		public SurfaceFormat Format;
		public SurfaceFlags Flags;

		public uint Handle32
		{
			get { return (uint)BufferObject.gbm_bo_get_handle(ref this); }
		}
		public uint Stride
		{
			get { return BufferObject.gbm_bo_get_stride(ref this); }
		}
		public void SetUserData(ref uint data, DestroyUserDataCallback destroyFB)
		{
			BufferObject.gbm_bo_set_user_data(ref this, ref data, destroyFB);
		}
	}
	unsafe public class BufferObject : IDisposable
	{
		#region pinvoke
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern gbm_bo* gbm_bo_create (IntPtr gbm, uint width, uint height, SurfaceFormat format, SurfaceFlags flags);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void gbm_bo_destroy (gbm_bo* bo);
		[DllImport("gbm", EntryPoint = "gbm_bo_destroy", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void destryBO (ref gbm_bo bo);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int gbm_bo_write(gbm_bo* bo, IntPtr buf, IntPtr count);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern Device gbm_bo_get_device(ref gbm_bo bo);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong gbm_bo_get_handle(ref gbm_bo bo);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int gbm_bo_get_height(ref gbm_bo bo);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int gbm_bo_get_width(ref gbm_bo bo);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint gbm_bo_get_stride (ref gbm_bo bo);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void gbm_bo_set_user_data(ref gbm_bo bo, ref uint data, DestroyUserDataCallback callback);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr gbm_bo_get_user_data (IntPtr bo);
		[DllImport("gbm",  CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr gbm_bo_map (ref gbm_bo bo, uint x, uint y, uint width, uint height, TransferFlags flags, ref uint stride, out IntPtr data);
		[DllImport("gbm", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void gbm_bo_unmap (ref gbm_bo bo, IntPtr data);
		#endregion

		internal gbm_bo* handle;

		#region ctor
		public BufferObject (gbm_bo* _handle){
			handle = _handle;
		}
		public BufferObject (Device dev, uint _width, uint _height, SurfaceFormat format, SurfaceFlags flags)
		{
			handle = gbm_bo_create (dev.handle, _width, _height, format, flags);
			if (handle == null)
				throw new NotSupportedException("[GBM] BO creation failed.");
		}
		#endregion

		public uint Stride { get { return handle->Stride; }}
		public byte[] Data {
			set {				
				fixed (byte* pdata = value) {
					gbm_bo_write (handle, (IntPtr)pdata, (IntPtr)value.Length);
				}
			}
		}
			
		#region IDisposable implementation
		~BufferObject(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (handle != null)
				gbm_bo_destroy (handle);
			handle = null;
		}
		#endregion
	}
}


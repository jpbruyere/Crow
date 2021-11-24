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

		public Device()
		{
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






		#endregion

		#region IDispose implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing){
				
			}
				
		}
		#endregion
	}
}


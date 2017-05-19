//
// Encoder.cs
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
	public struct drmEncoder
	{
		public uint encoder_id;
		public EncoderType encoder_type;
		public uint crtc_id;
		public uint possible_crtcs;
		public uint possible_clones;
	}

	unsafe public class Encoder : IDisposable
	{
		#region pinvoke
		[DllImport("libdrm", EntryPoint = "drmModeGetEncoder", CallingConvention = CallingConvention.Cdecl)]
		internal static extern drmEncoder* ModeGetEncoder(int fd, uint encoder_id);
		[DllImport("libdrm", EntryPoint = "drmModeFreeEncoder", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ModeFreeEncoder(drmEncoder* ptr);
		#endregion

		int fd_gpu;
		internal drmEncoder* handle;

		#region ctor
		unsafe internal Encoder (int _fd_gpu, uint _id)
		{
			fd_gpu = _fd_gpu;
			handle = ModeGetEncoder (fd_gpu, _id);

			if (handle == null)
				throw new NotSupportedException("[DRI] drmModeGetEncoder failed.");
		}
		#endregion
			
		public uint Id { get { return handle->encoder_id; }}
		public EncoderType Type { get { return handle->encoder_type; }}
		public uint PossibleCrtcs { get { return handle->possible_crtcs; }}
		public uint PossibleClones { get { return handle->possible_clones; }}

		public Crtc CurrentCrtc {
			get {
				return handle->crtc_id == 0 ? null : new Crtc (fd_gpu, handle->crtc_id);
			}
		}

		#region IDisposable implementation
		~Encoder(){
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
					ModeFreeEncoder (handle);
				handle = null;
			}
		}
		#endregion

		public override string ToString ()
		{
			return string.Format ("[Encoder: Id={0}, Type={1}, PossibleCrtcs={2}, PossibleClones={3}]", Id, Type, PossibleCrtcs, PossibleClones);
		}
	}
}


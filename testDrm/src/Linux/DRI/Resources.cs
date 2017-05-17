//
// Resources.cs
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
	unsafe internal struct drmResources
	{
		public int count_fbs;
		public uint* fbs;
		public int count_crtcs;
		public uint* crtcs;
		public int count_connectors;
		public uint* connectors;
		public int count_encoders;
		public uint* encoders;
		public uint min_width, max_width;
		public uint min_height, max_height;
	}

	unsafe public class Resources: IDisposable
	{
		#region pinvoke
		[DllImport("libdrm", EntryPoint = "drmModeGetResources", CallingConvention = CallingConvention.Cdecl)]
		internal static extern drmResources* ModeGetResources(int fd);
		[DllImport("libdrm", EntryPoint = "drmModeFreeResources", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ModeFreeResources(drmResources* ptr);
		#endregion

		int gpu_fd;
		drmResources* handle;
			
		#region ctor
		public Resources (int fd_gpu)
		{
			gpu_fd = fd_gpu;
			handle = ModeGetResources (fd_gpu);

			if (handle == null)
				throw new NotSupportedException("[DRI] drmModeGetResources failed.");
		}
		#endregion

		public uint min_width { get { return handle->min_width; }}
		public uint max_width { get { return handle->max_width; }}
		public uint min_height { get { return handle->min_height; }}
		public uint max_height { get { return handle->max_height; }}

		public Connector[] Connectors {
			get {
				Connector[] tmp = new Connector[handle->count_connectors];
				for (int i = 0; i < handle->count_connectors; i++)
					tmp [i] = new Connector (gpu_fd, *(handle->connectors + i));
				return tmp;
			}
		}
		public Encoder[] Encoders {
			get {
				Encoder[] tmp = new Encoder[handle->count_encoders];
				for (int i = 0; i < handle->count_encoders; i++)
					tmp [i] = new Encoder (gpu_fd, *(handle->encoders + i));
				return tmp;
			}
		}
		public Crtc[] Crtcs {
			get {
				Crtc[] tmp = new Crtc[handle->count_encoders];
				for (int i = 0; i < handle->count_crtcs; i++)
					tmp [i] = new Crtc (gpu_fd, *(handle->crtcs + i));
				return tmp;
			}
		}

		#region IDisposable implementation
		~Resources(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (handle != null)
				ModeFreeResources (handle);
			handle = null;
		}
		#endregion

	}
}


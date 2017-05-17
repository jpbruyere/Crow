//
// Connector.cs
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
	#region Enums
	public enum ConnectionStatus
	{
		Connected = 1,
		Disconnected = 2,
		Unknown = 3
	}
	public enum ConnectorType
	{
		Unknown	= 0,
		VGA=1,
		DVII=2,
		DVID=3,
		DVIA=4,
		Composite=5,
		SVIDEO=6,
		LVDS=7,
		Component=8,
		PinDIN9 = 9,
		DisplayPort=10,
		HDMIA=11,
		HDMIB=12,
		TV=13,
		eDP=14,
		VIRTUAL=15,
		DSI=16,
		DPI=17
	}
	public enum SubPixel
	{
		Unknown = 1,
		HorizontalRgb = 2,
		HorizontalBgr = 3,
		VerticalRgb = 4,
		VerticalBgr = 5,
		None = 6
	}
	#endregion

	[StructLayout(LayoutKind.Sequential)]
	unsafe internal struct drmConnector
	{
		public uint connector_id;
		public uint encoder_id;
		public ConnectorType connector_type;
		public uint connector_type_id;
		public ConnectionStatus connection;
		public uint mmWidth, mmHeight;
		public SubPixel subpixel;

		public int count_modes;
		public ModeInfo* modes;

		public int count_props;
		public uint *props;
		public ulong *prop_values;

		public int count_encoders;
		public uint *encoders;
	}
	[StructLayout(LayoutKind.Sequential)]
	unsafe public struct ModeInfo
	{
		public uint clock;
		public ushort hdisplay, hsync_start, hsync_end, htotal, hskew;
		public ushort vdisplay, vsync_start, vsync_end, vtotal, vscan;

		public int vrefresh; // refresh rate * 1000

		public uint flags;
		public uint type;
		public fixed sbyte name[32];

		public string Name {
			get {
				fixed( sbyte* bytes = name)
					return new string (bytes);
			}
		}

		public override string ToString ()
		{
			return string.Format ("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}",
				Name, clock, hdisplay, hsync_start, hsync_end, htotal, hskew,
				vdisplay, vsync_start, vsync_end, vtotal, vscan, vrefresh);
		}
	}

	unsafe public class Connector : IDisposable
	{
		#region pinvoke
		[DllImport("libdrm", EntryPoint = "drmModeGetConnector", CallingConvention = CallingConvention.Cdecl)]
		internal static extern drmConnector* ModeGetConnector(int fd, uint connector_id);
		[DllImport("libdrm", EntryPoint = "drmModeFreeConnector", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ModeFreeConnector(drmConnector* ptr);
		#endregion

		int fd_gpu;
		drmConnector* handle;

		#region ctor
		public Connector (int _fd_gpu, uint _id)
		{
			fd_gpu = _fd_gpu;
			handle = ModeGetConnector (fd_gpu, _id);

			if (handle == null)
				throw new NotSupportedException("[DRI] drmModeGetConnector failed.");
		}
		#endregion

		public uint Id { get { return handle->connector_id; }}
		public ConnectionStatus State { get { return handle->connection; }}
		public ConnectorType Type { get { return handle->connector_type; }}
		public SubPixel SubPixel { get { return handle->subpixel; }}

		public Encoder CurrentEncoder {
			get { 
				return handle->encoder_id == 0 ? null : new Encoder (fd_gpu, handle->encoder_id);
			}
		}

		public ModeInfo[] Modes {
			get { 
				ModeInfo[] tmp = new ModeInfo[handle->count_modes];
				for (int i = 0; i < handle->count_modes; i++) {
					ModeInfo m = *(handle->modes + i);
					tmp [i] = m;
				}
				return tmp;
			}
		}

		#region IDisposable implementation
		~Connector(){
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
					ModeFreeConnector (handle);
				handle = null;
			}
		}
		#endregion

		public override string ToString ()
		{
			return string.Format ("Connector: Id={0}, State={1}, Type={2}, SubPixel={3}", Id, State, Type, SubPixel);
		}
	}
}


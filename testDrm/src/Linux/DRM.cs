//
// DRM.cs
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

namespace Crow.Linux.DRI {
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void VBlankCallback(int fd, int sequence, int tv_sec, int tv_usec, IntPtr user_data);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void PageFlipCallback(int fd, int sequence, int tv_sec,	int tv_usec, ref int user_data);

	enum ModeConnection
	{
		Connected = 1,
		Disconnected = 2,
		Unknown = 3
	}
	enum ModeConnectorType
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
	enum ModeEncoderType
	{
		NONE=0,
		DAC=1,
		TMDS=2,
		LVDS=3,
		TVDAC=4,
		VIRTUAL=5,
		DSI=6,
		DPMST=7,
		DPI=8,
	}
	enum ModeSubPixel
	{
		Unknown = 1,
		HorizontalRgb = 2,
		HorizontalBgr = 3,
		VerticalRgb = 4,
		VerticalBgr = 5,
		None = 6
	}

	[Flags]
	enum PageFlipFlags
	{
		FlipEvent = 0x01,
		FlipAsync = 0x02,
		FlipFlags = FlipEvent | FlipAsync
	}

	[Flags]
	enum ModeFlags
	{
		/* Video mode flags */
		/* bit compatible with the xorg definitions. */
		PHSYNC = 0x01,
		NHSYNC = 0x02,
		PVSYNC = 0x04,
		NVSYNC = 0x08,
		INTERLACE = 0x10,
		DBLSCAN = 0x20,
		CSYNC = 0x40,
		PCSYNC = 0x80,
		NCSYNC = 0x10,
		HSKEW = 0x0200,
		BCAST = 0x0400,
		PIXMUX = 0x0800,
		DBLCLK = 0x1000,
		CLKDIV2 = 0x2000,
		//		FLAG_3D_MASK			(0x1f<<14)
		//		FLAG_3D_NONE = 0x0;
		//		FLAG_3D_FRAME_PACKING = 0x4000,
		//		FLAG_3D_FIELD_ALTERNATIVE = 0x8000,
		//		FLAG_3D_LINE_ALTERNATIVE	(3<<14)
		//		FLAG_3D_SIDE_BY_SIDE_FULL	(4<<14)
		//		FLAG_3D_L_DEPTH		(5<<14)
		//		FLAG_3D_L_DEPTH_GFX_GFX_DEPTH	(6<<14)
		//		FLAG_3D_TOP_AND_BOTTOM	(7<<14)
		//		FLAG_3D_SIDE_BY_SIDE_HALF	(8<<14)		
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct EventContext
	{
		public int version;
		public IntPtr vblank_handler;
		public IntPtr page_flip_handler;
		public static readonly int Version = 2;
	}

	[StructLayout(LayoutKind.Sequential)]
	unsafe struct ModeConnector
	{
		public int connector_id;
		public int encoder_id;
		public ModeConnectorType connector_type;
		public int connector_type_id;
		public ModeConnection connection;
		public int mmWidth, mmHeight;
		public ModeSubPixel subpixel;

		public int count_modes;
		public ModeInfo* modes;

		public int count_props;
		public int *props;
		public long *prop_values;

		public int count_encoders;
		public int *encoders;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct ModeCrtc
	{
		public int crtc_id;
		public int buffer_id;

		public int x, y;
		public int width, height;
		public int mode_valid;
		public ModeInfo mode;

		public int gamma_size;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct ModeEncoder
	{
		public int encoder_id;
		public ModeEncoderType encoder_type;
		public int crtc_id;
		public int possible_crtcs;
		public int possible_clones;
	}

	[StructLayout(LayoutKind.Sequential)]
	unsafe struct ModeInfo
	{
		public uint clock;
		public ushort hdisplay, hsync_start, hsync_end, htotal, hskew;
		public ushort vdisplay, vsync_start, vsync_end, vtotal, vscan;

		public int vrefresh; // refresh rate * 1000

		public uint flags;
		public uint type;
		public fixed sbyte name[32];
	}

	[StructLayout(LayoutKind.Sequential)]
	unsafe struct ModeRes
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



	public class GPUControler : IDisposable {
		int fd_gpu = -1;
		ModeRes resources = new ModeRes ();

		public GPUControler(string gpu_path = "/dev/dri/card0"){
			fd_gpu = Libc.open(gpu_path, OpenFlags.ReadWrite | OpenFlags.CloseOnExec);
			if (fd_gpu < 0)
				throw new NotSupportedException("[KMS] Failed to open gpu");
			
			unsafe {
				ModeRes* ptrRes = ModeGetResources (fd_gpu);
				resources = *ptrRes;
				ModeFreeResources (ptrRes);
			}

//			if (resources == null)
//				throw new NotSupportedException("[KMS] Drm.ModeGetResources failed.");
		}

		#region IDisposable implementation
		~GPUControler(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (fd_gpu > 0)
				Libc.close (fd_gpu);
			fd_gpu = -1;
		}
		#endregion

		#region ioctl overrides
		const string lib = "libdrm";
		[DllImport(lib, EntryPoint = "drmHandleEvent", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HandleEvent(int fd, ref EventContext evctx);

		[DllImport(lib, EntryPoint = "drmModeAddFB", CallingConvention = CallingConvention.Cdecl)]
		public static extern int ModeAddFB(int fd, int width, int height, byte depth,
			byte bpp, int pitch, int bo_handle,
			out int buf_id);

		[DllImport(lib, EntryPoint = "drmModeRmFB", CallingConvention = CallingConvention.Cdecl)]
		public static extern int ModeRmFB(int fd, int bufferId);


		[DllImport(lib, EntryPoint = "drmModeGetResources", CallingConvention = CallingConvention.Cdecl)]
		unsafe static extern ModeRes* ModeGetResources(int fd);
		[DllImport(lib, EntryPoint = "drmModeGetCrtc", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ModeGetCrtc(int fd, int crtcId);
		[DllImport(lib, EntryPoint = "drmModeGetConnector", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ModeGetConnector(int fd, int connector_id);
		[DllImport(lib, EntryPoint = "drmModeGetEncoder", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ModeGetEncoder(int fd, int encoder_id);

		[DllImport(lib, EntryPoint = "drmModeFreeResources", CallingConvention = CallingConvention.Cdecl)]
		unsafe static extern void ModeFreeResources(ModeRes* ptr);
		[DllImport(lib, EntryPoint = "drmModeFreeCrtc", CallingConvention = CallingConvention.Cdecl)]
		public static extern void ModeFreeCrtc(IntPtr ptr);
		[DllImport(lib, EntryPoint = "drmModeFreeConnector", CallingConvention = CallingConvention.Cdecl)]
		public static extern void ModeFreeConnector(IntPtr ptr);
		[DllImport(lib, EntryPoint = "drmModeFreeEncoder", CallingConvention = CallingConvention.Cdecl)]
		public static extern void ModeFreeEncoder(IntPtr ptr);

		[DllImport(lib, EntryPoint = "drmModePageFlip", CallingConvention = CallingConvention.Cdecl)]
		static extern int ModePageFlip(int fd, int crtc_id, int fb_id,
			PageFlipFlags flags, ref int user_data);

		[DllImport(lib, EntryPoint = "drmModeSetCrtc", CallingConvention = CallingConvention.Cdecl)]
		unsafe static extern int ModeSetCrtc(int fd, int crtcId, int bufferId,
			int x, int y, int* connectors, int count, ModeInfo* mode);

		[DllImport(lib, EntryPoint = "drmModeSetCursor2", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SetCursor(int fd, int crtcId, int bo_handle, int width, int height, int hot_x, int hot_y);

		[DllImport(lib, EntryPoint = "drmModeMoveCursor", CallingConvention = CallingConvention.Cdecl)]
		public static extern int MoveCursor(int fd, int crtcId, int x, int y);
		#endregion
	}
}
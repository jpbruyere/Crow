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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Linux.DRI {
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void VBlankCallback(int fd, int sequence, int tv_sec, int tv_usec, IntPtr user_data);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void PageFlipCallback(int fd, int sequence, int tv_sec,	int tv_usec, ref int user_data);

	public enum EncoderType : uint
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
	public enum PlaneType {
		Overlay = 0,
		Primary = 1,
		Cursor = 2
	}

	[Flags]public enum PageFlipFlags
	{
		FlipEvent = 0x01,
		FlipAsync = 0x02,
		FlipFlags = FlipEvent | FlipAsync
	}
	/// <summary>Video mode flags, bit compatible with the xorg definitions. </summary>
	[Flags]public enum VideoMode
	{
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
	unsafe public struct drmFrameBuffer {
		public uint fb_id;
		public uint width, height;
		public uint pitch;
		public uint bpp;
		public uint depth;
		/* driver specific handle */
		public uint handle;
	}
	[StructLayout(LayoutKind.Sequential)]
	unsafe internal struct drmPlaneRes {
		public uint count_planes;
		public uint *planes;
	}

	public class GPUControler : IDisposable {
		int fd_gpu = -1;
		GBM.Device gbmDev;
		GBM.Surface gbmSurf;
		EGL.Context eglctx;
		EGL.Surface eglSurf;

		Cairo.EGLDevice cairoDev;
		public Cairo.GLSurface CairoSurf;

		Resources resources = null;
		Connector connector = null;
		Crtc currentCrtc = null;
		ModeInfo currentMode, originalMode;
		uint originalFB;

		public GPUControler(string gpu_path = "/dev/dri/card0"){
			fd_gpu = Libc.open(gpu_path, OpenFlags.ReadWrite | OpenFlags.CloseOnExec);
			if (fd_gpu < 0)
				throw new NotSupportedException("[DRI] Failed to open gpu");
			
			resources = new Resources (fd_gpu);
			gbmDev = new GBM.Device (fd_gpu);
			eglctx = new EGL.Context (gbmDev);

			try {
				if (defaultConfiguration ())
					Console.WriteLine ("default config ok");				
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString());
			}
		}

		byte bpp = 32;
		byte depth = 24;

		public int Width { get { return (int)currentMode.hdisplay; }}
		public int Height { get { return (int)currentMode.vdisplay; }}

		ModeInfo getNewMode(){
			ModeInfo mode = currentCrtc.CurrentMode;
			mode.clock = 118250;
			mode.hdisplay = 1600;
			mode.hsync_start = 1696;
			mode.hsync_end = 1856;
			mode.htotal = 2112;
			mode.vdisplay = 900;
			mode.vsync_start = 903;
			mode.vsync_end = 908;
			mode.vtotal = 934;
			mode.flags |= (uint)VideoMode.NHSYNC;
			mode.flags |= (uint)VideoMode.PVSYNC;
			return mode;
		}
		unsafe void setNewMode(){
			
			//currentCrtc.handle->mode = currentMode;
//			ModeInfo* mode = (ModeInfo*)Marshal.AllocHGlobal (sizeof(ModeInfo));// pConnector->modes;
//			*mode = currentMode;

			uint fb;
			GBM.gbm_bo* bo = GBM.BufferObject.gbm_bo_create (gbmDev.handle, (uint)Width, (uint)Height, GBM.SurfaceFormat.ARGB8888, GBM.SurfaceFlags.Scanout);
			int ret = drmModeAddFB (fd_gpu, (uint)Width, (uint)Height, (byte)depth, (byte)bpp, bo->Stride, (uint)bo->Handle32, out fb);
			if (ret != 0)
				Console.WriteLine ("addFb failed: {0}", ret);
			bo->SetUserData ((IntPtr)fb, handleDestroyFB);

			uint connId = connector.Id;
			ret = drmModeSetCrtc (fd_gpu, currentCrtc.Id, fb, 0, 0, &connId, 1, ref currentMode);
			if (ret != 0)
				Console.WriteLine ("set new mode setCrtc failed: {0}", ret);
			//GBM.BufferObject.gbm_bo_destroy (bo);

			//Console.WriteLine ("new mode set to {0} x {1}", Width, Height);
		}
		bool defaultConfiguration (){
			//select the first connected connector
			foreach (Connector c in resources.Connectors) {
				if (c.State == ConnectionStatus.Connected) {
					connector = c;
					break;
				}				
			}
			if (connector == null)
				return false;
			
			currentCrtc = connector.CurrentEncoder.CurrentCrtc;
			originalMode = currentCrtc.CurrentMode;
			originalFB = currentCrtc.CurrentFbId;
			currentMode = getNewMode();


			//configure a rendering stack
			gbmSurf = new GBM.Surface (gbmDev, Width, Height,
				GBM.SurfaceFlags.Rendering | GBM.SurfaceFlags.Scanout);

			//setNewMode ();

			eglSurf = new EGL.Surface (eglctx, gbmSurf);
			eglSurf.MakeCurrent ();

			cairoDev = new Cairo.EGLDevice (eglctx.dpy, eglctx.ctx);

			CairoSurf = new Cairo.GLSurface (cairoDev, eglSurf.handle, Width, Height);
			//cairoSurf = new Cairo.EGLSurface (cairoDev, egl_surface, 1600, 900);

			cairoDev.SetThreadAware (false);

			if (cairoDev.Acquire () != Cairo.Status.Success)
				Console.WriteLine ("[Cairo]: Failed to acquire egl device.");

//			using (Cairo.Context ctx = new Cairo.Context (CairoSurf)) {
//				ctx.Rectangle (0, 0, Width, Height);
//				ctx.SetSourceRGB (0, 0, 1);
//				ctx.Fill ();
//			}
//			CairoSurf.Flush ();
//			CairoSurf.SwapBuffers ();
//			Update ();

			//Thread.Sleep (1);
			return true;
		}
		void handleDestroyFB(ref GBM.gbm_bo bo, IntPtr data)
		{
			int fb = data.ToInt32();

			if (fb != 0) {
				try {
					if (drmModeRmFB (fd_gpu, fb) != 0)
						Console.WriteLine ("DestroyFB failed");
					else
						Console.WriteLine ("DestroyFB ok fd={0}", fd_gpu);					
				} catch (Exception ex) {
					Console.WriteLine ("DestroyFB error:" + ex.ToString());
				}
			}
		}



		unsafe public void Update(){
			GBM.gbm_bo* bo;	
			uint fb;

			if (!gbmSurf.HasFreeBuffers)
				throw new NotSupportedException("[GBM] Out of free buffer");
				
			bo = gbmSurf.Lock ();

			int ret = drmModeAddFB (fd_gpu, currentMode.hdisplay, currentMode.vdisplay, (byte)depth, (byte)bpp, bo->Stride, (uint)bo->Handle32, out fb);
			if (ret != 0)
				Console.WriteLine ("addFb failed: {0}", ret);
			bo->SetUserData ((IntPtr)fb, handleDestroyFB);

			uint connId = connector.Id;
			ret = drmModeSetCrtc (fd_gpu, currentCrtc.Id, fb, 0, 0, &connId, 1, ref currentMode);
			if (ret != 0)
				Console.WriteLine ("setCrtc failed: {0}", ret);

			gbmSurf.Release (bo);
		}
		[StructLayout(LayoutKind.Sequential)]
		struct drmClip {
			public ushort x1;
			public ushort y1;
			public ushort x2;
			public ushort y2;
		}
		unsafe public void MarkFBDirty(){
			IntPtr pClip = Marshal.AllocHGlobal (sizeof(drmClip));
			drmClip dc = new drmClip () { x1 = 0, y1 = 0, x2 = 500, y2 = 500 };
			Marshal.StructureToPtr (dc, pClip,false);
			int ret = drmModeDirtyFB (fd_gpu, currentCrtc.CurrentFbId, IntPtr.Zero, 0);
			if (ret < 0)
				Console.WriteLine ("set FB dirty failed: {0}", ret);
		}
		#region cursor
		GBM.BufferObject boMouseCursor;

		internal void updateCursor (Crow.XCursor cursor) {
			uint width = 64, height = 64;
			if (cursor.Width > width || cursor.Height > height){
				Debug.Print("[DRM] Cursor size {0}x{1} unsupported. Maximum is 64x64.",
					cursor.Width, cursor.Height);
				return;
			}
			boMouseCursor = new GBM.BufferObject (gbmDev, width, height, GBM.SurfaceFormat.ARGB8888,
				GBM.SurfaceFlags.Cursor64x64 | GBM.SurfaceFlags.Write);
			
			// Copy cursor.Data into a new buffer of the correct size
			byte[] cursor_data = new byte[width * height * 4];
			for (uint y = 0; y < cursor.Height; y++)
			{
				uint dst_offset = y * width * 4;
				uint src_offset = y * cursor.Width * 4;
				uint src_length = cursor.Width * 4;
				Array.Copy(
					cursor.data, src_offset,
					cursor_data, dst_offset,
					src_length);
			}

			boMouseCursor.Data = cursor_data;
			uint crtcid = currentCrtc.Id;

			unsafe {
				drmModeSetCursor2 (fd_gpu, crtcid,
					(uint)boMouseCursor.handle->Handle32, width, height, (int)cursor.Xhot, (int)cursor.Yhot);
				drmModeMoveCursor (fd_gpu, crtcid, 0, 0);
			}
		}
		internal void moveCursor (uint x, uint y){
			drmModeMoveCursor (fd_gpu, currentCrtc.Id, x, y);
		}
		#endregion

//		unsafe public drmPlane GetPlane (uint id) {
//			drmPlane p = new drmPlane();
//			drmPlane* pPlane = ModeGetPlane (fd_gpu, id);
//			if (pPlane != null) {
//				p = *pPlane;
//				ModeFreePlane (pPlane);
//			}
//			return p;
//		}
//		public void SetPlane (drmPlane p, uint flags, uint crtc_w, uint crtc_h, uint src_w, uint src_h) {
//			ModeSetPlane (fd_gpu, p.plane_id, p.crtc_id, p.fb_id, flags,
//				(int)p.crtc_x, (int)p.crtc_y,
//				crtc_w, crtc_h,
//				p.x, p.y,
//				src_w, src_h);
//
//				
//		}

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
			if (cairoDev != null) {
				cairoDev.Release ();
				CairoSurf.Dispose ();
				cairoDev.Dispose ();
				cairoDev = null;
				CairoSurf = null;
			}

			uint connId = connector.Id;
			unsafe{
				int ret = drmModeSetCrtc (fd_gpu, currentCrtc.Id, originalFB, 0, 0, &connId, 1, ref originalMode);
				if (ret != 0)
					Console.WriteLine ("restore Crtc failed: {0}", ret);
			}

			if (boMouseCursor != null)
				boMouseCursor.Dispose ();
			boMouseCursor = null;
			if (eglctx != null)
				eglctx.Dispose ();
			eglctx = null;

			if (gbmSurf != null)
				gbmSurf.Dispose ();
			if (gbmDev != null)
				gbmDev.Dispose ();			
			if (currentCrtc != null)
				currentCrtc.Dispose ();			
			if (connector != null)
				connector.Dispose ();			
			if (resources != null)
				resources.Dispose ();
			resources = null;
			if (fd_gpu > 0)
				Libc.close (fd_gpu);
			fd_gpu = -1;
			Console.WriteLine ("disposing ok");
		}
		#endregion

		#region dllimports
		const string lib = "libdrm";
		[DllImport(lib, EntryPoint = "drmHandleEvent", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HandleEvent(int fd, ref EventContext evctx);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		public static extern int drmModeAddFB(int fd, uint width, uint height, byte depth,
			byte bpp, uint stride, uint bo_handle, out uint buf_id);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		public static extern int drmModeRmFB(int fd, int bufferId);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		public static extern int drmModeDirtyFB(int fd, uint bufferId, IntPtr clips, uint num_clips);

		[DllImport(lib, EntryPoint = "drmModeGetFB", CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern drmFrameBuffer* ModeGetFB(int fd, uint fb_id);
		[DllImport(lib, EntryPoint = "drmModeGetPlaneResources", CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern drmPlaneRes* ModeGetPlaneResources(int fd);

		[DllImport(lib, EntryPoint = "drmModeFreeFB", CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void ModeFreeFB(drmFrameBuffer* ptr);
		[DllImport(lib, EntryPoint = "drmModeFreePlaneResources", CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void ModeFreePlaneResources(drmPlaneRes* ptr);

		[DllImport(lib, EntryPoint = "drmModeSetPlane", CallingConvention = CallingConvention.Cdecl)]
		unsafe static extern int ModeSetPlane(int fd, uint plane_id, uint crtc_id,
			uint fb_id, uint flags,
			int crtc_x, int crtc_y,
			uint crtc_w, uint crtc_h,
			uint src_x, uint src_y,
			uint src_w, uint src_h);

		[DllImport(lib, EntryPoint = "drmModePageFlip", CallingConvention = CallingConvention.Cdecl)]
		static extern int ModePageFlip(int fd, int crtc_id, int fb_id,
			PageFlipFlags flags, ref int user_data);

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		unsafe static extern int drmModeSetCrtc(int fd, uint crtcId, uint bufferId,	uint x, uint y, uint* connectors, int count, ref ModeInfo mode);

		[DllImport(lib, EntryPoint = "drmModeSetCrtc", CallingConvention = CallingConvention.Cdecl)]
		unsafe static extern int ModeSetCrtc(int fd, uint crtcId, uint bufferId,	uint x, uint y, uint* connectors, int count, ModeInfo* mode);

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int drmModeSetCursor2(int fd, uint crtcId, uint bo_handle, uint width, uint height, int hot_x, int hot_y);

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int drmModeMoveCursor(int fd, uint crtcId, uint x, uint y);
		#endregion


//		unsafe static bool paint(gbm_bo * bo)
//		{
//			uint w = (uint)bo->Width;
//			uint h = (uint)bo->Height;
//			uint stride = (uint)bo->Stride;
//
//			Console.WriteLine ("trying to map bo: {0}x{1} stride:{2}", w, h, stride);
//			bool success = false;
//			try {
//				unsafe {
//					IntPtr map_data = IntPtr.Zero;
//					IntPtr addr = Gbm.Map (bo, 0, 0, w, h, TransferFlags.Write, ref stride, out map_data);
//					if (addr == IntPtr.Zero || map_data == IntPtr.Zero) {
//						Console.WriteLine ("failed to mmap gbm bo");
//						return false;
//					}
//					Console.WriteLine ("addr = {0}", addr.ToString());
//					byte* b = (byte*)addr;
//					for (int y = 0; y < h; y++) {
//						for (int x = 0; x < w; x++) {							
//							*(b + x + y * stride) = 0xff;
//						}
//					}
//					Gbm.Unmap (bo, map_data);
//					success = true;
//				}
//			} catch (Exception ex) {
//				Console.WriteLine (ex.ToString ());
//			}
//			return success;
//		}
	}
}
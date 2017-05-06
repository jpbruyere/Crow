//
// DrmKms.cs
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
using OpenTK.Platform.Linux;
using OpenTK;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Platform.Egl;
using System.Runtime.InteropServices;
using System.Threading;

namespace testDrm
{
	public class DrmDevice : IDisposable
	{
		volatile bool run = true;

		int fd = 0;
		int major, minor;
		IntPtr gbm_device, gbm_surface, egl_display, egl_config, egl_surface, egl_ctx;

		int r, g, b, a;

		BufferObject cursor_custom;
		BufferObject cursor_default;
		BufferObject cursor_empty;


		ModeInfo originalMode;

		public IntPtr Connector;
		public IntPtr Crtc;
		public IntPtr Encoder;
		unsafe ModeConnector* pConnector { get { return (ModeConnector*)Connector; } }
		unsafe ModeCrtc* pCrtc { get { return (ModeCrtc*)Crtc; } }
		unsafe ModeEncoder* pEncoder { get { return (ModeEncoder*)Encoder; } }

		Cairo.EGLDevice cairoDev;
		Cairo.GLSurface cairoSurf;

		public DrmDevice(string gpu_path = "/dev/dri/card0"){

			PageFlip = HandlePageFlip;
			PageFlipPtr = Marshal.GetFunctionPointerForDelegate(PageFlip);		

			gbm_device = IntPtr.Zero;
			egl_display = IntPtr.Zero;

			fd = Libc.open(gpu_path, OpenFlags.ReadWrite | OpenFlags.CloseOnExec);
			if (fd < 0)
				throw new NotSupportedException("[KMS] Failed to open gpu");			
			Console.WriteLine("[KMS] GPU '{0}' opened as fd:{1}", gpu_path, fd);

			initDrm ();

			initGbm ();

			initEgl ();

			initCairo ();

			initInput ();
		}

		#region init
		unsafe void initDrm(){			
			ModeRes* resources = (ModeRes*)Drm.ModeGetResources(fd);
			if (resources == null)
				throw new NotSupportedException("[KMS] Drm.ModeGetResources failed.");

			ModeConnector* connector = null;
			for (int i = 0; i < resources->count_connectors; i++) {
				connector = (ModeConnector*)Drm.ModeGetConnector (fd, *(resources->connectors + i));
				if (connector != null) {
					if (connector->connection == ModeConnection.Connected && connector->count_encoders > 0)
						break;
					Drm.ModeFreeConnector ((IntPtr)connector);
					connector = null;
				}
			}
			if (connector == null)
				throw new NotSupportedException("[KMS] No connected screen found");

			Connector = (IntPtr)connector;
			Encoder = Drm.ModeGetEncoder (fd, connector->encoder_id);
			Crtc = Drm.ModeGetCrtc(fd, pEncoder->crtc_id);

			originalMode = pCrtc->mode;
			Console.WriteLine ("[DRM]: current mode = {0} X {1} at {2} Hz", originalMode.hdisplay, originalMode.vdisplay, originalMode.vrefresh);
		}
		void initGbm (){
			gbm_device = Gbm.CreateDevice(fd);
			if (gbm_device == IntPtr.Zero)
				throw new NotSupportedException("[GBM] Failed to create GBM device");			

			gbm_surface =  Gbm.CreateSurface(gbm_device, originalMode.hdisplay, originalMode.vdisplay, SurfaceFormat.ARGB8888, SurfaceFlags.Rendering | SurfaceFlags.Scanout);
			if (gbm_surface == IntPtr.Zero)
				throw new NotSupportedException("[GBM] Failed to create GBM surface for rendering");						
		}

		unsafe void initEgl () {
			IntPtr[] configs = new IntPtr[1];
			int[] contextAttrib = new int[] {
				Egl.CONTEXT_CLIENT_VERSION, 2,
				Egl.NONE
			};
			int[] attribList = new int[] 
			{				
				Egl.SURFACE_TYPE, Egl.WINDOW_BIT,
				Egl.RENDERABLE_TYPE, Egl.OPENGL_BIT,
				Egl.RED_SIZE, 1, 
				Egl.GREEN_SIZE, 1, 
				Egl.BLUE_SIZE, 1,
				Egl.ALPHA_SIZE, 0,

				//Egl.DEPTH_SIZE, 24,
				//Egl.STENCIL_SIZE, 0,

				//Egl.SAMPLE_BUFFERS, 2,
				//Egl.SAMPLES, 0,
				Egl.NONE
			};
			int num_configs;

			egl_display = Egl.GetDisplay(gbm_device);
			if (egl_display == IntPtr.Zero)
				throw new NotSupportedException("[KMS] Failed to create EGL display");
			Console.WriteLine("[EGL] EGL display {0:x} created successfully", egl_display);

			if (!Egl.Initialize(egl_display, out major, out minor))
				throw new NotSupportedException("[EGL] Failed to initialize EGL display. Error code: " + Egl.GetError());

			if (!Egl.BindAPI (RenderApi.GL))
				throw new NotSupportedException("[EGL] Failed to bind EGL Api: " + Egl.GetError());

			Console.WriteLine ("[EGL] Version: " +  Marshal.PtrToStringAuto (Egl.QueryString (egl_display, Egl.VERSION)));
			Console.WriteLine ("[EGL] Vendor: " + Marshal.PtrToStringAuto (Egl.QueryString (egl_display, Egl.VENDOR)));
			Console.WriteLine ("[EGL] Extensions: " + Marshal.PtrToStringAuto (Egl.QueryString (egl_display, Egl.EXTENSIONS)));

			if (!Egl.ChooseConfig(egl_display, attribList, configs, configs.Length, out num_configs) || num_configs == 0)
				throw new NotSupportedException(String.Format("Failed to retrieve GraphicsMode, error {0}", Egl.GetError()));


			// See what we really got
			int d, s, sample_buffers, samples;
			IntPtr active_config = configs[0];
			Egl.GetConfigAttrib(egl_display, active_config, Egl.RED_SIZE, out r);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.GREEN_SIZE, out g);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.BLUE_SIZE, out b);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.ALPHA_SIZE, out a);
			//Egl.GetConfigAttrib(egl_display, active_config, Egl.DEPTH_SIZE, out d);
			//Egl.GetConfigAttrib(egl_display, active_config, Egl.STENCIL_SIZE, out s);
			//Egl.GetConfigAttrib(egl_display, active_config, Egl.SAMPLE_BUFFERS, out sample_buffers);
			//Egl.GetConfigAttrib(egl_display, active_config, Egl.SAMPLES, out samples);
//			Console.WriteLine ("EGL context: {0},{1},{2},{3} depth={4} stencil={5} samples={6} sample buffers={7}",
//				r, g, b, a, d, s, samples, sample_buffers);
			egl_config = active_config;

			egl_ctx = Egl.CreateContext(egl_display, egl_config, IntPtr.Zero, contextAttrib);

			egl_surface = Egl.CreateWindowSurface(egl_display, egl_config, gbm_surface, IntPtr.Zero);

			if (egl_surface==IntPtr.Zero)
				throw new NotSupportedException(String.Format("[EGL] Failed to create window surface, error {0}.", Egl.GetError()));

			if (!Egl.MakeCurrent(egl_display, egl_surface, egl_surface, egl_ctx))
				throw new NotSupportedException(string.Format("Failed to make context {0} current. Error: {1}", gbm_surface, Egl.GetError()));
			
			cursor_default = CreateCursor(gbm_device, Cursors.Default);
			cursor_empty = CreateCursor(gbm_device, Cursors.Empty);

			SetCursor(MouseCursor.Default);
			unsafe {				
				Drm.MoveCursor (fd, pEncoder->crtc_id, 50, 50);
			}
		}

		void initCairo (){
			cairoDev = new Cairo.EGLDevice (egl_display, egl_ctx);

			cairoSurf = new Cairo.GLSurface (cairoDev, egl_surface, originalMode.hdisplay, originalMode.vdisplay);
			//cairoSurf = new Cairo.EGLSurface (cairoDev, egl_surface, 1600, 900);

			cairoDev.SetThreadAware (false);

			if (cairoDev.Acquire () != Cairo.Status.Success)
				Console.WriteLine ("[Cairo]: Failed to acquire egl device.");
		}
		#endregion

		#region cursor
		static BufferObject CreateCursor(IntPtr gbm, MouseCursor cursor)
		{
			if (cursor.Width > 64 || cursor.Height > 64)
			{
				Debug.Print("[KMS] Cursor size {0}x{1} unsupported. Maximum is 64x64.",
					cursor.Width, cursor.Height);
				return default(BufferObject);
			}

			int width = 64;
			int height = 64;
			SurfaceFormat format = SurfaceFormat.ARGB8888;
			SurfaceFlags usage = SurfaceFlags.Cursor64x64 | SurfaceFlags.Write;

			Debug.Print("[KMS] Gbm.CreateBuffer({0:X}, {1}, {2}, {3}, {4}).",
				gbm, width, height, format, usage);

			BufferObject bo = Gbm.CreateBuffer(
				gbm, width, height, format, usage);

			if (bo == BufferObject.Zero)
			{
				Debug.Print("[KMS] Failed to create buffer.");
				return bo;
			}

			// Copy cursor.Data into a new buffer of the correct size
			byte[] cursor_data = new byte[width * height * 4];
			for (int y = 0; y < cursor.Height; y++)
			{
				int dst_offset = y * width * 4;
				int src_offset = y * cursor.Width * 4;
				int src_length = cursor.Width * 4;
				Array.Copy(
					cursor.Data, src_offset,
					cursor_data, dst_offset,
					src_length);
			}
			bo.Write(cursor_data);

			return bo;
		}
		void SetCursor(MouseCursor cursor)
		{
			BufferObject bo = default(BufferObject);
			if (cursor == MouseCursor.Default)
			{
				bo = cursor_default;
			}
			else if (cursor == MouseCursor.Empty)
			{
				bo = cursor_empty;
			}
			else
			{
				if (cursor_custom != BufferObject.Zero)
					cursor_custom.Dispose();
				cursor_custom = CreateCursor(gbm_device, cursor);
				bo = cursor_custom;
			}

			// If we failed to create a proper cursor, try falling back
			// to the empty cursor. We do not want to crash here!
			if (bo == BufferObject.Zero)
			{
				bo = cursor_empty;
			}

			if (bo != BufferObject.Zero)
			{
				unsafe {
					Drm.SetCursor (fd, pEncoder->crtc_id,
						bo.Handle, bo.Width, bo.Height, cursor.X, cursor.Y);
				}
			}
		}
		#endregion
		int x, y;

		void drawR (Cairo.Context ctx, int inc, double r, double g, double b){
			ctx.Rectangle (x+inc, y+inc, 200, 200);
			ctx.SetSourceRGB (r, g, b);
			ctx.Fill ();
		}
		public void RenderingLoop(){
			while (run){
				if (updateMousePos) {
					lock (Sync) {
						updateMousePos = false;
						unsafe {	
							Drm.MoveCursor (fd, pEncoder->crtc_id, MouseX, MouseY);
						}
					}
				}

				if (x > 700) {
					x = y = 0;
				} else {
					x+=1;
					y+=1;
				}
				using (Cairo.Context ctx = new Cairo.Context (cairoSurf)) {
					ctx.Rectangle (0, 0, 1024, 780);
					ctx.SetSourceRGB (0, 0, 1);
					ctx.Fill ();
					drawR (ctx, 0, 0, 1, 0);
				}
				cairoSurf.Flush ();

				cairoSurf.SwapBuffers ();

//				if (!Egl.SwapBuffers(egl_display, egl_surface))
//					throw new NotSupportedException(string.Format("Failed to swap buffers for context {0} current. Error: {1}", gbm_device, Egl.GetError()));

				if (Gbm.HasFreeBuffers (gbm_surface) == 0)
					throw new Exception ("[GBM]: Out of free buffers.");

				next_bo = Gbm.LockFrontBuffer (gbm_surface);
				if (next_bo == BufferObject.Zero)
					throw new Exception ("[GBM]: Failed to lock front buffer.");

				int width = next_bo.Width;
				int height = next_bo.Height;
				int bpp = 32;
				int depth = 24;
				int stride = next_bo.Stride;
				int hndBO = next_bo.Handle;

				int next_fb;
				int ret = Drm.ModeAddFB (fd, width, height,(byte)depth, (byte)bpp, stride, hndBO, out next_fb);
				if (ret != 0)
					throw new Exception ("[DRM]: ModeAddFB failed.");

				SetScanoutRegion (next_fb);

				is_flip_queued = true;
				unsafe{
					ret = Drm.ModePageFlip (fd, pEncoder->crtc_id, next_fb, PageFlipFlags.FlipEvent, IntPtr.Zero);
				}
				if (ret < 0)
					throw new Exception ("[DRM] Failed to enqueue framebuffer flip.");

				PollFD fds = new PollFD();
				fds.fd = fd;
				fds.events = PollFlags.In;

				EventContext evctx = new EventContext();
				evctx.version = EventContext.Version;
				evctx.page_flip_handler = PageFlipPtr;

				int timeout = -1;//block ? -1 : 0;

				while (is_flip_queued)
				{
					fds.revents = 0;
					if (Libc.poll(ref fds, 1, timeout) < 0)
						break;

					if ((fds.revents & (PollFlags.Hup | PollFlags.Error)) != 0)
						break;

					if ((fds.revents & PollFlags.In) != 0)
						Drm.HandleEvent(fd, ref evctx);
					else
						break;
				}
			}
		}

		#region rendering
		BufferObject bo, next_bo;
		int fb, next_fb;
		bool is_flip_queued;
		// We only support a SwapInterval of 0 (immediate)
		// or 1 (vsynced).
		// Todo: add support for SwapInterval of -1 (adaptive).
		// This requires a small change in WaitFlip().
		int swap_interval=0;

		readonly IntPtr PageFlipPtr;
		readonly PageFlipCallback PageFlip;

		void HandlePageFlip(int fd,	int sequence, int tv_sec, int tv_usec, IntPtr user_data)
		{
			is_flip_queued = false;
			if (fb != 0)
				Drm.ModeRmFB (fd, fb);
			fb = next_fb;
			next_fb = 0;
			if (bo != BufferObject.Zero)
				Gbm.ReleaseBuffer (gbm_surface, bo);
			bo = next_bo;
			next_bo = BufferObject.Zero;
		}

		static readonly DestroyUserDataCallback DestroyFB = HandleDestroyFB;
		static void HandleDestroyFB(BufferObject bo, IntPtr data)
		{
			IntPtr gbm = bo.Device;
			int fb = data.ToInt32();
			Debug.Print("[KMS] Destroying framebuffer {0}", fb);

			if (fb != 0)
				Drm.ModeRmFB(Gbm.DeviceGetFD(gbm), fb);
		}

		public void SwapBuffers()
		{
			if (!Egl.SwapBuffers(egl_display, egl_surface))
				throw new NotSupportedException(string.Format("Failed to swap buffers for context {0} current. Error: {1}", gbm_device, Egl.GetError()));
			
			if (is_flip_queued)
			{
				// Todo: if we don't wait for the page flip,
				// we drop all rendering buffers and get a crash
				// in Egl.SwapBuffers(). We need to fix that
				// before we can disable vsync.
				WaitFlip(true); // WaitFlip(SwapInterval > 0)
				if (is_flip_queued)
				{
					Debug.Print("[KMS] Dropping frame");
					return;
				}
			}

			next_bo = Gbm.LockFrontBuffer (gbm_surface);
			int fb = GetFramebuffer(next_bo);
			QueueFlip(fb);
		}

		public void Update()
		{
			WaitFlip(true);

			if (!Egl.SwapBuffers(egl_display, egl_surface))
				throw new NotSupportedException(string.Format("Failed to swap buffers for context {0} current. Error: {1}", gbm_device, Egl.GetError()));

			bo = Gbm.LockFrontBuffer (gbm_surface);
			int fb = GetFramebuffer(bo);
			SetScanoutRegion(fb);
		}


		void WaitFlip(bool block)
		{
			PollFD fds = new PollFD();
			fds.fd = fd;
			fds.events = PollFlags.In;

			EventContext evctx = new EventContext();
			evctx.version = EventContext.Version;
			evctx.page_flip_handler = PageFlipPtr;

			int timeout = block ? -1 : 0;

			while (is_flip_queued)
			{
				fds.revents = 0;
				if (Libc.poll(ref fds, 1, timeout) < 0)
					break;

				if ((fds.revents & (PollFlags.Hup | PollFlags.Error)) != 0)
					break;

				if ((fds.revents & PollFlags.In) != 0)
					Drm.HandleEvent(fd, ref evctx);
				else
					break;
			}

			// Page flip has taken place, update buffer objects
			if (!is_flip_queued)
			{				
				Gbm.ReleaseBuffer(gbm_surface, bo);
				bo = next_bo;
			}
		}

		void QueueFlip(int buffer)
		{
			unsafe
			{
				int ret = Drm.ModePageFlip (fd, pEncoder->crtc_id, buffer, PageFlipFlags.FlipEvent, IntPtr.Zero);
				if (ret < 0)
					Debug.Print("[KMS] Failed to enqueue framebuffer flip. Error: {0}", ret);

				is_flip_queued = true;
			}
		}

		void SetScanoutRegion(int buffer)
		{			
			unsafe
			{
				ModeInfo* mode = pConnector->modes;
				int connector_id = pConnector->connector_id;
				int crtc_id = pEncoder->crtc_id;

				int x = 0;
				int y = 0;
				int connector_count = 1;
				int ret = Drm.ModeSetCrtc(fd, crtc_id, buffer, x, y, &connector_id, connector_count, mode);

				if (ret != 0)
				{
					Debug.Print("[KMS] Drm.ModeSetCrtc{0}, {1}, {2}, {3}, {4:x}, {5}, {6:x}) failed. Error: {7}",
						fd, crtc_id, buffer, x, y, (IntPtr)connector_id, connector_count, (IntPtr)mode, ret);
				}
			}
		}
			
		int GetFramebuffer(BufferObject bo)
		{
			if (bo == BufferObject.Zero)
				goto fail;

			int bo_handle = bo.Handle;
			if (bo_handle == 0)
			{
				Debug.Print("[KMS] Gbm.BOGetHandle({0:x}) failed.", bo);
				goto fail;
			}

			int width = bo.Width;
			int height = bo.Height;
			int bpp = 32;
			int depth = 24;
			int stride = bo.Stride;

			if (width == 0 || height == 0 || bpp == 0)
			{
				Debug.Print("[KMS] Invalid framebuffer format: {0}x{1} {2} {3} {4}",
					width, height, stride, bpp, depth);
				goto fail;
			}

			int buffer;
			int ret = Drm.ModeAddFB (fd, width, height,(byte)depth, (byte)bpp, stride, bo_handle,out buffer);
			if (ret != 0)
			{
				Debug.Print("[KMS] Drm.ModeAddFB({0}, {1}, {2}, {3}, {4}, {5}, {6}) failed. Error: {7}",
					fd, width, height, depth, bpp, stride, bo_handle, ret);
				goto fail;
			}

			bo.SetUserData((IntPtr)buffer, DestroyFB);
			return buffer;

			fail:
			Debug.Print("[Error] Failed to create framebuffer.");
			return -1;
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			cairoDev.Release ();
			cairoSurf.Dispose ();
			cairoDev.Dispose ();

			if (fb != 0)
				Drm.ModeRmFB (fd, fb);
			if (bo != BufferObject.Zero)
				Gbm.ReleaseBuffer (gbm_surface, bo);			
			if (next_fb != 0)
				Drm.ModeRmFB (fd, next_fb);
			if (next_bo != BufferObject.Zero)
				Gbm.ReleaseBuffer (gbm_surface, next_bo);			

			if (Egl.GetCurrentContext () == egl_ctx) {
				Console.WriteLine ("destroying context");
				Egl.DestroyContext (egl_display, egl_ctx);
			}else
				Console.WriteLine ("not current");

			cairoDev.Dispose ();

			Drm.ModeFreeCrtc (Crtc);
			Drm.ModeFreeConnector(Connector);
			Drm.ModeFreeEncoder(Encoder);
			Libc.close(fd);
		}
		#endregion

		#region tests
		unsafe void dumpDrmResources(){
			ModeRes* resources = (ModeRes*)Drm.ModeGetResources(fd);
			if (resources == null)
				throw new NotSupportedException("[KMS] Drm.ModeGetResources failed.");
			Console.WriteLine("[KMS] DRM found {0} connectors", resources->count_connectors);

			Console.WriteLine ("[ENCODERS]");
			for (int j = 0; j < resources->count_encoders; j++) {							
				ModeEncoder* e = (ModeEncoder*)Drm.ModeGetEncoder(fd, *(resources->encoders + j));

				if (e == null)
					continue;
				Console.WriteLine ("{0}\t{1}\t{2}",e->encoder_id, e->encoder_type, e->crtc_id);

				Drm.ModeFreeEncoder((IntPtr)e);
			}

			Console.WriteLine ("\n[CONNECTORS]");
			ModeConnector* connector = null;
			for (int i = 0; i < resources->count_connectors; i++)
			{
				connector = (ModeConnector*)Drm.ModeGetConnector(fd, *(resources->connectors + i));
				if (connector != null)
				{
					Console.WriteLine ("{0}\t{1}\t{2}\t{3}",
						connector->connector_id,
						connector->connector_type,
						connector->connection,
						connector->encoder_id);

					for (int j = 0; j < connector->count_modes; j++) {
						ModeInfo* mode = connector->modes + j;
						if (mode == null)
							continue;
						Console.WriteLine ("\t{0,-20}{1,5}{2,5}{3,4} hz", 								
							new string(mode->name),
							mode->hdisplay,
							mode->vdisplay,
							mode->vrefresh);
					}
					Drm.ModeFreeConnector((IntPtr)connector);
					connector = null;
				}
			}
		}

		unsafe static void GetModes(LinuxDisplay display, DisplayResolution[] modes, out DisplayResolution current)
		{
			int mode_count = display.pConnector->count_modes;
			Console.WriteLine("[KMS] Display supports {0} mode(s)", mode_count);
			for (int i = 0; i < mode_count; i++)
			{
				ModeInfo* mode = display.pConnector->modes + i;
				if (mode != null)
				{
					Console.WriteLine("Mode {0}: {1}x{2} @{3}", i,
						mode->hdisplay, mode->vdisplay, mode->vrefresh);
					DisplayResolution res = GetDisplayResolution(mode);
					modes[i] = res;
				}
			}

			if (display.pCrtc->mode_valid != 0)
			{
				ModeInfo cmode = display.pCrtc->mode;
				current = GetDisplayResolution(&cmode);
			}
			else
			{
				current = GetDisplayResolution(display.pConnector->modes);
			}
			Console.WriteLine("Current mode: {0}", current.ToString());
		}
		unsafe static DisplayResolution GetDisplayResolution(ModeInfo* mode)
		{
			return new DisplayResolution(
				0, 0,
				mode->hdisplay, mode->vdisplay,
				32, // This is actually part of the framebuffer, not the DisplayResolution
				mode->vrefresh);
		}

		unsafe static ModeInfo* GetModeInfo(LinuxDisplay display, DisplayResolution resolution)
		{
			for (int i = 0; i < display.pConnector->count_modes; i++)
			{
				ModeInfo* mode = display.pConnector->modes + i;
				if (mode != null &&
					mode->hdisplay == resolution.Width &&
					mode->vdisplay == resolution.Height)
				{
					return mode;
				}
			}
			return null;
		}

		SurfaceFormat GetSurfaceFormat()
		{
			int format;
			Egl.GetConfigAttrib(egl_display, egl_config,
				Egl.NATIVE_VISUAL_ID, out format);
			if (format == 0)
				throw new Exception ("[KMS] Failed to retrieve EGL visual from GBM surface. Error: " + Egl.GetError());

			return (SurfaceFormat)format;
		}
		#endregion

		#region INPUT
		Thread input_thread;
		long exit;

		static readonly object Sync = new object();
		static readonly Crow.Key[] KeyMap = Evdev.KeyMap;
		static long DeviceFDCount;

		IntPtr udev;
		IntPtr input_context;

		int input_fd = 0;

		InputInterface input_interface = new InputInterface(
			OpenRestricted, CloseRestricted);
		static CloseRestrictedCallback CloseRestricted = CloseRestrictedHandler;
		static void CloseRestrictedHandler(int fd, IntPtr data)
		{
			Debug.Print("[Input] Closing fd {0}", fd);
			int ret = Libc.close(fd);

			if (ret < 0)
			{
				Debug.Print("[Input] Failed to close fd {0}. Error: {1}", fd, ret);
			}
			else
			{
				Interlocked.Decrement(ref DeviceFDCount);
			}
		}

		static OpenRestrictedCallback OpenRestricted = OpenRestrictedHandler;
		static int OpenRestrictedHandler(IntPtr path, int flags, IntPtr data) 
		{
			int fd = Libc.open(path, (OpenFlags)flags);
			Debug.Print("[Input] Opening '{0}' with flags {1}. fd:{2}",
				Marshal.PtrToStringAnsi(path), (OpenFlags)flags, fd);

			if (fd >= 0)
			{
				Interlocked.Increment(ref DeviceFDCount);
			}

			return fd;
		}

		void initInput (){
			Semaphore ready = new Semaphore(0, 1);
			input_thread = new Thread (InputThreadLoop);
			input_thread.IsBackground = true;
			input_thread.Start(ready);
		}

		void InputThreadLoop(object semaphore)
		{
			Debug.Print("[Input] Running on thread {0}", Thread.CurrentThread.ManagedThreadId);
			Setup();

			// Inform the parent thread that initialization has completed successfully
			(semaphore as Semaphore).Release();
			Debug.Print("[Input] Released main thread.", input_context);

			// Use a blocking poll for input messages, in order to reduce CPU usage
			PollFD poll_fd = new PollFD();
			poll_fd.fd = input_fd;
			poll_fd.events = PollFlags.In;
			Debug.Print("[Input] Created PollFD({0}, {1})", poll_fd.fd, poll_fd.events);

			Debug.Print("[Input] Entering input loop.", poll_fd.fd, poll_fd.events);
			while (Interlocked.Read(ref exit) == 0)
			{
				int ret = Libc.poll(ref poll_fd, 1, -1);
				ErrorNumber error = (ErrorNumber)Marshal.GetLastWin32Error();
				bool is_error =
					ret < 0 && !(error == ErrorNumber.Again || error == ErrorNumber.Interrupted) ||
					(poll_fd.revents & (PollFlags.Hup | PollFlags.Error | PollFlags.Invalid)) != 0;

				if (ret > 0 && (poll_fd.revents & (PollFlags.In | PollFlags.Pri)) != 0)
					ProcessEvents(input_context);

				if (is_error)
				{
					Debug.Print("[Input] Exiting input loop {0} due to poll error [ret:{1} events:{2}]. Error: {3}.",
						input_thread.ManagedThreadId, ret, poll_fd.revents, error);
					Interlocked.Increment(ref exit);
				}
			}
			Debug.Print("[Input] Exited input loop.", poll_fd.fd, poll_fd.events);
		}

		void Setup()
		{
			// Todo: add static path fallback when udev is not installed.
			udev = Udev.New();
			if (udev == IntPtr.Zero)
			{
				Debug.Print("[Input] Udev.New() failed.");
				Interlocked.Increment(ref exit);
				return;
			}
			Debug.Print("[Input] Udev.New() = {0:x}", udev);

			input_context = LibInput.CreateContext(input_interface, IntPtr.Zero, udev);
			if (input_context == IntPtr.Zero)
			{
				Debug.Print("[Input] LibInput.CreateContext({0:x}) failed.", udev);
				Interlocked.Increment(ref exit);
				return;
			}
			Debug.Print("[Input] LibInput.CreateContext({0:x}) = {1:x}", udev, input_context);

			string seat_id = "seat0";
			int seat_assignment = LibInput.AssignSeat(input_context, seat_id);
			if (seat_assignment == -1)
			{
				Debug.Print("[Input] LibInput.AssignSeat({0:x}) = {1} failed.", input_context, seat_id);
				Interlocked.Increment(ref exit);
				return;
			}
			Debug.Print("[Input] LibInput.AssignSeat({0:x}) = {1}", input_context, seat_id);

			input_fd = LibInput.GetFD(input_context);
			if (input_fd < 0)
			{
				Debug.Print("[Input] LibInput.GetFD({0:x}) failed.", input_context);
				Interlocked.Increment(ref exit);
				return;
			}
			Debug.Print("[Input] LibInput.GetFD({0:x}) = {1}.", input_context, input_fd);

			ProcessEvents(input_context);
			LibInput.Resume(input_context);
			Debug.Print("[Input] LibInput.Resume({0:x})", input_context);

			if (Interlocked.Read(ref DeviceFDCount) <= 0)
			{
				Debug.Print("[Error] Failed to open any input devices.");
				Debug.Print("[Error] Ensure that you have access to '/dev/input/event*'.");
				Interlocked.Increment(ref exit);
			}
		}

		void ProcessEvents(IntPtr input_context)
		{
			// Process all events in the event queue
			while (true)
			{				
				// Data available
				int ret = LibInput.Dispatch(input_context);
				if (ret != 0)
				{
					Debug.Print("[Input] LibInput.Dispatch({0:x}) failed. Error: {1}",
						input_context, ret);
					break;
				}

				IntPtr pevent = LibInput.GetEvent(input_context);
				if (pevent == IntPtr.Zero)
				{
					break;
				}

				IntPtr device = LibInput.GetDevice(pevent);
				InputEventType type = LibInput.GetEventType(pevent);

				lock (Sync)
				{
					switch (type)
					{
//					case InputEventType.DeviceAdded:
//						HandleDeviceAdded(input_context, device);
//						break;
//
//					case InputEventType.DeviceRemoved:
//						HandleDeviceRemoved(input_context, device);
//						break;
//
					case InputEventType.KeyboardKey:
						run = false;
						//HandleKeyboard(GetKeyboard(device), LibInput.GetKeyboardEvent(pevent));
						break;
//
//					case InputEventType.PointerAxis:
//						HandlePointerAxis(GetMouse(device), LibInput.GetPointerEvent(pevent));
//						break;
//
//					case InputEventType.PointerButton:
//						HandlePointerButton(GetMouse(device), LibInput.GetPointerEvent(pevent));
//						break;

					case InputEventType.PointerMotion:
						HandlePointerMotion(LibInput.GetPointerEvent(pevent));
						break;

//					case InputEventType.PointerMotionAbsolute:
//						HandlePointerMotionAbsolute(GetMouse(device), LibInput.GetPointerEvent(pevent));
//						break;
					}
				}

				LibInput.DestroyEvent(pevent);
			}
		}
		int MouseX = 0, MouseY = 0;
		volatile bool updateMousePos = true;

		void HandlePointerMotion(PointerEvent e)
		{			
			MouseX += (int)e.DeltaX;
			MouseY += (int)e.DeltaY;
			updateMousePos = true;
		}

		#endregion
	}
}


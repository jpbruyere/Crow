//
// DRMDevice.cs
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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK;
using OpenTK.Platform.Linux;
using System.IO.Compression;

namespace Linux
{
	
	internal class DRMContext : IDisposable
	{
		Stopwatch drmTimeOut;

		public Interface CrowInterface;
		int fd_gpu = 0;

		IntPtr gbm_device, gbm_surface, egl_display, egl_config, egl_surface, egl_ctx;

		ModeInfo originalMode, currentMode;
		public int width, height;

		volatile bool run = true;

		public IntPtr Connector, Crtc, Encoder;
		unsafe ModeCrtc* saved_crtc;
		unsafe ModeConnector* pConnector { get { return (ModeConnector*)Connector; } }
		unsafe ModeCrtc* pCrtc { get { return (ModeCrtc*)Crtc; } }
		unsafe ModeEncoder* pEncoder { get { return (ModeEncoder*)Encoder; } }

		Cairo.EGLDevice cairoDev;
		Cairo.GLSurface cairoSurf;

		Dictionary<KeyModifiers, string[]> keymap;

		int previousVT = 0, appVT;

		public DRMContext(string gpu_path = "/dev/dri/card0"){
			drmTimeOut = new Stopwatch();

			DestroyFB = HandleDestroyFB;
			DestroyFBPtr = Marshal.GetFunctionPointerForDelegate(DestroyFB);
			PageFlip = HandlePageFlip;
			PageFlipPtr = Marshal.GetFunctionPointerForDelegate(PageFlip);

			gbm_device = IntPtr.Zero;
			egl_display = IntPtr.Zero;

			fd_gpu = Libc.open(gpu_path, OpenFlags.ReadWrite | OpenFlags.CloseOnExec);
			if (fd_gpu < 0)
				throw new NotSupportedException("[KMS] Failed to open gpu");			
			Console.WriteLine("[KMS] GPU '{0}' opened as fd:{1}", gpu_path, fd_gpu);

			initDrm ();

			initGbm ();

			setNewMode ();

			initEgl ();

			initCairo ();

			initInput ();
		}

		#region init
		unsafe void initDrm(){			
			ModeRes* resources = (ModeRes*)Drm.ModeGetResources(fd_gpu);
			if (resources == null)
				throw new NotSupportedException("[KMS] Drm.ModeGetResources failed.");

			ModeConnector* connector = null;
			for (int i = 0; i < resources->count_connectors; i++) {
				connector = (ModeConnector*)Drm.ModeGetConnector (fd_gpu, *(resources->connectors + i));
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
			Encoder = Drm.ModeGetEncoder (fd_gpu, connector->encoder_id);
			Crtc = Drm.ModeGetCrtc(fd_gpu, pEncoder->crtc_id);
			saved_crtc = (ModeCrtc*) Drm.ModeGetCrtc (fd_gpu, pEncoder->crtc_id);

			originalMode = pCrtc->mode;
			width = 1600;
			height = 900;

			//			Console.WriteLine ("[DRM]: current mode = {0} X {1} at {2} Hz", width, height, currentMode.vrefresh);
		}
		void initGbm (){
			gbm_device = Gbm.CreateDevice(fd_gpu);
			if (gbm_device == IntPtr.Zero)
				throw new NotSupportedException("[GBM] Failed to create GBM device");			

			gbm_surface =  Gbm.CreateSurface(gbm_device, width, height, SurfaceFormat.ARGB8888, SurfaceFlags.Rendering | SurfaceFlags.Scanout);
			if (gbm_surface == IntPtr.Zero)
				throw new NotSupportedException("[GBM] Failed to create GBM surface for rendering");						
		}

		void setNewMode (){
			//118.25  1600 1696 1856 2112  900 903 908 934 -hsync +vsync
			//currentMode = pCrtc->mode;
			currentMode.clock = 118250;
			currentMode.hdisplay = 1600;
			currentMode.hsync_start = 1696;
			currentMode.hsync_end = 1856;
			currentMode.htotal = 2112;
			currentMode.vdisplay = 900;
			currentMode.vsync_start = 903;
			currentMode.vsync_end = 908;
			currentMode.vtotal = 934;
			currentMode.flags |= (uint)ModeFlags.NHSYNC;
			currentMode.flags |= (uint)ModeFlags.PVSYNC;
			//			byte[] tmp = System.Text.Encoding.ASCII.GetBytes ("1600x900");
			//			for (int i = 0; i < tmp.Length; i++) {
			//				currentMode.name [i] = (sbyte)tmp [i];
			//			}

			unsafe
			{
				pCrtc->mode = currentMode;
				ModeInfo* mode = (ModeInfo*)Marshal.AllocHGlobal (sizeof(ModeInfo));// pConnector->modes;
				*mode = currentMode;
				int connector_id = pConnector->connector_id;
				int crtc_id = pEncoder->crtc_id;
				BufferObject bo = Gbm.CreateBuffer(
					gbm_device, width, height, SurfaceFormat.ARGB8888, SurfaceFlags.Scanout);
				int nfb = getFbFromBo (bo);
				int ret = Drm.ModeSetCrtc (fd_gpu, crtc_id, nfb, 0, 0, &connector_id, 1, mode);

				if (ret != 0)
					Console.WriteLine("[KMS] Drm.ModeSetCrtc failed. Error: " + ret);				
			}
			//			width = currentMode.hdisplay;
			//			height = currentMode.vdisplay;			
		}

		unsafe void initEgl () {
			int major, minor;
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
			Console.WriteLine ("                  " + Marshal.PtrToStringAuto (Egl.QueryString(IntPtr.Zero, Egl.EXTENSIONS)));

			if (!Egl.ChooseConfig(egl_display, attribList, configs, configs.Length, out num_configs) || num_configs == 0)
				throw new NotSupportedException(String.Format("Failed to retrieve GraphicsMode, error {0}", Egl.GetError()));

			egl_config = configs[0];
			egl_ctx = Egl.CreateContext(egl_display, egl_config, IntPtr.Zero, contextAttrib);
			egl_surface = Egl.CreateWindowSurface(egl_display, egl_config, gbm_surface, IntPtr.Zero);

			if (egl_surface==IntPtr.Zero)
				throw new NotSupportedException(String.Format("[EGL] Failed to create window surface, error {0}.", Egl.GetError()));

			// See what we really got
			int r, g, b, a, d, s, sample_buffers, samples;
			IntPtr active_config = configs[0];

			Egl.GetConfigAttrib(egl_display, active_config, Egl.RED_SIZE, out r);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.GREEN_SIZE, out g);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.BLUE_SIZE, out b);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.ALPHA_SIZE, out a);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.DEPTH_SIZE, out d);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.STENCIL_SIZE, out s);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.SAMPLE_BUFFERS, out sample_buffers);
			Egl.GetConfigAttrib(egl_display, active_config, Egl.SAMPLES, out samples);
			Console.WriteLine ("EGL context: {0},{1},{2},{3} depth={4} stencil={5} samples={6} sample buffers={7}",
							r, g, b, a, d, s, samples, sample_buffers);


			if (!Egl.MakeCurrent(egl_display, egl_surface, egl_surface, egl_ctx))
				throw new NotSupportedException(string.Format("Failed to make context {0} current. Error: {1}", gbm_surface, Egl.GetError()));
		}

		void initCairo (){
			cairoDev = new Cairo.EGLDevice (egl_display, egl_ctx);

			cairoSurf = new Cairo.GLSurface (cairoDev, egl_surface, width, height);
			//cairoSurf = new Cairo.EGLSurface (cairoDev, egl_surface, 1600, 900);

			cairoDev.SetThreadAware (false);

			if (cairoDev.Acquire () != Cairo.Status.Success)
				Console.WriteLine ("[Cairo]: Failed to acquire egl device.");
		}
		#endregion

		#region cursor
		BufferObject boMouseCursor = BufferObject.Zero;

		internal void updateCursor (XCursor cursor) {
			uint width = 64, height = 64;
			if (cursor.Width > width || cursor.Height > height){
				Debug.Print("[DRM] Cursor size {0}x{1} unsupported. Maximum is 64x64.",
					cursor.Width, cursor.Height);
				return;
			}

			if (boMouseCursor != BufferObject.Zero)
				Gbm.DestroyBuffer (boMouseCursor);

			boMouseCursor = Gbm.CreateBuffer (gbm_device, (int)width, (int)height,
				SurfaceFormat.ARGB8888, SurfaceFlags.Cursor64x64 | SurfaceFlags.Write);

			if (boMouseCursor == BufferObject.Zero)
			{
				Debug.Print("[DRM] Failed to create buffer for mouse cursor.");
				return;
			}

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

			boMouseCursor.Write(cursor_data);

			unsafe {				
				Drm.SetCursor (fd_gpu, pEncoder->crtc_id,
					boMouseCursor.Handle, boMouseCursor.Width, boMouseCursor.Height, (int)cursor.Xhot, (int)cursor.Yhot);
			}
		}
		#endregion

		int getFbFromBo (BufferObject bo){
			int width = bo.Width;
			int height = bo.Height;
			int bpp = 32;
			int depth = 24;
			int stride = bo.Stride;
			int hndBO = bo.Handle;

			int fb;
			int ret = Drm.ModeAddFB (fd_gpu, width, height,(byte)depth, (byte)bpp, stride, hndBO, out fb);
			if (ret != 0)
				throw new Exception ("[DRM]: ModeAddFB failed.");
			bo.SetUserData ((IntPtr)fb, DestroyFBPtr);
			return fb;
		}

		public void Run(){
			BufferObject bo;
			int fb;

			PollFD fds = new PollFD();
			fds.fd = fd_gpu;
			fds.events = PollFlags.In;

			EventContext evctx = new EventContext();
			evctx.version = EventContext.Version;
			evctx.page_flip_handler = PageFlipPtr;

			int timeout = -1;//block ? -1 : 0;

			using (Cairo.Context ctx = new Cairo.Context (cairoSurf)) {
				ctx.Rectangle (0, 0, width, height);
				ctx.SetSourceRGB (0, 0, 0);
				ctx.Fill ();
			}

			cairoSurf.SwapBuffers ();

			bo = Gbm.LockFrontBuffer (gbm_surface);
			fb = getFbFromBo (bo);

			SetScanoutRegion (fb);
			drmTimeOut.Restart();

			while (run && drmTimeOut.ElapsedMilliseconds < 10000){				
				BufferObject next_bo;
				bool update = false;

				if (updateMousePos) {
					lock (Sync) {
						updateMousePos = false;
						unsafe {	
							Drm.MoveCursor (fd_gpu, pEncoder->crtc_id, MouseX-8, MouseY-4);
						}
					}
				}

				if (Monitor.TryEnter (CrowInterface.RenderMutex)) {
					if (CrowInterface.IsDirty) {
						CrowInterface.IsDirty = false;
						update = true;
						using (Cairo.Context ctx = new Cairo.Context (cairoSurf)) {
							using (Cairo.Surface d = new Cairo.ImageSurface (CrowInterface.dirtyBmp, Cairo.Format.Argb32,
								width, height, width * 4)) {
								ctx.SetSourceSurface (d, 0, 0);
								ctx.Operator = Cairo.Operator.Source;
								ctx.Paint ();
							}
						}
					}
					Monitor.Exit (CrowInterface.RenderMutex);
				}

				if (!update)
					continue;
				update = false;

				cairoSurf.Flush ();
				cairoSurf.SwapBuffers ();

				if (Gbm.HasFreeBuffers (gbm_surface) == 0)
					throw new Exception ("[GBM]: Out of free buffers.");

				next_bo = Gbm.LockFrontBuffer (gbm_surface);
				if (next_bo == BufferObject.Zero)
					throw new Exception ("[GBM]: Failed to lock front buffer.");

				fb = getFbFromBo (next_bo);

				unsafe{
					int is_flip_queued = 1;

					while (Drm.ModePageFlip (fd_gpu, pEncoder->crtc_id, fb, PageFlipFlags.FlipEvent, ref is_flip_queued) < 0) {
						//Console.WriteLine ("[DRM] Failed to enqueue framebuffer flip.");				
						continue;
					}

					while (is_flip_queued != 0)
					{
						fds.revents = 0;
						if (Libc.poll (ref fds, 1, timeout) < 0)
							break;						

						if ((fds.revents & (PollFlags.Hup | PollFlags.Error)) != 0)
							break;

						if ((fds.revents & PollFlags.In) != 0)
							Drm.HandleEvent (fd_gpu, ref evctx);
						else
							break;
						Thread.Sleep (1);
					}
					if (is_flip_queued != 0)
						Console.WriteLine ("flip canceled");

					Gbm.ReleaseBuffer (gbm_surface, bo);
					//Drm.ModeRmFB(fd_gpu, fb);

					bo = next_bo;
					next_bo = BufferObject.Zero;

				}
			}
		}

		#region rendering



		// We only support a SwapInterval of 0 (immediate)
		// or 1 (vsynced).
		// Todo: add support for SwapInterval of -1 (adaptive).
		// This requires a small change in WaitFlip().
		int swap_interval=0;

		readonly IntPtr PageFlipPtr;
		readonly PageFlipCallback PageFlip;

		readonly IntPtr DestroyFBPtr;
		readonly DestroyUserDataCallback DestroyFB;

		void HandlePageFlip(int fd,	int sequence, int tv_sec, int tv_usec, ref int user_data)
		{
			user_data = 0;
		}


		void HandleDestroyFB(BufferObject bo, IntPtr data)
		{
			Console.WriteLine ("DestroyFB");
			IntPtr gbm = bo.Device;
			int fb = data.ToInt32();

			if (fb != 0)
				Drm.ModeRmFB(fd_gpu, fb);
		}

		void SetScanoutRegion(int buffer)
		{			
			unsafe
			{
				ModeInfo* mode = pConnector->modes;
				int connector_id = pConnector->connector_id;
				int crtc_id = pEncoder->crtc_id;

				int ret = Drm.ModeSetCrtc (fd_gpu, crtc_id, buffer, 0, 0, &connector_id, 1, mode);

				if (ret != 0)
					Debug.Print("[KMS] Drm.ModeSetCrtc{0}, {1}, {2} failed. Error: {3}",
						fd_gpu, crtc_id, buffer, ret);				
				Console.WriteLine ("scanout region set: {0}x{1}", mode->hdisplay, mode->vdisplay);
			}
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			cairoDev.Release ();
			cairoSurf.Dispose ();
			cairoDev.Dispose ();

			//			if (fb != 0)
			//				Drm.ModeRmFB (fd_gpu, fb);
			//			if (bo != BufferObject.Zero)
			//				Gbm.ReleaseBuffer (gbm_surface, bo);			
			//			if (next_fb != 0)
			//				Drm.ModeRmFB (fd_gpu, next_fb);
			//			if (next_bo != BufferObject.Zero)
			//				Gbm.ReleaseBuffer (gbm_surface, next_bo);			

			if (Egl.GetCurrentContext () == egl_ctx) {
				Console.WriteLine ("destroying context");
				Egl.DestroyContext (egl_display, egl_ctx);
			}else
				Console.WriteLine ("not current");

			cairoDev.Dispose ();

			unsafe{
				Drm.ModeSetCrtc (fd_gpu, saved_crtc->crtc_id, saved_crtc->buffer_id,
					saved_crtc->x, saved_crtc->y, &pConnector->connector_id, 1, &saved_crtc->mode);
				Drm.ModeFreeCrtc ((IntPtr)saved_crtc);
			}

			Drm.ModeFreeCrtc (Crtc);
			Drm.ModeFreeConnector(Connector);
			Drm.ModeFreeEncoder(Encoder);
			Libc.close(fd_gpu);

//			using(VTControler master = new VTControler()){
//				master.CurrentMode = TTY.Mode.TEXT;
//				if (master.CurrentVT != previousVT) {
//					master.SwitchTo (previousVT);
//				}
//			}
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
				drmTimeOut.Restart ();

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
						//run = false;
						handleKeyboard(LibInput.GetKeyboardEvent(pevent));
						break;
						//
						//					case InputEventType.PointerAxis:
						//						HandlePointerAxis(GetMouse(device), LibInput.GetPointerEvent(pevent));
						//						break;
						//
					case InputEventType.PointerButton:
						handlePointerButton (LibInput.GetPointerEvent(pevent));
						break;

					case InputEventType.PointerMotion:
						handlePointerMotion (LibInput.GetPointerEvent(pevent));
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

		int roundDelta (double d){
			return d > 0 ? (int)Math.Ceiling(d) : (int)Math.Floor (d);
		}

		void handlePointerMotion(PointerEvent e)
		{
			MouseX += roundDelta (e.DeltaX);
			MouseY += roundDelta (e.DeltaY);

			Rectangle bounds = CrowInterface.ClientRectangle;
			if (MouseX < bounds.Left)
				MouseX = bounds.Left;
			else if (MouseX > bounds.Right)
				MouseX = bounds.Right;

			if (MouseY < bounds.Top)
				MouseY = bounds.Top;
			else if (MouseY > bounds.Bottom)
				MouseY = bounds.Bottom;

			CrowInterface.ProcessMouseMove (MouseX, MouseY);

			updateMousePos = true;
		}
		void handlePointerButton (PointerEvent e)
		{			
			int but = 0;
			switch (e.Button) {
			case EvdevButton.LEFT:
				but = 0;
				break;
			case EvdevButton.MIDDLE:
				but = 1;
				break;
			case EvdevButton.RIGHT:
				but = 2;
				break;
			}
			if (e.ButtonState == ButtonState.Pressed)
				CrowInterface.ProcessMouseButtonDown (but);
			else
				CrowInterface.ProcessMouseButtonUp (but);
		}

		KeyModifiers curModifiers = KeyModifiers.None;

		void handleKeyboard(KeyboardEvent e)
		{
			return;
			int key = (int)Evdev.KeyMap [e.Key];
			Key k = (Key)key;
			if (e.KeyState == KeyState.Pressed) {
				CrowInterface.ProcessKeyDown (key);
				switch (k) {
				case Key.ShiftLeft:
				case Key.ShiftRight:
					curModifiers |= KeyModifiers.Shift;
					break;
				case Key.ControlLeft:
				case Key.ControlRight:
					curModifiers |= KeyModifiers.Control;
					break;
				case Key.AltLeft:
					curModifiers |= KeyModifiers.Alt;
					break;
				case Key.AltRight:
					curModifiers |= KeyModifiers.AltGr;
					break;
				}
			}else {
				CrowInterface.ProcessKeyUp (key);
				switch (k) {
				case Key.ShiftLeft:
				case Key.ShiftRight:
					curModifiers &= ~KeyModifiers.Shift;
					break;
				case Key.ControlLeft:
				case Key.ControlRight:
					curModifiers &= ~KeyModifiers.Control;
					break;
				case Key.AltLeft:
					curModifiers &= ~KeyModifiers.Alt;
					break;
				case Key.AltRight:
					curModifiers &= ~KeyModifiers.AltGr;
					break;
				}
				if (!keymap.ContainsKey (curModifiers)) {
					Console.WriteLine ("keymap not found for: " + curModifiers + " " + (int)curModifiers);
					return;
				}
				string tmp = keymap [curModifiers] [e.Key];
//				if (string.IsNullOrEmpty (tmp))
//					return;
//				if (char.IsControl (tmp[0]))
//					return;
//				CrowInterface.ProcessKeyPress (tmp [0]);
			}			
		}

		#endregion
	}
}





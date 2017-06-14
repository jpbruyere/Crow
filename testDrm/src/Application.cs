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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Linux;
using Linux.VT;
using System.Text;
using Linux.oldEvDev;
using Linux.DRI;

namespace Crow
{
	public class Application : IDisposable
	{
		public enum RunState {
			ActivateRequest,
			DesactivateRequest,
			Paused,
			Running,
		}
		#if MEASURE_TIME
		public List<PerformanceMeasure> PerfMeasures;
		protected PerformanceMeasure glDrawMeasure = new PerformanceMeasure("OpenGL Draw", 10);
		#endif

		public bool Running = true;
		public volatile RunState CurrentState = RunState.Running;

		protected Interface CrowInterface;
		protected DRIControler gpu;
		protected Cairo.GLSurface cairoSurf { get { return gpu?.CairoSurf; }}

		protected bool mouseIsInInterface = false;

		protected volatile int ifaceSleep = 1, updateSleep = 0;

		void interfaceThread()
		{
			while (CrowInterface.ClientRectangle.Size.Width == 0)
				Thread.Sleep (5);

			while (true) {
				if (CurrentState == RunState.Running) {
					CrowInterface.Update ();
					Thread.Sleep (ifaceSleep);
				} else
					Thread.Sleep (1000);
			}
		}

		int previousVT = -1, appVT = -1;

		public Application(){
			if (Kernel.signal (Signal.SIGUSR1, switch_request_handle) < 0)
				throw new Exception ("signal handler registation failed");			
//			if (Kernel.signal (Signal.SIGUSR2, switch_request_handle) < 0)
//				throw new Exception ("signal handler registation failed");
			if (Kernel.signal (Signal.SIGINT, sigint_handler) < 0)
				throw new Exception ("SIGINT handler registation failed");

			using (VTControler master = new VTControler ()) {
				previousVT = master.CurrentVT;
				appVT = master.FirstAvailableVT;

				master.SwitchTo (appVT);

				try {
					master.KDMode = KDMode.GRAPHICS;
//					VT.vt_mode vtm = master.VTMode;
//					vtm.mode = VT.SwitchMode.PROCESS;
//					master.VTMode = vtm;
				} catch (Exception ex) {
					Console.WriteLine (ex.ToString ());	
				}
			}
				
			gpu = new DRIControler();

			initCrow ();

			initInput ();

			MouseX = gpu.Width / 2;
			MouseY = gpu.Height / 2;

			initCursor ();
		}

		#region CROW
		public GraphicObject Load (string path){
			return CrowInterface.LoadInterface (path);
		}

		void initCrow () {
			CrowInterface = new Interface ();

			Thread t = new Thread (interfaceThread);
			t.Name = "Interface";
			t.IsBackground = true;
			t.Start ();

			CrowInterface.ProcessResize (new Size (gpu.Width, gpu.Height));
			CrowInterface.MouseCursorChanged += CrowInterface_MouseCursorChanged;

			#if MEASURE_TIME
			PerfMeasures = new List<PerformanceMeasure> (
				new PerformanceMeasure[] {
					this.CrowInterface.updateMeasure,
					this.CrowInterface.layoutingMeasure,
					this.CrowInterface.clippingMeasure,
					this.CrowInterface.drawingMeasure,
					this.glDrawMeasure
				}
			);
			#endif
		}
		void CrowInterface_MouseCursorChanged (object sender, MouseCursorChangedEventArgs e)
		{
			gpu.updateCursor (e.NewCursor);
		}
		#endregion

		void initCursor(){
			gpu.updateCursor (XCursor.Default);
			gpu.moveCursor ((uint)MouseX - 8, (uint)MouseY - 4);
		}

		void switch_request_handle (Signal s){
			Console.WriteLine ("****** switch request catched: " + s.ToString());
			using (VTControler master = new VTControler ()) {
				Libc.write (master.fd, Encoding.ASCII.GetBytes ("this is a test string"));
				master.AcknoledgeSwitchRequest ();
			}			
		}
		void sigint_handler (Signal s){
			Console.WriteLine ("{0}: SIGINT catched");
			Running = false;
		}

		public virtual void Run ()
		{
			int cpt = 0;
			while(Running){				
				switch (CurrentState) {
				case RunState.ActivateRequest:
					activate ();
					continue;
				case RunState.DesactivateRequest:
					desactivate ();
					continue;
				case RunState.Paused:
					Thread.Sleep (1000);
					continue;
				}

				uiDraw ();

				#if MEASURE_TIME
				if (cpt%10==0){
					foreach (PerformanceMeasure m in PerfMeasures)
						m.NotifyChanges();
				}
				#endif

				cpt++;
				Thread.Sleep (updateSleep);
			}
		}
		protected virtual void uiDraw (){
			#if MEASURE_TIME
			glDrawMeasure.StartCycle();
			#endif

			bool update = false;

			if (updateMousePos) {
				lock (Sync) {
					updateMousePos = false;
					gpu.moveCursor ((uint)MouseX - 8, (uint)MouseY - 4);
				}
			}

			if (Monitor.TryEnter (CrowInterface.RenderMutex)) {
				if (CrowInterface.IsDirty) {
					CrowInterface.IsDirty = false;
					update = true;
					Rectangle r = CrowInterface.DirtyRect;
					using (Cairo.Context ctx = new Cairo.Context (cairoSurf)) {
						using (Cairo.Surface d = new Cairo.ImageSurface (CrowInterface.dirtyBmp, Cairo.Format.ARGB32,
							r.Width, r.Height, r.Width * 4)) {
							ctx.SetSourceSurface (d, 0, 0);
							ctx.Operator = Cairo.Operator.Source;
							ctx.Paint ();
						}
					}
				}
				Monitor.Exit (CrowInterface.RenderMutex);
			}

			if (!update)
				return;
			update = false;

			cairoSurf.Flush ();
			cairoSurf.SwapBuffers ();

			//gpu.UpdateWithPageFlip ();
			gpu.Update();

			#if MEASURE_TIME
			glDrawMeasure.StopCycle ();
			#endif
		}
			
		#region INPUT
		Thread input_thread;
		long exit;

		static readonly object Sync = new object();
		static readonly Crow.Key[] KeyMap = EvdevClass.KeyMap;
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
			input_thread.Name = "input_thread"; 
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
				if (CurrentState != RunState.Running) {
					Thread.Sleep (1000);
					continue;
				}
				//drmTimeOut.Restart ();

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
					case InputEventType.PointerAxis:
						handlePointerAxis(LibInput.GetPointerEvent(pevent));
						break;

					case InputEventType.PointerButton:
						handlePointerButton (LibInput.GetPointerEvent(pevent));
						break;

					case InputEventType.PointerMotion:
						handlePointerMotion (LibInput.GetPointerEvent(pevent));
						break;

//					case InputEventType.PointerMotionAbsolute:
//						handlePointerMotionAbsolute(LibInput.GetPointerEvent(pevent));
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
		void handlePointerAxis(PointerEvent e){
//			if (e.HasAxis(PointerAxis.HorizontalScroll))
//			{
//				CrowInterface.ProcessMouseWheelChanged ((float)e.AxisValue (PointerAxis.HorizontalScroll));
//			}
			if (e.HasAxis(PointerAxis.VerticalScroll))
			{
				CrowInterface.ProcessMouseWheelChanged ((float)-e.AxisValue (PointerAxis.VerticalScroll));
			}
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
			if (e.ButtonState == global::Linux.ButtonState.Pressed)
				CrowInterface.ProcessMouseButtonDown (but);
			else
				CrowInterface.ProcessMouseButtonUp (but);
		}

		KeyModifiers curModifiers = KeyModifiers.None;

		void handleKeyboard(KeyboardEvent e)
		{			
			int key = (int)EvdevClass.KeyMap [e.Key];
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
//				if (!keymap.ContainsKey (curModifiers)) {
//					Console.WriteLine ("keymap not found for: " + curModifiers + " " + (int)curModifiers);
//					return;
//				}
				//				string tmp = keymap [curModifiers] [e.Key];
				//				if (string.IsNullOrEmpty (tmp))
				//					return;
				//				if (char.IsControl (tmp[0]))
				//					return;
				//				CrowInterface.ProcessKeyPress (tmp [0]);
			}			
		}

		#endregion



		void desactivate () {
			CurrentState = RunState.Paused;
			gpu.Dispose ();
			gpu = null;
		}
		void activate (){
			gpu = new DRIControler();
			initCursor ();
			CurrentState = RunState.Running;
		}

		#region IDisposable implementation
		~Application(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (gpu != null)
				gpu.Dispose ();
			gpu = null;

			using (VTControler master = new VTControler ()) {
				try {
					master.KDMode = KDMode.TEXT;
				} catch (Exception ex) {
					Console.WriteLine (ex.ToString ());	
				}
				master.SwitchTo (previousVT);
			}

		}
		#endregion
	}
}


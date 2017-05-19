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

using VT = Linux.VT;
using DRI = Linux.DRI;

using Linux;
using System.Text;
using OpenTK.Platform.Linux;

namespace Crow
{
	public class Application : IDisposable
	{
		public bool Running = true;
		DRI.GPUControler gpu;
		Cairo.GLSurface cairoSurf;

		public Interface CrowInterface;

		public bool mouseIsInInterface = false;

		void interfaceThread()
		{
			while (CrowInterface.ClientRectangle.Size.Width == 0)
				Thread.Sleep (5);

			while (true) {
				CrowInterface.Update ();
				Thread.Sleep (1);
			}
		}
//
		Crow.XCursor cursor;
		int previousVT = -1, appVT = -1;

		public Application(){
			if (Kernel.signal (Signal.SIGUSR1, switch_request_handle) < 0)
				throw new Exception ("signal handler registation failed");			
			if (Kernel.signal (Signal.SIGINT, sigint_handler) < 0)
				throw new Exception ("SIGINT handler registation failed");

			using (VT.VTControler master = new VT.VTControler ()) {
				previousVT = master.CurrentVT;
				appVT = master.FirstAvailableVT;
				master.SwitchTo (appVT);
				try {
					master.KDMode = VT.KDMode.GRAPHICS;
				} catch (Exception ex) {
					Console.WriteLine (ex.ToString ());	
				}
			}

			gpu = new DRI.GPUControler();
			cairoSurf = gpu.CairoSurf;

			CrowInterface = new Interface ();

			Thread t = new Thread (interfaceThread);
			t.Name = "Interface";
			t.IsBackground = true;
			t.Start ();

			initInput ();

			CrowInterface.ProcessResize (new Size (gpu.Width, gpu.Height));
			cursor = Crow.XCursorFile.Load("#Crow.Images.Icons.Cursors.arrow").Cursors[0];
			gpu.updateCursor (cursor);
			//CrowInterface.MouseCursorChanged += CrowInterface_MouseCursorChanged;
		}
		void switch_request_handle (Signal s){
			Console.WriteLine ("switch request catched: " + s.ToString());
			using (VT.VTControler master = new VT.VTControler ()) {
				Libc.write (master.fd, Encoding.ASCII.GetBytes ("this is a test string"));
				master.AcknoledgeSwitchRequest ();
			}			
		}
		void sigint_handler (Signal s){
			Console.WriteLine ("{0}: SIGINT catched");
			Running = false;
		}
//		void CrowInterface_MouseCursorChanged (object sender, MouseCursorChangedEventArgs e)
//		{
//			gpu.updateCursor (e.NewCursor);
//		}

		public GraphicObject Load (string path){
			return CrowInterface.LoadInterface (path);
		}
		public virtual void Run (){
			updateCrow ();
		}

		public void updateCrow (){
			bool update = false;

			if (updateMousePos) {
				lock (Sync) {
					updateMousePos = false;
					gpu.moveCursor ((uint)MouseX - 8, (uint)MouseY - 4);
				}
			}

//			using (Cairo.Context ctx = new Cairo.Context (cairoSurf)) {
//				ctx.Rectangle (0, 0, gpu.Width, gpu.Height);
//				ctx.SetSourceRGB (0, 0, 1);
//				ctx.Fill ();
//				ctx.Rectangle (5, 5, 50, 50);
//				ctx.SetSourceRGB (1, 0, 0);
//				ctx.Fill ();
//				ctx.Rectangle (1550, 850, 50, 50);
//				ctx.SetSourceRGB (0, 1, 0);
//				ctx.Fill ();
//			}
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
//
//			if (!update)
//				return;
//			update = false;

			cairoSurf.Flush ();
			cairoSurf.SwapBuffers ();

			gpu.Update ();
			//Thread.Sleep (1);
			//gpu.MarkFBDirty ();
		}


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
			if (e.ButtonState == global::Linux.ButtonState.Pressed)
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

			using (VT.VTControler master = new VT.VTControler ()) {
				//				try {
				//					master.KDMode = VT.KDMode.TEXT;
				//				} catch (Exception ex) {
				//					Console.WriteLine (ex.ToString ());	
				//				}
				master.SwitchTo (previousVT);
			}

		}
		#endregion
	}
}


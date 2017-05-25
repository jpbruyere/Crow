//
// testLibInput.cs
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
using System.Diagnostics;
using Linux;
using System.Runtime.InteropServices;
using System.Threading;
using Crow;
using System.IO;

namespace testDrm
{
	public class testLibInput
	{
		static void Main(){

			testLibInput tli = new testLibInput();
			tli.run ();
		}
		public testLibInput ()
		{
			Console.WriteLine ("starting");
			initInput ();
		}
		void run(){
			while (true)
				Console.ReadKey (true);
		}
		#region INPUT
		Thread input_thread;
		long exit;

		static readonly object Sync = new object();
		static readonly Crow.Key[] KeyMap = Linux.oldEvDev.EvdevClass.KeyMap;
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


						//					case InputEventType.PointerMotionAbsolute:
						//						handlePointerMotionAbsolute(LibInput.GetPointerEvent(pevent));
						//						break;
					}
				}

				LibInput.DestroyEvent(pevent);
			}
		}



		KeyModifiers curModifiers = KeyModifiers.None;

		void handleKeyboard(KeyboardEvent e)
		{			
			int key = (int)Linux.oldEvDev.EvdevClass.KeyMap [e.Key];
			Key k = (Key)key;
			if (e.KeyState == KeyState.Pressed) {
				Console.WriteLine ("KeyDown: raw:{0} evdev:{1} key:{2}",e.Key, key,k);
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
				Console.WriteLine ("KeyUp: raw:{0} evdev:{1} key:{2}",e.Key, key,k);
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

	}
}


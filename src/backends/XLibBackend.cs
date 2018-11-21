//
// XLibBackend.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.Runtime.InteropServices;

namespace Crow.XLIB
{
	public class XLibBackend : IBackend
	{
		#region pinvoke
		[DllImportAttribute("X11")]
		static extern int XInitThreads();
		[DllImportAttribute("X11")]
		static extern IntPtr XOpenDisplay(IntPtr displayName);
		[DllImportAttribute("X11")]
		static extern IntPtr XCloseDisplay(IntPtr disp);
		[DllImportAttribute("X11")]
		static extern Int32 XDefaultScreen(IntPtr disp);
		[DllImportAttribute("X11")]
		static extern IntPtr XDefaultRootWindow(IntPtr disp);
		[DllImportAttribute("X11")]
		static extern UInt32 XDefaultDepth (IntPtr disp, Int32 screen);
		[DllImportAttribute("X11")]
		static extern IntPtr XDefaultVisual(IntPtr disp, Int32 screen);
		[DllImportAttribute("X11")]
		static extern IntPtr XCreateSimpleWindow(IntPtr disp, IntPtr rootWindow, Int32 x, Int32 y, UInt32 width, UInt32 height,
			UInt32 borderWidth, IntPtr border, IntPtr background);
		[DllImportAttribute("X11")]
		static extern IntPtr XCreatePixmap(IntPtr disp, IntPtr rootWindow, UInt32 width, UInt32 height, UInt32 depth);
		[DllImportAttribute("X11")]
		static extern IntPtr XFreePixmap(IntPtr disp, IntPtr pixmap);
		[DllImportAttribute("X11")]
		static extern IntPtr XFree(IntPtr data);
		[DllImportAttribute("X11")]
		static extern Int32 XSelectInput(IntPtr disp, IntPtr win, EventMask eventMask);
		[DllImportAttribute("X11")]
		static extern Int32 XMapWindow(IntPtr disp, IntPtr win);
		[DllImportAttribute("X11")]
		static extern int XPending (IntPtr disp);
		[DllImportAttribute("X11")]
		static extern IntPtr XNextEvent(IntPtr disp, ref XEvent xevent);
		[DllImportAttribute("X11")]
		static extern Int32 XSync(IntPtr disp, int discard);
		[DllImportAttribute("X11")]
		static extern int XConnectionNumber(IntPtr disp);
		[DllImportAttribute("X11")]
		static extern IntPtr XSetErrorHandler(XErrorHandler error_handler);

		[DllImport ("libX11")]
		static extern void XDisplayKeycodes (IntPtr disp, out int min, out int max);
		[DllImport ("libX11")]
		static extern IntPtr XGetKeyboardMapping (IntPtr disp, byte first_keycode, int keycode_count, 
			out int keysyms_per_keycode_return);
		[DllImport ("libX11")]
		unsafe static extern byte* XGetModifierMapping (IntPtr disp);
		[DllImport ("libX11")]
		static extern uint XKeycodeToKeysym (IntPtr display, int keycode, int index);
		#endregion

		IntPtr xDisp, xwinHnd, xDefaultRootWin, xDefaultVisual;
		UInt32 xDefaultDepth;
		Int32 xScreen;
		XErrorHandler errorHnd;

		Interface iFace;

		#region IBackend implementation

		public void Init (Interface _iFace)
		{
			iFace = _iFace;
			XInitThreads ();
			xDisp = XOpenDisplay(IntPtr.Zero);
			if (xDisp == IntPtr.Zero)
				throw new NotSupportedException("[XLib] Failed to open display.");

			xScreen = XDefaultScreen(xDisp);

			xDefaultRootWin = XDefaultRootWindow (xDisp);
			xDefaultVisual = XDefaultVisual (xDisp, xScreen);
			xDefaultDepth = XDefaultDepth (xDisp, xScreen);

			xwinHnd = XCreateSimpleWindow (xDisp, xDefaultRootWin,
				0, 0, (uint)iFace.ClientRectangle.Width, (uint)iFace.ClientRectangle.Height, 0, IntPtr.Zero, IntPtr.Zero);
			if (xwinHnd == IntPtr.Zero)
				throw new NotSupportedException("[XLib] Failed to create window.");

			XSelectInput (xDisp, xwinHnd, EventMask.ExposureMask | 
				EventMask.KeyPressMask	| EventMask.KeyReleaseMask | 
				EventMask.PointerMotionMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask);

			XMapWindow (xDisp, xwinHnd);

			iFace.surf = new Cairo.XlibSurface (xDisp, xwinHnd, xDefaultVisual, iFace.ClientRectangle.Width, iFace.ClientRectangle.Height);

			errorHnd = new XErrorHandler (HandleError);
			XSetErrorHandler (errorHnd);

			int min_keycode, max_keycode, keysyms_per_keycode;

			XDisplayKeycodes (xDisp, out min_keycode, out max_keycode);
			IntPtr ksp = XGetKeyboardMapping (xDisp, (byte) min_keycode,
				max_keycode + 1 - min_keycode, out keysyms_per_keycode);
			XFree (ksp);

			unsafe {
				byte* modmap_unmanaged = XGetModifierMapping (xDisp);
				int nummodmap = 0;
				int* ptr = (int*)modmap_unmanaged;
				nummodmap = ptr [0];

				for (int i = 0; i< nummodmap; i++) {
					Console.WriteLine(modmap_unmanaged[i+4]);
				}
				XFree ((IntPtr)modmap_unmanaged);
			}
		}

		public void CleanUp ()
		{
			XCloseDisplay (xDisp);
		}
		public void Flush () {
			XSync (xDisp, 0);
		}
		public void ProcessEvents ()
		{
			if (XPending (xDisp) > 0) {
				XEvent xevent = new XEvent ();
				XNextEvent (xDisp, ref xevent);

				switch (xevent.type) {
				case XEventName.Expose:
					iFace.ProcessResize (new Rectangle (0, 0, xevent.ExposeEvent.width, xevent.ExposeEvent.height));
					break;
				case XEventName.KeyPress:
					iFace.ProcessKeyDown (xevent.KeyEvent.keycode);

					/*int min_keycode, max_keycode, keysyms_per_keycode;
						XLib.NativeMethods.XDisplayKeycodes (xDisp, out min_keycode, out max_keycode);

						IntPtr ksp = XLib.NativeMethods.XGetKeyboardMapping (xDisp, (byte)min_keycode,
							             max_keycode + 1 - min_keycode, out keysyms_per_keycode);
						XLib.NativeMethods.XFree (ksp);*/

					uint keySym;
					keySym = XKeycodeToKeysym (xDisp, xevent.KeyEvent.keycode, 0);
					char c = (char)keySym;

					Debug.WriteLine ("keycode:{0}; keysym:{1}; char:{2}", xevent.KeyEvent.keycode, keySym, c);
					break;
				case XEventName.KeyRelease:
					iFace.ProcessKeyUp (xevent.KeyEvent.keycode);
					//Debug.WriteLine ("keypress: {0}", xevent.KeyEvent.keycode);
					break;
				case XEventName.MotionNotify:
					//Debug.WriteLine ("motion: ({0},{1})", xevent.MotionEvent.x, xevent.MotionEvent.y);
					iFace.ProcessMouseMove (xevent.MotionEvent.x, xevent.MotionEvent.y);
					break;
				case XEventName.ButtonPress:
					//Debug.WriteLine ("button press: {0}", xevent.ButtonEvent.button);
					if (xevent.ButtonEvent.button == 4)
						iFace.ProcessMouseWheelChanged (Interface.WheelIncrement);
					else if(xevent.ButtonEvent.button == 5)
						iFace.ProcessMouseWheelChanged (-Interface.WheelIncrement);
					else
						iFace.ProcessMouseButtonDown (xevent.ButtonEvent.button - 1);
					break;
				case XEventName.ButtonRelease:
					//Debug.WriteLine ("button release: {0}", xevent.ButtonEvent.button);
					iFace.ProcessMouseButtonUp (xevent.ButtonEvent.button - 1);
					break;

				}
			}
		}

		#endregion

		int HandleError (IntPtr display, ref XErrorEvent error_event)
		{
			/*if (ErrorExceptions)
				throw new X11Exception (error_event.display, error_event.resourceid,
					error_event.serial, error_event.error_code,
					error_event.request_code, error_event.minor_code);
			else
				Console.WriteLine ("X11 Error encountered: {0}{1}\n",
					X11Exception.GetMessage(error_event.display, error_event.resourceid,
						error_event.serial, error_event.error_code,
						error_event.request_code, error_event.minor_code),
					WhereString());*/
			Debug.WriteLine ("XERROR {0}", error_event.error_code);
			return 0;
		}
	}
}


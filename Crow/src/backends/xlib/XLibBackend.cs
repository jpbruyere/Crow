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

namespace Crow.XLib
{
	public class XLibBackend : IBackend
	{
		#region pinvoke
		[DllImport("X11")]
		static extern int XInitThreads();
		[DllImport("X11")]
		internal static extern IntPtr XOpenDisplay(IntPtr displayName);
		[DllImport("X11")]
		internal static extern IntPtr XCloseDisplay(IntPtr disp);
		[DllImport("X11")]
		static extern Int32 XDefaultScreen(IntPtr disp);
		[DllImport("X11")]
		static extern IntPtr XDefaultRootWindow(IntPtr disp);
		[DllImport("X11")]
		static extern UInt32 XDefaultDepth (IntPtr disp, Int32 screen);
		[DllImport("X11")]
		static extern IntPtr XDefaultVisual(IntPtr disp, Int32 screen);
		[DllImport("X11")]
		static extern IntPtr XCreateSimpleWindow(IntPtr disp, IntPtr rootWindow, Int32 x, Int32 y, UInt32 width, UInt32 height,
			UInt32 borderWidth, IntPtr border, IntPtr background);
		[DllImport("X11")]
		static extern IntPtr XCreatePixmap(IntPtr disp, IntPtr rootWindow, UInt32 width, UInt32 height, UInt32 depth);
		[DllImport("X11")]
		static extern IntPtr XFreePixmap(IntPtr disp, IntPtr pixmap);
		[DllImport("X11")]
		static extern IntPtr XFree(IntPtr data);
		[DllImport("X11")]
		static extern Int32 XSelectInput(IntPtr disp, IntPtr win, EventMask eventMask);
		[DllImport("X11")]
		static extern Int32 XMapWindow(IntPtr disp, IntPtr win);
		[DllImport("X11")]
		static extern int XPending (IntPtr disp);
		[DllImport("X11")]
		static extern IntPtr XNextEvent(IntPtr disp, ref XEvent xevent);
		[DllImport("X11")]
		static extern Int32 XSync(IntPtr disp, int discard);
		[DllImport("X11")]
		static extern int XConnectionNumber(IntPtr disp);
		[DllImport("X11")]
		static extern IntPtr XSetErrorHandler(XErrorHandler error_handler);
		[DllImport ("X11")]
		static extern int XDefineCursor (IntPtr disp, IntPtr wnd, IntPtr cursor);

		[DllImport ("X11")]
		static extern uint XWarpPointer (IntPtr disp, IntPtr src_w, IntPtr dest_w, int src_x, int src_y, uint src_width, uint src_height, int dest_x, int dest_y);

		[DllImport ("libXcursor.so.1")]
		static extern int XcursorGetDefaultSize (IntPtr dpy);
		[DllImport ("libXcursor.so.1")]
		static extern string XcursorGetTheme (IntPtr dpy);
		[DllImport ("libXcursor.so.1")]
		static extern IntPtr XcursorLibraryLoadImage (string name, string theme, int size);
		[DllImport ("libXcursor.so.1")]
		static extern void XcursorImageDestroy (IntPtr image);

		[DllImport ("libXcursor.so.1")]
		static extern IntPtr XcursorImageLoadCursor (IntPtr dpy, IntPtr image);

		#endregion

		IntPtr xDisp, xwinHnd, xDefaultRootWin, xDefaultVisual;
		UInt32 xDefaultDepth;
		Int32 xScreen;
		XErrorHandler errorHnd;

		Interface iFace;
		X11Keyboard keyboard;

		IntPtr [] cursors = new IntPtr [(int)MouseCursors.MaxEnum];

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

			keyboard = new Crow.XLib.X11Keyboard (xDisp);

			iFace.surf = new Crow.Cairo.XlibSurface (xDisp, xwinHnd, xDefaultVisual, iFace.ClientRectangle.Width, iFace.ClientRectangle.Height);

			errorHnd = new XErrorHandler (HandleError);
			XSetErrorHandler (errorHnd);

			loadCursors ();
		}

		public void CleanUp ()
		{
			keyboard.Destroy ();

			XCloseDisplay (xDisp);
		}
		public void Flush () {
			XSync (xDisp, 1);
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
					keyboard.HandleEvent ((uint)xevent.KeyEvent.keycode, true);
					break;
				case XEventName.KeyRelease:
					keyboard.HandleEvent ((uint)xevent.KeyEvent.keycode, false);
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
						iFace.ProcessMouseButtonDown ((MouseButton)(xevent.ButtonEvent.button - 1));
					break;
				case XEventName.ButtonRelease:
					//Debug.WriteLine ("button release: {0}", xevent.ButtonEvent.button);
					iFace.ProcessMouseButtonUp ((MouseButton)(xevent.ButtonEvent.button - 1));
					break;

				}
			}
		}
		public bool IsDown (Key key) {
			return false;
		}
		public bool Shift {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool Ctrl {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool Alt {
			get {
				throw new NotImplementedException ();
			}
		}
		public void SetCursor (MouseCursors newCur)
		{
			XDefineCursor (xDisp, xwinHnd, cursors[(int)newCur]);
			XSync (xDisp, 0);
		}
		public void SetCursorPosition (int x, int y)
		{
			XWarpPointer (xDisp, IntPtr.Zero, xwinHnd, 0, 0, 0, 0, x, y);
		}
		#endregion

		void loadCursor (MouseCursors id, string name)
		{
			IntPtr img = XcursorLibraryLoadImage (name, null, XcursorGetDefaultSize (xDisp));
			cursors[(int)id] = XcursorImageLoadCursor (xDisp, img);
			XcursorImageDestroy (img);
		}

		void loadCursors ()
		{
			loadCursor (MouseCursors.Default, "default");
			loadCursor (MouseCursors.Cross, "cross");
			loadCursor (MouseCursors.Arrow, "arrow");
			loadCursor (MouseCursors.Text, "text");
			loadCursor (MouseCursors.SW, "bottom_left_corner");
			loadCursor (MouseCursors.SE, "bottom_right_corner");
			loadCursor (MouseCursors.NW, "top_left_corner");
			loadCursor (MouseCursors.NE, "top_right_corner");
			loadCursor (MouseCursors.N, "top_side");
			loadCursor (MouseCursors.S, "bottom_side");
			loadCursor (MouseCursors.V, "size_ver");
			loadCursor (MouseCursors.H, "size_hor");
		}

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


using System;
using System.Runtime.InteropServices;
using Crow;
using Mono.Unix.Native;
using System.Diagnostics;
using System.Threading;

namespace testsCairoXLib
{
	class MainClass
	{
		static internal IntPtr xHandle, xwinHnd;
		static internal Int32 xScreen;
		static IntPtr xLastEvent;
		static XLib.XErrorHandler errorHnd;

		static Pollfd[] pollfds;

		static Cairo.Surface surf;

		static void init() {
			XLib.NativeMethods.XInitThreads ();
			xHandle = XLib.NativeMethods.XOpenDisplay(IntPtr.Zero);
			if (xHandle == IntPtr.Zero)
				throw new NotSupportedException("[XLib] Failed to open display.");

			xScreen = XLib.NativeMethods.XDefaultScreen(xHandle);
			xLastEvent = Marshal.AllocHGlobal(96);

			int x = 0, y = 0;
			int width = 800, height = 600;

			xwinHnd = XLib.NativeMethods.XCreateSimpleWindow (xHandle, XLib.NativeMethods.XDefaultRootWindow(xHandle),
				x, y, (uint)width, (uint)height, 0, IntPtr.Zero, IntPtr.Zero);
			if (xwinHnd == IntPtr.Zero)
				throw new NotSupportedException("[XLib] Failed to create window.");

			XLib.NativeMethods.XSelectInput (xHandle, xwinHnd, XLib.EventMask.ExposureMask | XLib.EventMask.KeyPressMask
				| XLib.EventMask.PointerMotionMask | XLib.EventMask.ButtonPressMask | XLib.EventMask.ButtonReleaseMask);

			XLib.NativeMethods.XMapWindow (xHandle, xwinHnd);

			surf = new Cairo.XlibSurface (xHandle, xwinHnd, XLib.NativeMethods.XDefaultVisual (xHandle, xScreen), (int)width, (int)height);

			pollfds = new Pollfd [1];
			pollfds [0] = new Pollfd ();
			pollfds [0].fd = XLib.NativeMethods.XConnectionNumber (xHandle);
			pollfds [0].events = PollEvents.POLLIN;

			errorHnd = new XLib.XErrorHandler (HandleError);
			XLib.NativeMethods.XSetErrorHandler (errorHnd);
		}
		static int HandleError (IntPtr display, ref XLib.XErrorEvent error_event)
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

		static Stopwatch timer;

		static Point mousePos;

		static void draw() {
			using (Cairo.Context ctx = new Cairo.Context (surf)) {
				ctx.SetSourceRGBA (1, 0, 0, 1);
				ctx.Paint ();
				ctx.MoveTo (100, 100);
				ctx.SetSourceRGBA (1, 1, 1, 1);
				ctx.SetFontSize (20);
				ctx.ShowText (string.Format("{0,10}",timer.ElapsedMilliseconds.ToString()));
				ctx.MoveTo (mousePos + new Point(10,0));
				ctx.Arc (mousePos.X, mousePos.Y, 10, 0, Math.PI * 2.0);
				ctx.Stroke ();
			}
			XLib.NativeMethods.XSync (xHandle, 0);
		}

		static void interfaceThread()
		{
			timer = Stopwatch.StartNew ();

			while (true) {
				//pollfds [0].fd = XLib.NativeMethods.XConnectionNumber (xHandle);
				//Syscall.poll (pollfds, 1U, 0);

				draw ();

				if (XLib.NativeMethods.XPending (xHandle) > 0) {
					XLib.XEvent xevent = new XLib.XEvent ();
					XLib.NativeMethods.XNextEvent (xHandle, ref xevent);

					switch (xevent.type) {
					case XLib.XEventName.KeyPress:
						Debug.WriteLine ("keypress: {0}", xevent.KeyEvent.keycode);
						break;
					case XLib.XEventName.MotionNotify:
						Debug.WriteLine ("motion: ({0},{1})", xevent.MotionEvent.x, xevent.MotionEvent.y);
						mousePos = new Point(xevent.MotionEvent.x, xevent.MotionEvent.y);
						break;
					case XLib.XEventName.ButtonPress:
						Debug.WriteLine ("button press: {0}", xevent.ButtonEvent.button);
						break;
					case XLib.XEventName.ButtonRelease:
						Debug.WriteLine ("button release: {0}", xevent.ButtonEvent.button);
						break;

					}
				}
			}
		}

		public static void Main(string[] args)
		{
			/*using (Interface app = new Interface ()) {
								XWindow win = new XWindow (app);
								win.Show ();
				app.LoadIMLFragment (@"<SimpleGauge Level='40' Margin='5' Background='Jet' Foreground='Grey' Width='30' Height='50%'/>");
				app.AddWidget(@"Interfaces/Divers/0.crow");
				System.Threading.Thread.Sleep (10000);
			}*/

			init ();

			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();

			while (true)
				continue;
		}

	}
}

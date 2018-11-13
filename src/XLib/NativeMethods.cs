using System;
using System.Runtime.InteropServices;

namespace XLib
{
    public static class NativeMethods
    {
		#region pinvoke
		[DllImportAttribute("X11")]
		public static extern int XInitThreads();
		[DllImportAttribute("X11")]
		public static extern IntPtr XOpenDisplay(IntPtr displayName);
		[DllImportAttribute("X11")]
		public static extern IntPtr XCloseDisplay(IntPtr disp);
		[DllImportAttribute("X11")]
		public static extern Int32 XDefaultScreen(IntPtr disp);
		[DllImportAttribute("X11")]
		public static extern IntPtr XDefaultRootWindow(IntPtr disp);
		[DllImportAttribute("X11")]
		public static extern UInt32 XDefaultDepth (IntPtr disp, Int32 screen);
		[DllImportAttribute("X11")]
		public static extern IntPtr XDefaultVisual(IntPtr disp, Int32 screen);
		[DllImportAttribute("X11")]
		public static extern IntPtr XCreateSimpleWindow(IntPtr disp, IntPtr rootWindow, Int32 x, Int32 y, UInt32 width, UInt32 height,
								  UInt32 borderWidth, IntPtr border, IntPtr background);
		[DllImportAttribute("X11")]
		public static extern IntPtr XCreatePixmap(IntPtr disp, IntPtr rootWindow, UInt32 width, UInt32 height, UInt32 depth);
		[DllImportAttribute("X11")]
		public static extern IntPtr XFreePixmap(IntPtr disp, IntPtr pixmap);
		[DllImportAttribute("X11")]
		public static extern Int32 XSelectInput(IntPtr disp, IntPtr win, EventMask eventMask);
		[DllImportAttribute("X11")]
		public static extern Int32 XMapWindow(IntPtr disp, IntPtr win);
		[DllImportAttribute("X11")]
		public static extern int XPending (IntPtr disp);
		[DllImportAttribute("X11")]
		public static extern IntPtr XNextEvent(IntPtr disp, ref XEvent xevent);
		[DllImportAttribute("X11")]
		public static extern Int32 XSync(IntPtr disp, int discard);
		[DllImportAttribute("X11")]
		public static extern int XConnectionNumber(IntPtr disp);
		[DllImportAttribute("X11")]
		public static extern IntPtr XSetErrorHandler(XErrorHandler error_handler);
		#endregion


	}
}

using System;
using System.Runtime.InteropServices;

namespace XLib
{
    
    public enum EventType {
        KeyPress = 2,
        KeyRelease,
        ButtonPress,
        ButtonRelease,
        MotionNotify,
        EnterNotify,
        LeaveNotify,
        FocusIn,
        FocusOut,
        KeymapNotify,
        Expose,
        GraphicsExpose,
        NoExpose,
        VisibilityNotify,
        CreateNotify,
        DestroyNotify,
        UnmapNotify,
        MapNotify,
        MapRequest,
        ReparentNotify,
        ConfigureNotify,
        ConfigureRequest,
        GravityNotify,
        ResizeRequest,
        CirculateNotify,
        CirculateRequest,
        PropertyNotify,
        SelectionClear,
        SelectionRequest,
        SelectionNotify,
        ColormapNotify,
        ClientMessage,
        MappingNotify,
        GenericEvent,
        LASTEvent = 36  /* must be bigger than any event # */
	}
    public class Window 
    {
        IntPtr handle;
        Display disp;

		public Window (Display display, UInt32 width = 800, UInt32 height = 600, Int32 x = 0, Int32 y = 0) {
            disp = display;
            handle = NativeMethods.XCreateSimpleWindow (disp.handle, NativeMethods.XDefaultRootWindow(disp.handle), x, y, width, height,
														   0, IntPtr.Zero, IntPtr.Zero);
			if (handle == IntPtr.Zero)
				throw new NotSupportedException("[XLib] Failed to create window.");

            NativeMethods.XSelectInput (disp.handle, handle, EventMask.ExposureMask | EventMask.KeyPressMask);

            NativeMethods.XMapWindow (disp.handle, handle);
		}

    }
}

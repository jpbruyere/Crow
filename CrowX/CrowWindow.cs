//
// CrowWindow.cs
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
using System.Threading;
using System.Collections.Generic;
using X11Sharp;
using System.Runtime.InteropServices;

namespace Crow
{
	public class CrowWindow : IValueChange, IDisposable
    {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			if (ValueChanged != null)
				ValueChanged.Invoke(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		public IntPtr handle, rootWindow, display;
		public int screen;
		XVisualInfo visualInfo;
		public EventMask eventMask;

		#region ctor
		public CrowWindow(int _width = 800, int _height = 600)			
		{			
			IntPtr test = API.DefaultDisplay;
			display = Functions.XOpenDisplay(IntPtr.Zero);
			using (new XLock(display))
			{
				XSetWindowAttributes attr = new XSetWindowAttributes();

				screen = Functions.XDefaultScreen(display);
				rootWindow = Functions.XRootWindow(display, screen);
				handle = Functions.XCreateWindow(display, rootWindow,
						0, 0, _width, _height, 0, 0, CreateWindowArgs.CopyFromParent, IntPtr.Zero,
						SetWindowValuemask.Nothing, attr);
			Functions.XSelectInput (display, handle, new IntPtr((int)(EventMask.ExposureMask | EventMask.KeyPressMask)));
			}

//				ProcessingThread = new Thread(ProcessEvents);
//				ProcessingThread.IsBackground = true;
//				ProcessingThread.Start();
		}

		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			Functions.XCloseDisplay(display);
		}
		#endregion
    }
}

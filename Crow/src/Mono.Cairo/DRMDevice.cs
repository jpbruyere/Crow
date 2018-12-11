﻿//
// Mono.Cairo.Device.cs
//
// Authors:
//			JP Bruyère (jp_bruyere@hotmail.com)
//
// This is an OO wrapper API for the Cairo API
//
// Copyright (C) 2016 JP Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;

namespace Cairo
{
	public class DRMDevice : Device
	{
		public DRMDevice () : base (NativeMethods.cairo_drm_device_default (), true)
		{
		}
		public DRMDevice (int fd) : base (NativeMethods.cairo_drm_device_get_for_fd (fd), true)
		{
		}
		public DRMDevice (IntPtr udev_device) : base (NativeMethods.cairo_drm_device_get (udev_device), true)
		{
		}

		public int FileDescriptor {
			get { return NativeMethods.cairo_drm_device_get_fd (Handle); }
		}

		public void DeviceThrottle () { NativeMethods.cairo_drm_device_throttle (Handle);}
	}
}


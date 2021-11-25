// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Drawing2D;

namespace Crow.CairoBackend
{
	public abstract class GLDevice : CairoDevice
	{
		protected GLDevice (IntPtr handle, bool owner = true) : base (handle, owner) {}
		public void SetThreadAware (bool value) {
			NativeMethods.cairo_gl_device_set_thread_aware (handle, value ? 1 : 0);
		}

	}
}


// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.SkiaBackend;

namespace Crow.Backends
{
	public class DefaultBackend : VulkanBackend
	{
		public DefaultBackend (ref IntPtr nativeWindoPointer, out bool ownGlfwWinHandle, int width, int height)
		: base (ref nativeWindoPointer, out ownGlfwWinHandle, width, height) {}
		public DefaultBackend (int width, int height)
		: base (width, height) {}
	}
}
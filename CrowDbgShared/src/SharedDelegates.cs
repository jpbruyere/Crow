// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Glfw;

namespace CrowDbgShared
{
	public delegate void InterfaceResizeDelegate(int a, int b);
	public delegate bool InterfaceMouseMoveDelegate(int a, int b);
	public delegate bool InterfaceMouseButtonDelegate(MouseButton button);
	public delegate void VoidDelegate();
	public delegate IntPtr IntPtrGetterDelegate();
}


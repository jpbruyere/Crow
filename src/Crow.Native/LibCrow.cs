//
// LibCrow.cs
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
using System.Runtime.InteropServices;

namespace Crow.Native
{
	internal static class LibCrow
	{
		#region PINVOKE
		const string lib = "libcrow";
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr crow_context_create ();
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void crow_context_destroy (IntPtr ctx);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void crow_context_set_root (IntPtr ctx, crow_object_t* root);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void crow_context_process_layouting (IntPtr ctx);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void crow_context_process_clipping (IntPtr ctx);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void crow_context_process_drawing (IntPtr ctx, IntPtr cairoCtx);

		[DllImport(lib)]
		unsafe internal static extern crow_object_t* crow_object_create();
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void crow_object_destroy(crow_object_t* go);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void crow_object_set_type (crow_object_t* go, CrowType objType);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern byte crow_object_do_layout (crow_object_t* go, LayoutingType layout);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern byte crow_object_register_layouting (crow_object_t* go, LayoutingType layout);


		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void crow_object_child_add (crow_object_t* parent, crow_object_t* child);
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		unsafe internal static extern void crow_object_child_remove (crow_object_t* parent, crow_object_t* child);
		#endregion		
	}
}


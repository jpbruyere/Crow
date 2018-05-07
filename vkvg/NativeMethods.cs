//
// NativeMethods.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2018 jp
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

namespace vkvg
{
	internal static class NativeMethods
	{
		const string libvkvg = "vkvg";

		#region Device
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_device_create (IntPtr phy, IntPtr dev, uint qFamIdx, uint qIndex);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_device_destroy (IntPtr device);
		#endregion

		#region Context
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_create (IntPtr surface);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_destroy (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_flush (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_close_path (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_line_to (IntPtr ctx, float x, float y);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_rel_line_to (IntPtr ctx, float x, float y);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_move_to (IntPtr ctx, float x, float y);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_rel_move_to (IntPtr ctx, float x, float y);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_curve_to (IntPtr ctx, float x1, float y1, float x2, float y2, float x3, float y3);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_rel_curve_to (IntPtr ctx, float x1, float y1, float x2, float y2, float x3, float y3);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_rectangle (IntPtr ctx, float x, float y, float width, float height);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_arc (IntPtr ctx, float xc, float yc, float radius, float a1, float a2);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_arc_negative (IntPtr ctx, float xc, float yc, float radius, float a1, float a2);

		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_scale (IntPtr ctx, float sx, float sy);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_translate (IntPtr ctx, float dx, float dy);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_rotate (IntPtr ctx, float alpha);

		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_stroke (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_stroke_preserve (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_clip (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_clip_preserve (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_reset_clip (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_fill (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_fill_preserve (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_paint (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_set_source_rgba (IntPtr ctx, float r, float g, float b, float a);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_set_line_width (IntPtr ctx, float width);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_set_operator (IntPtr ctx, Operator op);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Operator vkvg_get_operator (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_set_source_surface(IntPtr ctx, IntPtr surf, float x, float y);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_font_extents (IntPtr ctx, out FontExtents extents);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_text_extents (IntPtr ctx, byte[] utf8, out TextExtents extents);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_select_font_face (IntPtr ctx, string name);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_set_font_size (IntPtr ctx, uint size);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_show_text (IntPtr ctx, string text);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_save (IntPtr ctx);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkvg_restore (IntPtr ctx);
		#endregion

		#region Surface
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_create (IntPtr device, uint width, uint height);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_create_from_image  (IntPtr dev, string filePath);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_create_from_bitmap  (IntPtr dev, ref byte[] data, uint width, uint height);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void	vkvg_surface_destroy (IntPtr surf);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_get_vk_image	(IntPtr surf);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int vkvg_surface_get_width (IntPtr surf);
		[DllImport (libvkvg, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int vkvg_surface_get_height (IntPtr surf);
		#endregion
	}
}


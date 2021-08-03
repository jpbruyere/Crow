// Copyright (c) 2018-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Runtime.InteropServices;

namespace Crow.Drawing
{
	internal static class NativeMethods
	{
		const string libvkvg = "vkvg";

		#region Device
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_device_create(IntPtr instance, IntPtr phy, IntPtr dev, uint qFamIdx, uint qIndex);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_device_destroy(IntPtr device);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_device_create_multisample(IntPtr inst, IntPtr phy, IntPtr vkdev, uint qFamIdx, uint qIndex, SampleCount samples, bool deferredResolve);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_device_reference(IntPtr dev);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint vkvg_device_get_reference_count(IntPtr dev);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_device_set_dpy(IntPtr dev, int hdpy, int vdpy);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_device_get_dpy(IntPtr dev, out int hdpy, out int vdpy);
		#endregion

		#region Context
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_create(IntPtr surface);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_destroy(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_flush(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_new_path(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_new_sub_path(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_close_path(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_line_to(IntPtr ctx, float x, float y);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_rel_line_to(IntPtr ctx, float x, float y);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_move_to(IntPtr ctx, float x, float y);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_rel_move_to(IntPtr ctx, float x, float y);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_arc(IntPtr ctx, float xc, float yc, float radius, float a1, float a2);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_arc_negative(IntPtr ctx, float xc, float yc, float radius, float a1, float a2);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_curve_to(IntPtr ctx, float x1, float y1, float x2, float y2, float x3, float y3);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_rel_curve_to(IntPtr ctx, float x1, float y1, float x2, float y2, float x3, float y3);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_rectangle(IntPtr ctx, float x, float y, float width, float height);

		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_scale(IntPtr ctx, float sx, float sy);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_translate(IntPtr ctx, float dx, float dy);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_rotate(IntPtr ctx, float alpha);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_transform(IntPtr ctx, ref Matrix matrix);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_matrix(IntPtr ctx, ref Matrix matrix);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_get_matrix(IntPtr ctx, out Matrix matrix);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_identity_matrix(IntPtr ctx);

		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_stroke(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_stroke_preserve(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_clip(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_clip_preserve(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_reset_clip(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_fill(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_fill_preserve(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_paint(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_source_rgba(IntPtr ctx, float r, float g, float b, float a);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_line_width(IntPtr ctx, float width);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_line_cap(IntPtr ctx, LineCap cap);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_line_join(IntPtr ctx, LineJoin join);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_operator(IntPtr ctx, Operator op);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern FillRule vkvg_get_fill_rule(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_fill_rule(IntPtr ctx, FillRule fr);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern Operator vkvg_get_operator(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_source_surface(IntPtr ctx, IntPtr surf, float x, float y);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_source(IntPtr ctx, IntPtr pattern);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_font_extents(IntPtr ctx, out FontExtents extents);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_text_extents(IntPtr ctx, byte[] utf8, out TextExtents extents);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_text_extents(IntPtr cr, ref byte utf8, out TextExtents extents);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_select_font_face(IntPtr ctx, string name);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_font_size(IntPtr ctx, uint size);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_show_text(IntPtr ctx, byte [] utf8);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_show_text(IntPtr cr, ref byte utf8);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_save(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_restore(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_clear(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern float vkvg_get_line_width(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern LineCap vkvg_get_line_cap(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern LineJoin vkvg_get_line_join(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_get_source(IntPtr ctx);

		[DllImport (libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_set_dash (IntPtr ctx, float[] dashes, uint dashCount, float offset);

		//void vkvg_set_dash (VkvgContext ctx, const float* dashes, uint32_t num_dashes, float offset);
		//void vkvg_get_dash (VkvgContext ctx, const float* dashes, uint32_t* num_dashes, float* offset

	   [DllImport (libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_reference(IntPtr ctx);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint vkvg_get_reference_count(IntPtr ctx);
		#endregion

		#region TextRun
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_text_run_create(IntPtr ctx, byte[] utf8);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_text_run_destroy(IntPtr textRun);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_show_text_run(IntPtr ctx, IntPtr textRun);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_text_run_get_extents(IntPtr textRun, out TextExtents extents);
		#endregion

		#region Pattern
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_pattern_create();
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_pattern_reference(IntPtr pat);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint vkvg_pattern_get_reference_count(IntPtr pat);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_pattern_create_rgba(float r, float g, float b, float a);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_pattern_create_rgb(float r, float g, float b);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_pattern_create_for_surface(IntPtr surf);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_pattern_create_linear(float x0, float y0, float x1, float y1);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_pattern_create_radial(float cx0, float cy0, float radius0,
											 float cx1, float cy1, float radius1);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_pattern_destroy(IntPtr pat);

		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_pattern_add_color_stop(IntPtr pat, float offset, float r, float g, float b, float a);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_pattern_set_extend(IntPtr pat, Extend extend);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_pattern_set_filter(IntPtr pat, Filter filter);

		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern Extend vkvg_pattern_get_extend(IntPtr pat);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern Filter vkvg_pattern_get_filter(IntPtr pat);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern PatternType vkvg_pattern_get_type(IntPtr pat);
		#endregion

		#region Matrices
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_init_identity(out Matrix matrix);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_init(out Matrix matrix,
		   float xx, float yx,
		   float xy, float yy,
		   float x0, float y0);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_init_translate(out Matrix matrix, float tx, float ty);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_init_scale(out Matrix matrix, float sx, float sy);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_init_rotate(out Matrix matrix, float radians);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_translate(ref Matrix matrix, float tx, float ty);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_scale(ref Matrix matrix, float sx, float sy);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_rotate(ref Matrix matrix, float radians);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_multiply(out Matrix result, ref Matrix a, ref Matrix b);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_transform_distance(ref Matrix matrix, ref float dx, ref float dy);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_transform_point(ref Matrix matrix, ref float x, ref float y);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_matrix_invert(ref Matrix matrix);
		#endregion

		#region Surface
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_create(IntPtr device, uint width, uint height);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_create_from_image(IntPtr dev, string filePath);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_create_from_svg(IntPtr dev, string filePath);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_create_from_svg_fragment(IntPtr dev, byte[] filePath);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_create_from_bitmap(IntPtr dev, ref byte data, uint width, uint height);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_surface_destroy(IntPtr surf);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_get_vk_image(IntPtr surf);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int vkvg_surface_get_width(IntPtr surf);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int vkvg_surface_get_height(IntPtr surf);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_surface_clear(IntPtr surf);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr vkvg_surface_reference(IntPtr surf);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint vkvg_surface_get_reference_count(IntPtr surf);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_surface_write_to_png(IntPtr surf, [MarshalAs(UnmanagedType.LPStr)]string path);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_surface_write_to_memory(IntPtr surf, IntPtr pBitmap);
		#endregion

		#region NSVG
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr nsvg_load_file(IntPtr dev, string filePath);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr nsvg_load(IntPtr dev, ref byte fragment);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void nsvg_destroy(IntPtr nsvgImage);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void nsvg_get_size(IntPtr nsvgImage, out int width, out int height);
		[DllImport(libvkvg, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void vkvg_render_svg(IntPtr ctx, IntPtr nsvgImage, string subId);
		#endregion
	}
}


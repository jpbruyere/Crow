﻿//
// Cairo.cs - a simplistic binding of the Cairo API to C#.
//
// Authors: Duncan Mak (duncan@ximian.com)
//          Hisham Mardam Bey (hisham.mardambey@gmail.com)
//          John Luke (john.luke@gmail.com)
//          Alp Toker (alp@atoker.com)
//
// (C) Ximian, Inc. 2003
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// Copyright (C) 2005 John Luke
// Copyright (C) 2006 Alp Toker
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
using System.Runtime.InteropServices;
using Drawing2D;

namespace Crow.CairoBackend
{
	// sort the functions like in the following page so it is easier to find what is missing
	// http://cairographics.org/manual/index-all.html

	internal static class NativeMethods
	{
#if MONOTOUCH
		const string cairo = "__Internal";
#else
    	const string cairo = "cairo";
#endif

        //[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
        //internal static extern void cairo_append_path (IntPtr cr, Path path);

        [DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_arc (IntPtr cr, double xc, double yc, double radius, double angle1, double angle2);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_arc_negative (IntPtr cr, double xc, double yc, double radius, double angle1, double angle2);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_atsui_font_face_create_for_atsu_font_id (IntPtr font_id);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_clip (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_clip_preserve (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_clip_extents (IntPtr cr, out double x1, out double y1, out double x2, out double y2);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_close_path (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_copy_page (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_copy_path (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_copy_path_flat (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_append_path (IntPtr cr, IntPtr path);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_create (IntPtr target);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_curve_to (IntPtr cr, double x1, double y1, double x2, double y2, double x3, double y3);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_debug_reset_static_data ();

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_destroy (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_device_to_user (IntPtr cr, ref double x, ref double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_device_to_user_distance (IntPtr cr, ref double dx, ref double dy);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_fill (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_fill_extents (IntPtr cr, out double x1, out double y1, out double x2, out double y2);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_fill_preserve (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_font_extents (IntPtr cr, out FontExtents extents);

		#region FontFace
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_font_face_destroy (IntPtr font_face);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern FontType cairo_font_face_get_type (IntPtr font_face);

		//[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern void cairo_font_face_get_user_data (IntPtr font_face);

		//[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern void cairo_font_face_set_user_data (IntPtr font_face);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_font_face_reference (IntPtr font_face);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_font_face_status (IntPtr font_face);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern uint cairo_font_face_get_reference_count (IntPtr surface);
		#endregion

		#region FontOptions
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_font_options_copy (IntPtr original);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_font_options_create ();

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_font_options_destroy (IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.U1)]
		internal static extern bool cairo_font_options_equal (IntPtr options, IntPtr other);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Antialias cairo_font_options_get_antialias (IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern HintMetrics cairo_font_options_get_hint_metrics (IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern HintStyle cairo_font_options_get_hint_style (IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern SubpixelOrder cairo_font_options_get_subpixel_order (IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern long cairo_font_options_hash (IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_font_options_merge (IntPtr options, IntPtr other);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_font_options_set_antialias (IntPtr options, Antialias aa);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_font_options_set_hint_metrics (IntPtr options, HintMetrics metrics);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_font_options_set_hint_style (IntPtr options, HintStyle style);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_font_options_set_subpixel_order (IntPtr options, SubpixelOrder order);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_font_options_status (IntPtr options);
		#endregion

		#region Freetype / FontConfig
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_ft_font_face_create_for_ft_face (IntPtr face, int load_flags);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_ft_font_face_create_for_pattern (IntPtr fc_pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_ft_font_options_substitute (FontOptions options, IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_ft_scaled_font_lock_face (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_ft_scaled_font_unlock_face (IntPtr scaled_font);
		#endregion

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Antialias cairo_get_antialias (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_get_current_point (IntPtr cr, out double x, out double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern FillRule cairo_get_fill_rule (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_get_font_face (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_get_font_matrix (IntPtr cr, out Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_get_font_options (IntPtr cr, IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_get_group_target (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern LineCap cairo_get_line_cap (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern LineJoin cairo_get_line_join (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern double cairo_get_line_width (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_get_matrix (IntPtr cr, out Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern double cairo_get_miter_limit (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Operator cairo_get_operator (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern uint cairo_get_reference_count (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_get_source (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_get_target (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern double cairo_get_tolerance (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_glitz_surface_create (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_glyph_extents (IntPtr cr, IntPtr glyphs, int num_glyphs, out TextExtents extents);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_glyph_path (IntPtr cr, IntPtr glyphs, int num_glyphs);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.U1)]
		internal static extern bool cairo_has_current_point (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_identity_matrix (IntPtr cr);

		#region Image Surface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_image_surface_create (Format format, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_image_surface_create_for_data (byte[] data, Format format, int width, int height, int stride);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_image_surface_create_for_data (IntPtr data, Format format, int width, int height, int stride);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_image_surface_create_from_png  (string filename);

		//[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern IntPtr cairo_image_surface_create_from_png_stream  (string filename);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_image_surface_get_data (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Format cairo_image_surface_get_format (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_image_surface_get_height (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_image_surface_get_stride (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_image_surface_get_width  (IntPtr surface);
		#endregion

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.U1)]
		internal static extern bool cairo_in_clip (IntPtr cr, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.U1)]
		internal static extern bool cairo_in_fill (IntPtr cr, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.U1)]
		internal static extern bool cairo_in_stroke (IntPtr cr, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_line_to (IntPtr cr, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mask (IntPtr cr, IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mask_surface (IntPtr cr, IntPtr surface, double x, double y);

		#region Matrix
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_init (Matrix matrix, double xx, double yx, double xy, double yy, double x0, double y0);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_init_identity (Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_init_rotate (Matrix matrix, double radians);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_init_scale (Matrix matrix, double sx, double sy);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_init_translate (Matrix matrix, double tx, double ty);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_matrix_invert (Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_multiply (Matrix result, Matrix a, Matrix b);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_scale (Matrix matrix, double sx, double sy);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_rotate (Matrix matrix, double radians);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_transform_distance (Matrix matrix, ref double dx, ref double dy);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_transform_point (Matrix matrix, ref double x, ref double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_matrix_translate (Matrix matrix, double tx, double ty);
		#endregion

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_move_to (IntPtr cr, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_new_path (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_new_sub_path (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_paint (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_paint_with_alpha (IntPtr cr, double alpha);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_path_destroy (IntPtr path);

		#region Pattern
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pattern_add_color_stop_rgb (IntPtr pattern, double offset, double red, double green, double blue);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pattern_add_color_stop_rgba (IntPtr pattern, double offset, double red, double green, double blue, double alpha);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_pattern_get_color_stop_count (IntPtr pattern, out int count);
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_pattern_get_color_stop_rgba (IntPtr pattern, int index, out double offset, out double red, out double green, out double blue, out double alpha);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pattern_create_for_surface (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_pattern_get_surface (IntPtr pattern, out IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pattern_create_linear (double x0, double y0, double x1, double y1);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_pattern_get_linear_points (IntPtr pattern, out double x0, out double y0, out double x1, out double y1);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pattern_create_radial (double cx0, double cy0, double radius0, double cx1, double cy1, double radius1);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_pattern_get_radial_circles (IntPtr pattern, out double x0, out double y0, out double r0, out double x1, out double y1, out double r1);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pattern_create_rgb (double r, double g, double b);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pattern_create_rgba (double r, double g, double b, double a);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_pattern_get_rgba (IntPtr pattern, out double red, out double green, out double blue, out double alpha);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pattern_destroy (IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Extend cairo_pattern_get_extend (IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Filter cairo_pattern_get_filter (IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pattern_get_matrix (IntPtr pattern, Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern PatternType cairo_pattern_get_type (IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pattern_reference (IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pattern_set_extend (IntPtr pattern, Extend extend);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pattern_set_filter (IntPtr pattern, Filter filter);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pattern_set_matrix (IntPtr pattern, Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_pattern_status (IntPtr pattern);

		//mesh pattern
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pattern_create_mesh ();

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mesh_pattern_begin_patch (IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mesh_pattern_end_patch (IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mesh_pattern_move_to (IntPtr pattern, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mesh_pattern_line_to (IntPtr pattern, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mesh_pattern_curve_to (IntPtr pattern, double x1, double y1,
			double x2, double y2, double x3, double y3);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mesh_pattern_set_control_point (IntPtr pattern, uint point_num, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mesh_pattern_set_corner_color_rgb (IntPtr pattern, uint corner_num,
			double r, double g, double b);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_mesh_pattern_set_corner_color_rgba (IntPtr pattern, uint corner_num,
			double r, double g, double b, double a);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_mesh_pattern_get_patch_count (IntPtr pattern, out uint count);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_mesh_pattern_get_path (IntPtr pattern, uint patch_num);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_mesh_pattern_get_control_point (IntPtr pattern,
			uint patch_num, uint point_num, out double x, out double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_mesh_pattern_get_corner_color_rgba (IntPtr pattern,
			uint patch_num, uint point_num, out double r, out double g, out double b, out double a);
		#endregion

		#region PdfSurface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pdf_surface_create (string filename, double width, double height);

		//[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern IntPtr cairo_pdf_surface_create_for_stream (string filename, double width, double height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pdf_surface_set_size (IntPtr surface, double x, double y);
		#endregion

		#region PostscriptSurface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_ps_surface_create (string filename, double width, double height);

		//[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern IntPtr cairo_ps_surface_create_for_stream (string filename, double width, double height);
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_ps_surface_dsc_begin_page_setup (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_ps_surface_dsc_begin_setup (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_ps_surface_dsc_comment (IntPtr surface, string comment);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_ps_surface_set_size (IntPtr surface, double x, double y);
		#endregion

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_pop_group (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_pop_group_to_source (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_push_group (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_push_group_with_content (IntPtr cr, Content content);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_quartz_surface_create (IntPtr context, bool flipped, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_rectangle (IntPtr cr, double x, double y, double width, double height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_reference (IntPtr cr);

		#region Regions
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern bool cairo_region_contains_point (IntPtr region, int x, int y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern RegionOverlap cairo_region_contains_rectangle (IntPtr region, ref Rectangle rectangle);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_region_copy (IntPtr original);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_region_create ();

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_region_create_rectangle (ref Rectangle rect);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_region_create_rectangles (IntPtr rects, int count);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_region_destroy (IntPtr region);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern bool cairo_region_equal (IntPtr a, IntPtr b);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_region_get_extents (IntPtr region, out Rectangle extents);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_region_get_rectangle (IntPtr region, int nth, out Rectangle rectangle);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_intersect (IntPtr dst, IntPtr other);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_intersect_rectangle (IntPtr dst, ref Rectangle rectangle);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern bool cairo_region_is_empty (IntPtr region);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_region_num_rectangles (IntPtr region);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_region_reference (IntPtr region);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_status (IntPtr region);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_subtract (IntPtr dst, IntPtr other);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_subtract_rectangle (IntPtr dst, ref Rectangle rectangle);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_region_translate (IntPtr region, int dx, int dy);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_union (IntPtr dst, IntPtr other);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_union_rectangle (IntPtr dst, ref Rectangle rectangle);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_xor (IntPtr dst, IntPtr other);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_region_xor_rectangle (IntPtr dst, ref Rectangle rectangle);
		#endregion

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_rel_curve_to (IntPtr cr, double dx1, double dy1, double dx2, double dy2, double dx3, double dy3);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_rel_line_to (IntPtr cr, double dx, double dy);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_rel_move_to (IntPtr cr, double dx, double dy);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_reset_clip (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_restore (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_rotate (IntPtr cr, double angle);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_save (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_scale (IntPtr cr, double sx, double sy);

		#region ScaledFont
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_scaled_font_create (IntPtr fontFace, Matrix matrix, Matrix ctm, IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_scaled_font_destroy (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_scaled_font_extents (IntPtr scaled_font, out FontExtents extents);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_scaled_font_get_ctm (IntPtr scaled_font, out Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_scaled_font_get_font_face (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_scaled_font_get_font_matrix (IntPtr scaled_font, out Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_scaled_font_get_font_options (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern FontType cairo_scaled_font_get_type (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_scaled_font_glyph_extents (IntPtr scaled_font, IntPtr glyphs, int num_glyphs, out TextExtents extents);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_scaled_font_reference (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_scaled_font_status (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_scaled_font (IntPtr cr, IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_get_scaled_font (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_scaled_font_text_extents (IntPtr scaled_font, byte[] utf8, out TextExtents extents);
		#endregion

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_select_font_face (IntPtr cr, string family, FontSlant slant, FontWeight weight);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_antialias (IntPtr cr, Antialias antialias);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_dash (IntPtr cr, double [] dashes, int ndash, double offset);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_get_dash (IntPtr cr, IntPtr dashes, out double offset);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_get_dash_count (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_fill_rule (IntPtr cr, FillRule fill_rule);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_font_face (IntPtr cr, IntPtr fontFace);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_font_matrix (IntPtr cr, Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_font_options (IntPtr cr, IntPtr options);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_font_size (IntPtr cr, double size);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_line_cap (IntPtr cr, LineCap line_cap);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_line_join (IntPtr cr, LineJoin line_join);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_line_width (IntPtr cr, double width);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_matrix (IntPtr cr, ref Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_miter_limit (IntPtr cr, double limit);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_operator (IntPtr cr, Operator op);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_source (IntPtr cr, IntPtr pattern);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_source_rgb (IntPtr cr, double red, double green, double blue);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_source_rgba (IntPtr cr, double red, double green, double blue, double alpha);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_source_surface (IntPtr cr, IntPtr surface, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_set_tolerance (IntPtr cr, double tolerance);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_show_glyphs (IntPtr ct, IntPtr glyphs, int num_glyphs);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_show_page (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_status (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_status_to_string (Status status);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_stroke (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_stroke_extents (IntPtr cr, out double x1, out double y1, out double x2, out double y2);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_stroke_preserve (IntPtr cr);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_rectangle_list_destroy (IntPtr rectangle_list);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_copy_clip_rectangle_list (IntPtr cr);

		#region Surface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_surface_create_similar (IntPtr surface, Content content, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_destroy (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_finish (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_flush (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Content cairo_surface_get_content (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_get_device_offset (IntPtr surface, out double x, out double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_get_font_options (IntPtr surface, IntPtr FontOptions);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern uint cairo_surface_get_reference_count (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern SurfaceType cairo_surface_get_type (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_mark_dirty (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_mark_dirty_rectangle (IntPtr surface, int x, int y, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_surface_reference (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_set_device_offset (IntPtr surface, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_set_fallback_resolution (IntPtr surface, double x, double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_surface_status (IntPtr surface);
		#endregion

		#region SVG surface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_surface_write_to_png (IntPtr surface, string filename);

		//[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern void cairo_surface_write_to_png_stream (IntPtr surface, WriteFunc writeFunc);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_svg_surface_create (string fileName, double width, double height);

		//[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern IntPtr cairo_svg_surface_create_for_stream (double width, double height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_svg_surface_restrict_to_version (IntPtr surface, SvgVersion version);
		#endregion

		[DllImport (cairo, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void cairo_show_text (IntPtr cr, byte[] text);

		[DllImport (cairo, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void cairo_text_extents (IntPtr cr, byte[] utf8, out TextExtents extents);

		[DllImport (cairo, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void cairo_show_text (IntPtr cr, ref byte utf8);

		[DllImport (cairo, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void cairo_text_extents (IntPtr cr, ref byte utf8, out TextExtents extents);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_text_path (IntPtr ct, byte[] utf8);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_transform (IntPtr cr, Matrix matrix);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_translate (IntPtr cr, double tx, double ty);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_user_to_device (IntPtr cr, ref double x, ref double y);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_user_to_device_distance (IntPtr cr, ref double dx, ref double dy);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_version ();

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_version_string ();

		#region DirectFBSurface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_directfb_surface_create (IntPtr dfb, IntPtr surface);
		#endregion

		#region win32 fonts
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_win32_font_face_create_for_logfontw (IntPtr logfontw);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_win32_scaled_font_done_font (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern double cairo_win32_scaled_font_get_metrics_factor (IntPtr scaled_font);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_win32_scaled_font_select_font (IntPtr scaled_font, IntPtr hdc);
		#endregion

		#region win32 surface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_win32_surface_create (IntPtr hdc);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_win32_surface_create_with_ddb (IntPtr hdc, Format format, int width, int height);
		#endregion

		#region XcbSurface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_xcb_surface_create (IntPtr connection, uint drawable, IntPtr visual, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_xcb_surface_create_for_bitmap (IntPtr connection, uint bitmap, IntPtr screen, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_xcb_surface_set_size (IntPtr surface, int width, int height);
		#endregion

		#region XlibSurface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_xlib_surface_create (IntPtr display, IntPtr drawable, IntPtr visual, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_xlib_surface_create_for_bitmap (IntPtr display, IntPtr bitmap, IntPtr screen, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_xlib_surface_get_depth (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_xlib_surface_get_display (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_xlib_surface_get_drawable (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_xlib_surface_get_height (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_xlib_surface_get_screen (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_xlib_surface_get_visual (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_xlib_surface_get_width (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_xlib_surface_set_drawable (IntPtr surface, IntPtr drawable, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_xlib_surface_set_size (IntPtr surface, int width, int height);
		#endregion

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_gl_device_set_thread_aware(IntPtr device, int value);

		#region GLSurface
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_gl_surface_create (IntPtr device, uint content, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_gl_surface_create_for_texture (IntPtr device, uint content, uint tex, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_gl_surface_set_size (IntPtr surface, int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_gl_surface_get_width (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_gl_surface_get_height (IntPtr surface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_gl_surface_swapbuffers (IntPtr surf);
		#endregion

		#region GLX Functions
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_glx_device_create (IntPtr dpy, IntPtr gl_ctx);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_glx_device_get_display (IntPtr device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_glx_device_get_context (IntPtr device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_gl_surface_create_for_window (IntPtr device, IntPtr window, int width, int height);
		#endregion

		#region WGL Fucntions
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_wgl_device_create (IntPtr hglrc);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_wgl_device_get_context (IntPtr device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_gl_surface_create_for_dc (IntPtr device, IntPtr hdc, int width, int height);
		#endregion

		#region EGL Functions
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_egl_device_create (IntPtr dpy, IntPtr gl_ctx);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_gl_surface_create_for_egl (IntPtr device, IntPtr eglSurface, int width, int height);
		#endregion

		#region DRM Functions
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_device_get (IntPtr udev_device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_device_get_for_fd (int fd);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_device_default ();

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_drm_device_get_fd (IntPtr cairo_device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern  void cairo_drm_device_throttle (IntPtr cairo_device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_surface_create (IntPtr cairo_device, Format format,	int width, int height);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_surface_create_for_name (IntPtr cairo_device, uint name, Format format,	int width, int height, int stride);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_surface_create_from_cacheable_image (IntPtr cairo_device, IntPtr imageSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_drm_surface_enable_scan_out (IntPtr drmSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_surface_get_handle (IntPtr drmSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_surface_get_name (IntPtr drmSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Format cairo_drm_surface_get_format (IntPtr drmSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_drm_surface_get_width (IntPtr drmSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_drm_surface_get_height (IntPtr drmSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int cairo_drm_surface_get_stride (IntPtr drmSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_drm_surface_map_to_image (IntPtr drmSurface);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void	cairo_drm_surface_unmap (IntPtr drmSurface,	IntPtr imageSurface);
		#endregion

		#region Device
		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_device_acquire(IntPtr device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_device_destroy (IntPtr device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_device_reference (IntPtr device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void cairo_device_release(IntPtr device);

		[DllImport (cairo, CallingConvention=CallingConvention.Cdecl)]
		internal static extern Status cairo_device_status(IntPtr device);
		#endregion
	}
}
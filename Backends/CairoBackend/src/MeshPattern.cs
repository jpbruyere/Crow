//
// Mono.Cairo.Pattern.cs
//
// Author: Jordi Mas (jordi@ximian.com)
//         Hisham Mardam Bey (hisham.mardambey@gmail.com)
// (C) Ximian Inc, 2004.
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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
using Drawing2D;

namespace Crow.CairoBackend {

	public class MeshPattern : Pattern
	{
		internal MeshPattern (IntPtr handle, bool owned) : base (handle, owned)
		{
		}

		public MeshPattern ()
			: base (NativeMethods.cairo_pattern_create_mesh(), true)
		{
		}

		//no idea why this is here, the base one is identical, but we can't remove it now
		public new Extend Extend {
			set { NativeMethods.cairo_pattern_set_extend (handle, value); }
			get { return NativeMethods.cairo_pattern_get_extend (handle); }
		}

		public Filter Filter {
			set { NativeMethods.cairo_pattern_set_filter (handle, value); }
			get { return NativeMethods.cairo_pattern_get_filter (handle); }
		}

		public void BeginPatch(){
			NativeMethods.cairo_mesh_pattern_begin_patch (handle);
		}
		public void EndPatch(){
			NativeMethods.cairo_mesh_pattern_end_patch (handle);
		}
		public void MoveTo(double x, double y){
			NativeMethods.cairo_mesh_pattern_move_to (handle, x, y);
		}
		public void MoveTo (PointD p) {
			NativeMethods.cairo_mesh_pattern_move_to (handle, p.X, p.Y);
		}
		public void LineTo(double x, double y){
			NativeMethods.cairo_mesh_pattern_line_to (handle, x, y);
		}
		public void LineTo (PointD p) {
			NativeMethods.cairo_mesh_pattern_line_to (handle, p.X, p.Y);
		}
		public void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
		{
			NativeMethods.cairo_mesh_pattern_curve_to (handle, x1, y1, x2, y2, x3, y3);
		}
		public void SetControlPoint(uint point_num, double x, double y){
			NativeMethods.cairo_mesh_pattern_set_control_point (handle, point_num, x, y);
		}
		public void SetControlPoint (uint point_num, PointD p) {
			NativeMethods.cairo_mesh_pattern_set_control_point (handle, point_num, p.X, p.Y);
		}
		public void SetCornerColorRGB(uint corner_num, double r, double g, double b){
			NativeMethods.cairo_mesh_pattern_set_corner_color_rgb (handle, corner_num, r, g, b);
		}
		public void SetCornerColorRGBA(uint corner_num, double r, double g, double b, double a){
			NativeMethods.cairo_mesh_pattern_set_corner_color_rgba (handle, corner_num, r, g, b, a);
		}
		public void SetCornerColor (uint corner_num, Color c) {
			NativeMethods.cairo_mesh_pattern_set_corner_color_rgba (handle, corner_num, c.R, c.G, c.B, c.A);
		}
		public uint PatchCount {
			get {
				uint count = 0;
				NativeMethods.cairo_mesh_pattern_get_patch_count(handle, out count);
				return count;
			}	
		}
		public Path GetPath(uint patch_num){
			return new Path(NativeMethods.cairo_mesh_pattern_get_path(handle, patch_num));
		}
		public PointD GetControlPoint(uint point_num, uint patch_num = 0) {
			NativeMethods.cairo_mesh_pattern_get_control_point (handle, patch_num, point_num, out double x, out double y);
			return new PointD (x, y);
		}
		public void GetCornerColorRGBA(){
			
		}
	}
}


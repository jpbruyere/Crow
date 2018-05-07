//
// Context.cs
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
using Crow;
using System.Text;

namespace vkvg
{
	public class Context: IDisposable
	{

		IntPtr handle = IntPtr.Zero;

		public Context (Surface surf)
		{
			handle = NativeMethods.vkvg_create (surf.Handle);
		}
		~Context ()
		{
			Dispose (false);
		}

		public IntPtr Handle { get { return handle; }}

		public double LineWidth {			
			set { NativeMethods.vkvg_set_line_width (handle, (float)value); }
		}
		public uint FontSize {
			set { NativeMethods.vkvg_set_font_size (handle, value); }
		}
		public string FontFace {
			set { NativeMethods.vkvg_select_font_face (handle, value); }
		}
		public Operator Operator {
			set { NativeMethods.vkvg_set_operator (handle, value); }
			get { return NativeMethods.vkvg_get_operator (handle); }
		}
		public FontExtents FontExtents {
			get {
				FontExtents f_extents;
				NativeMethods.vkvg_font_extents (handle, out f_extents);
				return f_extents;
			}
		}
		public TextExtents TextExtents(string s)
		{
			TextExtents extents;
			NativeMethods.vkvg_text_extents (handle, TerminateUtf8(s), out extents);
			return extents;
		}
		public void ShowText (string txt) {
			NativeMethods.vkvg_show_text (handle, txt);
		}
		public void Save () {
			NativeMethods.vkvg_save (handle);
		}
		public void Restore () {
			NativeMethods.vkvg_restore (handle);
		}
		public void Flush () {
			NativeMethods.vkvg_flush (handle);
		}

		public void Paint () {
			NativeMethods.vkvg_paint (handle);
		}
		public void Arc (float xc, float yc, float radius, float a1, float a2) {
			NativeMethods.vkvg_arc (handle, xc, yc, radius, a1, a2);
		}
		public void Arc (double xc, double yc, double radius, double a1, double a2) {
			NativeMethods.vkvg_arc (handle, (float)xc, (float)yc, (float)radius, (float)a1, (float)a2);
		}
		public void ArcNegative (float xc, float yc, float radius, float a1, float a2) {
			NativeMethods.vkvg_arc_negative (handle, xc, yc, radius, a1, a2);
		}
		public void Rectangle (float x, float y, float width, float height) {
			NativeMethods.vkvg_rectangle (handle, x, y, width, height);
		}
		public void Scale (float sx, float sy) {
			NativeMethods.vkvg_scale (handle, sx, sy);
		}
		public void Translate (float dx, float dy) {
			NativeMethods.vkvg_translate (handle, dx, dy);
		}
		public void Rotate (float alpha) {
			NativeMethods.vkvg_rotate (handle, alpha);
		}
		public void ArcNegative (double xc, double yc, double radius, double a1, double a2) {
			NativeMethods.vkvg_arc_negative (handle, (float)xc, (float)yc, (float)radius, (float)a1, (float)a2);
		}
		public void Rectangle (double x, double y, double width, double height) {
			NativeMethods.vkvg_rectangle (handle, (float)x, (float)y, (float)width, (float)height);
		}
		public void Scale (double sx, double sy) {
			NativeMethods.vkvg_scale (handle, (float)sx, (float)sy);
		}
		public void Translate (double dx, double dy) {
			NativeMethods.vkvg_translate (handle, (float)dx, (float)dy);
		}
		public void Rotate (double alpha) {
			NativeMethods.vkvg_rotate (handle, (float)alpha);
		}

		public void Fill () {
			NativeMethods.vkvg_fill (handle);
		}
		public void FillPreserve () {
			NativeMethods.vkvg_fill_preserve (handle);
		}
		public void Stroke () {
			NativeMethods.vkvg_stroke (handle);
		}
		public void StrokePreserve () {
			NativeMethods.vkvg_stroke_preserve (handle);
		}
		public void Clip () {
			NativeMethods.vkvg_clip (handle);
		}
		public void ClipPreserve () {
			NativeMethods.vkvg_clip_preserve (handle);
		}
		public void ResetClip () {
			NativeMethods.vkvg_reset_clip (handle);
		}
		public void ClosePath () {
			NativeMethods.vkvg_close_path (handle);
		}

//		public void Rectangle (float x, float y, float width, float height){
//			NativeMethods.vkvg_rectangle ();
//		}
		public void MoveTo (PointD p){
			NativeMethods.vkvg_move_to (handle, (float)p.X, (float)p.Y);
		}
		public void MoveTo (float x, float y){
			NativeMethods.vkvg_move_to (handle, x, y);
		}
		public void RelMoveTo (float x, float y){
			NativeMethods.vkvg_rel_move_to (handle, x, y);
		}
		public void LineTo (float x, float y){
			NativeMethods.vkvg_line_to (handle, x, y);
		}
		public void LineTo (Point p){
			NativeMethods.vkvg_line_to (handle, p.X, p.Y);
		}
		public void LineTo (PointD p){
			NativeMethods.vkvg_line_to (handle, (float)p.X, (float)p.Y);
		}
		public void RelLineTo (float x, float y){
			NativeMethods.vkvg_rel_line_to (handle, x, y);
		}
		public void CurveTo (float x1, float y1, float x2, float y2, float x3, float y3){
			NativeMethods.vkvg_curve_to (handle, x1, y1, x2, y2, x3, y3);
		}
		public void RelCurveTo (float x1, float y1, float x2, float y2, float x3, float y3){
			NativeMethods.vkvg_rel_curve_to (handle, x1, y1, x2, y2, x3, y3);
		}

		public void MoveTo (double x, double y){
			NativeMethods.vkvg_move_to (handle, (float)x, (float)y);
		}
		public void RelMoveTo (double x, double y){
			NativeMethods.vkvg_rel_move_to (handle, (float)x, (float)y);
		}
		public void LineTo (double x, double y){
			NativeMethods.vkvg_line_to (handle, (float)x, (float)y);
		}
		public void RelLineTo (double x, double y){
			NativeMethods.vkvg_rel_line_to (handle, (float)x, (float)y);
		}
		public void CurveTo (double x1, double y1, double x2, double y2, double x3, double y3){
			NativeMethods.vkvg_curve_to (handle, (float)x1, (float)y1, (float)x2, (float)y2, (float)x3, (float)y3);
		}
		public void RelCurveTo (double x1, double y1, double x2, double y2, double x3, double y3){
			NativeMethods.vkvg_rel_curve_to (handle, (float)x1, (float)y1, (float)x2, (float)y2, (float)x3, (float)y3);
		}


		public void SetSource (float r, float g, float b, float a = 1f) {
			NativeMethods.vkvg_set_source_rgba (handle, r, g, b, a);
		}
		public void SetSource (double r, double g, double b, double a = 1.0) {
			NativeMethods.vkvg_set_source_rgba (handle, (float)r, (float)g, (float)b, (float)a);
		}
		public void SetSource (Surface surf, float x = 0f, float y = 0f) {
			NativeMethods.vkvg_set_source_surface (handle, surf.Handle, x, y);
		}
		public void SetSourceSurface (Surface surf, float x = 0f, float y = 0f) {
			NativeMethods.vkvg_set_source_surface (handle, surf.Handle, x, y);
		}

		private static byte[] TerminateUtf8(string s)
		{
			// compute the byte count including the trailing \0
			var byteCount = Encoding.UTF8.GetMaxByteCount(s.Length + 1);
			var bytes = new byte[byteCount];
			Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
			return bytes;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing || handle == IntPtr.Zero)
				return;

			NativeMethods.vkvg_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}


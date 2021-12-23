// Copyright (C) 2011 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Drawing2D;

namespace Crow.CairoBackend
{
	[StructLayout(LayoutKind.Sequential)]
	struct RectangleList {
		public Status Status;
		public IntPtr Rectangles;
		public int NumRectangles;
	}


	public class Region : IRegion {

		IntPtr handle;

		#region CTOR
		public Region (IntPtr handle, bool owned)
		{
			this.handle = handle;
			if (!owned)
				NativeMethods.cairo_region_reference (handle);
			if (CairoDebug.Enabled)
				CairoDebug.OnAllocated (handle);
		}
		public Region () : this (NativeMethods.cairo_region_create () , true)
		{}
		public Region (Rectangle rect) {
			handle = NativeMethods.cairo_region_create_rectangle (ref rect);
		}
		#endregion

		public Region Copy () => new Region (NativeMethods.cairo_region_copy (handle), true);


		public Status Status {
			get { return NativeMethods.cairo_region_status (handle); }
		}

		public Rectangle Extents {
			get {
				Rectangle result;
				NativeMethods.cairo_region_get_extents (handle, out result);
				return result;
			}
		}

		public bool Contains (int x, int y)
		{
			return NativeMethods.cairo_region_contains_point (handle, x, y);
		}

		public void Translate (int dx, int dy)
		{
			NativeMethods.cairo_region_translate (handle, dx, dy);
		}

		public Status Subtract (Region other)
		{
			return NativeMethods.cairo_region_subtract (handle, other.handle);
		}

		public Status SubtractRectangle (Rectangle rectangle)
		{
			return NativeMethods.cairo_region_subtract_rectangle (handle, ref rectangle);
		}

		public Status Intersect (Region other)
		{
			return NativeMethods.cairo_region_intersect (handle, other.handle);
		}

		public Status IntersectRectangle (Rectangle rectangle)
		{
			return NativeMethods.cairo_region_intersect_rectangle (handle, ref rectangle);
		}

		public Status Union (Region other)
		{
			return NativeMethods.cairo_region_union (handle, other.handle);
		}


		public Status Xor (Region other)
		{
			return NativeMethods.cairo_region_xor (handle, other.handle);
		}

		public Status XorRectangle (Rectangle rectangle)
		{
			return NativeMethods.cairo_region_xor_rectangle (handle, ref rectangle);
		}

		#region  IRegion implementation
		public bool IsEmpty => NativeMethods.cairo_region_is_empty (handle);
		public int NumRectangles => NativeMethods.cairo_region_num_rectangles (handle);
		public Rectangle GetRectangle (int nth)
		{
			Rectangle val;
			NativeMethods.cairo_region_get_rectangle (handle, nth, out val);
			return val;
		}
		public void UnionRectangle (Rectangle rectangle)
			=> NativeMethods.cairo_region_union_rectangle (handle, ref rectangle);
		public bool OverlapOut (Rectangle rectangle) => Contains (rectangle) == RegionOverlap.Out;
		public RegionOverlap Contains (Rectangle rectangle)
			=> NativeMethods.cairo_region_contains_rectangle (handle, ref rectangle);
		public void Reset () {
			if (IsEmpty)
				return;
			NativeMethods.cairo_region_destroy (handle);
			handle = NativeMethods.cairo_region_create ();
		}

		public bool Equals(IRegion other)
			=> other is Region r ? NativeMethods.cairo_region_equal (handle, r.handle) : false;
		#endregion

		public override bool Equals (object obj)
			=> obj is Region r ? NativeMethods.cairo_region_equal (handle, r.handle) : false;

		public override int GetHashCode () => handle.GetHashCode ();

		#region IDisposable
		~Region ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing || CairoDebug.Enabled)
				CairoDebug.OnDisposed<Region> (handle, disposing);

			if (!disposing|| handle == IntPtr.Zero)
				return;

			NativeMethods.cairo_region_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}

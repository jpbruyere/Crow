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

namespace Crow.CairoBackend
{
	public class Pattern : IPattern
	{
		internal IntPtr handle;
		public static Pattern Lookup (IntPtr pattern, bool owner)
		{
			if (pattern == IntPtr.Zero)
				return null;

			PatternType pt = NativeMethods.cairo_pattern_get_type (pattern);
			switch (pt) {
			case PatternType.Solid:
				return new SolidPattern (pattern, owner);
			case PatternType.Surface:
				return new SurfacePattern (pattern, owner);
			case PatternType.Linear:
				return new LinearGradient (pattern, owner);
			case PatternType.Radial:
				return new RadialGradient (pattern, owner);
			default:
				return new Pattern (pattern, owner);
			}
		}

		internal Pattern (IntPtr handle, bool owned)
		{
			this.handle = handle;
			if (!owned)
				NativeMethods.cairo_pattern_reference (handle);
			if (CairoDebug.Enabled)
				CairoDebug.OnAllocated (handle);
		}

		[Obsolete ("Use the SurfacePattern constructor")]
		public Pattern (Surface surface)
			: this ( NativeMethods.cairo_pattern_create_for_surface (surface.handle), true)
		{
		}

		[Obsolete]
		protected void Reference ()
		{
			NativeMethods.cairo_pattern_reference (handle);
		}



		public Status Status => NativeMethods.cairo_pattern_status (handle);

		public Matrix Matrix {
			set => NativeMethods.cairo_pattern_set_matrix (handle, value);
			get {
				Matrix m = new Matrix ();
				NativeMethods.cairo_pattern_get_matrix (handle, m);
				return m;
			}
		}

		public PatternType PatternType => NativeMethods.cairo_pattern_get_type (handle);

		#region IPattern implementation
		public Extend Extend
		{
			get { return NativeMethods.cairo_pattern_get_extend (handle); }
			set { NativeMethods.cairo_pattern_set_extend (handle, value); }
		}
		public Filter Filter {
			get => NativeMethods.cairo_pattern_get_filter (handle);
			set => NativeMethods.cairo_pattern_set_filter (handle, value);
		}
		#endregion

		~Pattern ()
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
				CairoDebug.OnDisposed<Pattern> (handle, disposing);

			if (!disposing|| handle == IntPtr.Zero)
				return;

			NativeMethods.cairo_pattern_destroy (handle);
			handle = IntPtr.Zero;
		}
	}
}


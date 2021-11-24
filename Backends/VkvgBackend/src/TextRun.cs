// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Drawing2D;

namespace Crow.VkvgBackend
{
	public class TextRun : IDisposable {

		IntPtr handle = IntPtr.Zero;

		#region CTORS & DTOR
		protected TextRun(IntPtr handle) {
			this.handle = handle;
		}
		public TextRun(string text) {
			handle = NativeMethods.vkvg_text_run_create (handle, Context.TerminateUtf8(text));
		}

		~TextRun() {
			Dispose (false);
		}
		#endregion

		//public void AddReference () {
		//	NativeMethods.vkvg_pattern_reference (handle);
		//}
		//public uint References () => NativeMethods.vkvg_pattern_get_reference_count (handle);

		public IntPtr Handle { get { return handle; } }

		public Drawing2D.TextExtents Extents {
			get {
				NativeMethods.vkvg_text_run_get_extents (handle, out TextExtents e);
				return new Drawing2D.TextExtents (e.XBearing, e.YBearing, e.Width, e.Height, e.XAdvance, e.YAdvance);
			}
		}

		#region IDisposable implementation
		public void Dispose () {
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing) {
			if (!disposing || handle == IntPtr.Zero)
				return;

			NativeMethods.vkvg_text_run_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}
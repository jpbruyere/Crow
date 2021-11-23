// Copyright (c) 2018-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
namespace Crow.Drawing {
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

		public TextExtents Extents {
			get {
				TextExtents extents;
				NativeMethods.vkvg_text_run_get_extents (handle, out extents);
				return extents;
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
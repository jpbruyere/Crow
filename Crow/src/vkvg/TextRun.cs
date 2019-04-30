//
// TextRun.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2019 jp
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
namespace vkvg {
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
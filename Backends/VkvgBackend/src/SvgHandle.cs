// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Drawing2D;

namespace Crow.VkvgBackend
{
	public sealed class SvgHandle : ISvgHandle {

		internal IntPtr handle;

		#region CTOR
		public  SvgHandle (Device dev, Span<byte> bytes)
		{
			/*int size = svgFragment.Length * 4 + 1;
			Span<byte> bytes = size > 512 ? new byte[size] : stackalloc byte[size];
			int encodedBytes = Crow.Text.Encoding.ToUtf8 (svgFragment, bytes);
			bytes[encodedBytes] = 0;*/
			handle = NativeMethods.nsvg_load (dev.Handle, ref bytes.GetPinnableReference());
		}
		public SvgHandle (Device dev, string file_name)
		{
			handle = NativeMethods.nsvg_load_file (dev.Handle, file_name);
		}
		#endregion

		public void Render(IContext cr) =>
			NativeMethods.vkvg_render_svg((cr as Context).handle, handle, null);
		public void Render (IContext cr, string id) =>
			NativeMethods.vkvg_render_svg((cr as Context).handle, handle, id);
		public Size Dimensions {
			get {
				NativeMethods.nsvg_get_size (handle, out int w, out int h);
				return new Size (w, h);
			}
		}
		public void Dispose() {
			NativeMethods.nsvg_destroy (handle);
		}

	}
}

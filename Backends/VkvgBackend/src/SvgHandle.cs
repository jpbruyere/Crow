//Copyright GPL2
using System;

namespace Crow.Drawing {


	public sealed class SvgHandle : IDisposable {

		public IntPtr Raw;
		
		public  SvgHandle (Device dev, Span<byte> bytes)		
		{
			/*int size = svgFragment.Length * 4 + 1;
			Span<byte> bytes = size > 512 ? new byte[size] : stackalloc byte[size];
			int encodedBytes = Crow.Text.Encoding.ToUtf8 (svgFragment, bytes);
			bytes[encodedBytes] = 0;*/
			Raw = NativeMethods.nsvg_load (dev.Handle, ref bytes.GetPinnableReference());
		}
		public SvgHandle (Device dev, string file_name)
		{			
			Raw = NativeMethods.nsvg_load_file (dev.Handle, file_name);
		}

		public void Render(Context cr) =>
			cr.RenderSvg (Raw);
		
		public void Render (Context cr, string id) =>
			cr.RenderSvg (Raw, id);
		
		public Size Dimensions {
			get {
				NativeMethods.nsvg_get_size (Raw, out int w, out int h);
				return new Size (w, h);
			}
		}

		public void Dispose() {			
			NativeMethods.nsvg_destroy (Raw);
		}

	}
}

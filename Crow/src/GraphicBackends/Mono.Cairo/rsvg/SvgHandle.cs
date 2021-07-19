//Copyright GPL2
using System;
using System.Runtime.InteropServices;

namespace Crow.Cairo {


	public sealed class SvgHandle : IDisposable {
		const string lib = "rsvg-2.40";

		public IntPtr Raw;

		[DllImport (lib)]
		static extern IntPtr rsvg_handle_new();
		[DllImport (lib)]
		static extern IntPtr rsvg_handle_new_from_data (byte[] data, UIntPtr n_data, out IntPtr error);
		[DllImport (lib)]
		static extern IntPtr rsvg_handle_new_from_file (string file_name, out IntPtr error);
		[DllImport (lib)]
		static extern IntPtr rsvg_handle_get_base_uri (IntPtr raw);
		[DllImport (lib)]
		static extern void rsvg_handle_set_dpi (IntPtr raw, double dpi);
		[DllImport (lib)]
		static extern void rsvg_handle_set_dpi_x_y (IntPtr raw, double dpi_x, double dpi_y);

		[DllImport (lib)]
		static extern void rsvg_handle_render_cairo (IntPtr raw, IntPtr cr);
		[DllImport (lib)]
		static extern void rsvg_handle_render_cairo_sub (IntPtr raw, IntPtr cr, string id);

		[DllImport (lib)]
		static extern void rsvg_handle_get_dimensions (IntPtr raw, IntPtr dimension_data);
		[DllImport (lib)]
		static extern bool rsvg_handle_close (IntPtr raw, out IntPtr error);
		[DllImport (lib)]
		static extern IntPtr rsvg_handle_get_title (IntPtr raw);
		[DllImport (lib)]
		static extern IntPtr rsvg_handle_get_metadata (IntPtr raw);

		public SvgHandle ()
		{
			Raw = rsvg_handle_new();
		}
		public  SvgHandle (byte[] data)
		{			
			Raw = rsvg_handle_new_from_data(data, new UIntPtr ((ulong) (data == null ? 0 : data.Length)), out IntPtr error);
			if (error != IntPtr.Zero) throw new Exception (error.ToString());
		}
		public SvgHandle (string file_name)
		{			
			Raw = rsvg_handle_new_from_file(file_name, out IntPtr error);
			if (error != IntPtr.Zero) throw new Exception (error.ToString());
		}


		public double Dpi { set => rsvg_handle_set_dpi (Raw, value); }
		public void SetDpiXY (double dpi_x, double dpi_y) => rsvg_handle_set_dpi_x_y (Raw, dpi_x, dpi_y);


		public void Render(Context cr) =>
			rsvg_handle_render_cairo (Raw, cr == null ? IntPtr.Zero : cr.Handle);
		
		public void Render (Context cr, string id) =>
			rsvg_handle_render_cairo_sub (Raw, cr == null ? IntPtr.Zero : cr.Handle, id);
		
		public Size Dimensions {
			get {
				DimensionData dimension_data;
				IntPtr native_dimension_data = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (DimensionData)));
				rsvg_handle_get_dimensions(Raw, native_dimension_data);
				dimension_data = DimensionData.New (native_dimension_data);
				Marshal.FreeHGlobal (native_dimension_data);
				return new Size (dimension_data.Width, dimension_data.Height);
			}
		}

		public void Dispose() {			
			bool raw_ret = rsvg_handle_close(Raw, out IntPtr error);
			if (error != IntPtr.Zero) throw new Exception (error.ToString());			
		}
	}
}

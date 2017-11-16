//Copyright GPL2
namespace Rsvg {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

	public class Handle {
		public const string librsvg = "rsvg";
		public IntPtr Raw;

		[DllImport(librsvg)]
		static extern IntPtr rsvg_handle_new();

		public Handle ()
		{
			Raw = rsvg_handle_new();
		}

		[DllImport(librsvg)]
		static extern unsafe IntPtr rsvg_handle_new_from_data(byte[] data, UIntPtr n_data, out IntPtr error);

		public unsafe Handle (byte[] data)
		{
			if (GetType () != typeof (Handle)) {
				throw new InvalidOperationException ("Can't override this constructor.");
			}
			IntPtr error = IntPtr.Zero;
			Raw = rsvg_handle_new_from_data(data, new UIntPtr ((ulong) (data == null ? 0 : data.Length)), out error);
			if (error != IntPtr.Zero) throw new Exception (error.ToString());
		}

		[DllImport(librsvg)]
		static extern unsafe IntPtr rsvg_handle_new_from_file(string file_name, out IntPtr error);

		public unsafe Handle (string file_name)
		{
			IntPtr error = IntPtr.Zero;
			Raw = rsvg_handle_new_from_file(file_name, out error);
			if (error != IntPtr.Zero) throw new Exception (error.ToString());
		}

		[DllImport(librsvg)]
		static extern IntPtr rsvg_handle_get_base_uri(IntPtr raw);

		[DllImport(librsvg)]
		static extern void rsvg_handle_set_dpi(IntPtr raw, double dpi);

		public double Dpi {
			set {
				rsvg_handle_set_dpi(Raw, value);
			}
		}

		[DllImport(librsvg)]
		static extern void rsvg_handle_render_cairo(IntPtr raw, IntPtr cr);

		public void RenderCairo(Cairo.Context cr) {
			unsafe{
				rsvg_handle_render_cairo (Raw, cr == null ? IntPtr.Zero : cr.Handle);
			}
		}

		[DllImport(librsvg)]
		static extern void rsvg_handle_set_dpi_x_y(IntPtr raw, double dpi_x, double dpi_y);

		public void SetDpiXY(double dpi_x, double dpi_y) {
			rsvg_handle_set_dpi_x_y(Raw, dpi_x, dpi_y);
		}

		[DllImport(librsvg)]
		static extern void rsvg_handle_get_dimensions(IntPtr raw, IntPtr dimension_data);

		public Rsvg.DimensionData Dimensions {
			get {
				Rsvg.DimensionData dimension_data;
				IntPtr native_dimension_data = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (Rsvg.DimensionData)));
				rsvg_handle_get_dimensions(Raw, native_dimension_data);
				dimension_data = Rsvg.DimensionData.New (native_dimension_data);
				Marshal.FreeHGlobal (native_dimension_data);
				return dimension_data;
			}
		}

		[DllImport(librsvg)]
		static extern unsafe bool rsvg_handle_close(IntPtr raw, out IntPtr error);

		public unsafe bool Close() {
			IntPtr error = IntPtr.Zero;
			bool raw_ret = rsvg_handle_close(Raw, out error);
			if (error != IntPtr.Zero) throw new Exception (error.ToString());
			return raw_ret;
		}

		[DllImport(librsvg)]
		static extern IntPtr rsvg_handle_get_title(IntPtr raw);

		public string Title {
			get {
				IntPtr raw_ret = rsvg_handle_get_title(Raw);
				return "not supported";
			}
		}

		[DllImport(librsvg)]
		static extern void rsvg_handle_render_cairo_sub(IntPtr raw, IntPtr cr, string id);

		public void RenderCairoSub(Cairo.Context cr, string id) {
			rsvg_handle_render_cairo_sub(Raw, cr == null ? IntPtr.Zero : cr.Handle, id);
		}

		[DllImport(librsvg)]
		static extern IntPtr rsvg_handle_get_metadata(IntPtr raw);

		public string Metadata {
			get {
				IntPtr raw_ret = rsvg_handle_get_metadata(Raw);
				return "not supported";
			}
		}
	}
}

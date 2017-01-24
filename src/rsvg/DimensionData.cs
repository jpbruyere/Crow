namespace Rsvg {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public struct DimensionData {

		public int Width;
		public int Height;
		public double Em;
		public double Ex;

		public static Rsvg.DimensionData Zero = new Rsvg.DimensionData ();

		public static Rsvg.DimensionData New(IntPtr raw) {
			if (raw == IntPtr.Zero)
				return Rsvg.DimensionData.Zero;
			return (Rsvg.DimensionData) Marshal.PtrToStructure (raw, typeof (Rsvg.DimensionData));
		}
	}
}

// Copyright (c) 2018-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace vkvg
{
	public class Device: IDisposable
	{

		IntPtr handle = IntPtr.Zero;

		#region CTORS & DTOR
		public Device (IntPtr instance, IntPtr phy, IntPtr dev, uint qFamIdx, SampleCount samples = SampleCount.Sample_1, uint qIndex = 0)
		{
			handle = NativeMethods.vkvg_device_create_multisample (instance, phy, dev, qFamIdx, qIndex, samples, false);
		}
		~Device ()
		{
			Dispose (false);
		}
		#endregion

		public void GetDpy (out int hdpy, out int vdpy) {
			NativeMethods.vkvg_device_get_dpy (handle, out hdpy, out vdpy);
		}
		public void SetDpy (int hdpy, int vdpy) {
			NativeMethods.vkvg_device_set_dpy (handle, hdpy, vdpy);
		}
		public void AddReference () {
			NativeMethods.vkvg_device_reference (handle);
		}
		public uint References () => NativeMethods.vkvg_device_get_reference_count (handle);

		public IntPtr Handle { get { return handle; }}

		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing || handle == IntPtr.Zero)
				return;

			NativeMethods.vkvg_device_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}


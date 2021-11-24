// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Drawing2D;

namespace Crow.VkvgBackend
{
	public class Pattern : IPattern
	{
		protected IntPtr handle = IntPtr.Zero;
		public IntPtr Handle => handle;

		#region CTORS & DTOR
		protected Pattern(IntPtr handle)
		{
			this.handle = handle;
		}
		public Pattern(float r, float g, float b)
		{
			handle = NativeMethods.vkvg_pattern_create_rgb(r, g, b);
		}
		public Pattern(float r, float g, float b, float a)
		{
			handle = NativeMethods.vkvg_pattern_create_rgba(r, g, b, a);
		}
		public Pattern(Surface surf)
		{
			handle = NativeMethods.vkvg_pattern_create_for_surface(surf.Handle);
		}

		~Pattern()
		{
			Dispose(false);
		}
		#endregion

		public void AddReference()
		{
			NativeMethods.vkvg_pattern_reference(handle);
		}
		public uint References() => NativeMethods.vkvg_pattern_get_reference_count(handle);


		public Extend Extend
		{
			get => NativeMethods.vkvg_pattern_get_extend(handle);
			set { NativeMethods.vkvg_pattern_set_extend(handle, value); }
		}
		public Filter Filter
		{
			get => NativeMethods.vkvg_pattern_get_filter(handle);
			set { NativeMethods.vkvg_pattern_set_filter(handle, value); }
		}

		#region IDisposable implementation
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || handle == IntPtr.Zero)
				return;

			NativeMethods.vkvg_pattern_destroy(handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}
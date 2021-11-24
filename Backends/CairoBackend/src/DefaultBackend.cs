using System;
using Crow.CairoBackend;

namespace Crow.Backends
{
	public class DefaultBackend : ImageBackend {
		/// <summary>
		/// Create a new generic backend bound to the application surface
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		public DefaultBackend (IntPtr nativeWindoPointer, int width, int height)
		: base (nativeWindoPointer, width, height) { }
	}
}


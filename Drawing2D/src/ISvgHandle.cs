//Copyright GPL2
using System;
using System.Runtime.InteropServices;

namespace Drawing2D {


	public interface ISvgHandle : IDisposable {
		void Render(IContext cr);
		void Render (IContext cr, string id);

		Size Dimensions { get; }
	}
}

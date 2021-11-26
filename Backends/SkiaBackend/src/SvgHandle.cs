// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using Drawing2D;
using SkiaSharp;
using Svg.Skia;

namespace Crow.SkiaBackend
{
	public sealed class SvgHandle : ISvgHandle {

		SKSvg handle;

		SvgHandle () {
			handle = new SKSvg ();
		}
		internal SvgHandle (string file_name) : this ()
		{
			handle.Load (file_name);
		}
		internal SvgHandle (Stream stream) : this ()
		{
			handle.Load (stream);
		}
		internal static SvgHandle FromFragment (string framgment) {
			SvgHandle svg = new SvgHandle();
			svg.handle.FromSvg (framgment);
			return svg;
		}

		public void Render(IContext cr)
		{
			Context ctx = cr as Context;
			ctx.canvas.DrawPicture (handle.Picture);
		}
		public void Render (IContext cr, string id)
		{
			Context ctx = cr as Context;
			ctx.canvas.DrawPicture (handle.Picture);
		}
		public Size Dimensions
			=> new Size ((int)handle.Drawable.Bounds.Width, (int)handle.Drawable.Bounds.Height);

		public void Dispose() {
			handle.Dispose ();
		}

	}
}

// Copyright (c) 2018-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;

namespace Drawing2D
{
	public interface IDevice: IDisposable
	{
		//IntPtr Handle => handle;

		void GetDpy (out int hdpy, out int vdpy);
		void SetDpy (int hdpy, int vdpy);
		IRegion CreateRegion ();
		ISurface CreateSurface (int width, int height);
		ISurface CreateSurface (byte[] data, int width, int height);
		ISurface CreateSurface (IntPtr glfwWinHandle, int width, int height);
		IContext CreateContext (ISurface surf);
		//IPattern CreatePattern (PatternType patternType);
		IGradient CreateGradient (GradientType gradientType, Rectangle bounds);
		byte[] LoadBitmap (Stream stream, out Size dimensions);
		ISvgHandle LoadSvg (Stream stream);
		ISvgHandle LoadSvg (string svgFragment);
	}
}


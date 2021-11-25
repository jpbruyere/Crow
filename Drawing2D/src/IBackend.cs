// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;

namespace Drawing2D
{
	public interface IBackend: IDisposable
	{
		ISurface CreateSurface (int width, int height);
		ISurface CreateSurface (byte[] data, int width, int height);
		//ISurface CreateMainSurface (IntPtr glfwWinHandle, int width, int height);
		ISurface MainSurface { get; }
		IRegion CreateRegion ();
		IContext CreateContext (ISurface surf);
		//IPattern CreatePattern (PatternType patternType);
		IGradient CreateGradient (GradientType gradientType, Rectangle bounds);
		byte[] LoadBitmap (Stream stream, out Size dimensions);
		ISvgHandle LoadSvg (Stream stream);
		ISvgHandle LoadSvg (string svgFragment);
		IContext PrepareUIFrame (IContext existingContext, IRegion clipping);
		void FlushUIFrame (IContext ctx);
		void ResizeMainSurface (int width, int height);
		/*IRegion CreateRegion ();
		ISurface CreateSurface (int width, int height);
		ISurface CreateSurface (byte[] data, int width, int height);
		ISurface CreateSurface (IntPtr glfwWinHandle, int width, int height);
		IContext CreateContext (ISurface surf);
		//IPattern CreatePattern (PatternType patternType);
		IGradient CreateGradient (GradientType gradientType, Rectangle bounds);
		byte[] LoadBitmap (Stream stream, out Size dimensions);
		ISvgHandle LoadSvg (Stream stream);
		ISvgHandle LoadSvg (string svgFragment);*/
	}
}


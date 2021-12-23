// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;

namespace Drawing2D
{
	public abstract class CrowBackend: IDisposable
	{
		/// <summary>
		/// GLFW Native window pointer
		/// </summary>
		public IntPtr hWin { get; protected set; }
		public bool ownGlfwWinHandle { get; private set; }
		public CrowBackend (int width, int height, IntPtr nativeWindow = default) {
			ownGlfwWinHandle = (nativeWindow == IntPtr.Zero);
			hWin = nativeWindow;
		}
		/// <summary>
		/// Main rendering surface, usualy an accelerated window surface
		/// </summary>
		public abstract ISurface MainSurface { get; }
		/// <summary>
		/// Create a new surface
		/// </summary>
		/// <param name="width">width of the new surface</param>
		/// <param name="height">height of the new surface</param>
		/// <returns>the new surface instance</returns>
		public abstract ISurface CreateSurface (int width, int height);
		/// <summary>
		/// Create a new surface backed by the byte array provided as argument
		/// </summary>
		/// <param name="data">a byte array to hold the pixels of the new surface</param>
		/// <param name="width">width of the new surface</param>
		/// <param name="height">height of the new surface</param>
		/// <returns>the new surface instance</returns>
		public abstract ISurface CreateSurface (byte[] data, int width, int height);
		//ISurface CreateMainSurface (IntPtr glfwWinHandle, int width, int height);
		public abstract IRegion CreateRegion ();
		public abstract IContext CreateContext (ISurface surf);
		//IPattern CreatePattern (PatternType patternType);
		public abstract IGradient CreateGradient (GradientType gradientType, Rectangle bounds);
		public abstract byte[] LoadBitmap (Stream stream, out Size dimensions);
		public abstract ISvgHandle LoadSvg (Stream stream);
		public abstract ISvgHandle LoadSvg (string svgFragment);
		public abstract IContext PrepareUIFrame (IContext existingContext, IRegion clipping);
		public abstract void FlushUIFrame (IContext ctx);
		public abstract void ResizeMainSurface (int width, int height);

		protected bool isDisposed;
		public abstract void Dispose();
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


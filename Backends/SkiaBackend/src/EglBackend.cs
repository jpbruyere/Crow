// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Runtime.InteropServices;
using Drawing2D;
using SkiaSharp;

namespace Crow.SkiaBackend
{
	public class EglBackend : IBackend
	{
		[DllImport("libEGL")]
		extern static IntPtr eglGetProcAddress(string procname);

		int sampleCount = 1;
		Surface surf;
		public ISurface MainSurface => surf;
		public EglBackend (IntPtr nativeWindoPointer, int width, int height) : base () {

		}
		/// <summary>
		/// Create a new offscreen backend, used in perfTests
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		public EglBackend (int width, int height) : base ()
		{
			/*GRGlGetProcedureAddressDelegate del = new GRGlGetProcedureAddressDelegate (eglGetProcAddress);
			GRGlInterface iface = GRGlInterface.CreateGles (del);
			GRContext gr = GRContext.CreateGl (iface, default);
			SKImageInfo sKImgInfo = new SKImageInfo (width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			GRBackendRenderTarget rtd = GRBackendRenderTarget.
			GRBackendRenderTarget target = new GRBackendRenderTarget (width, height, sampleCount, 0, sKImgInfo);
			SKSurface.CreateAsRenderTarget (gr,)
			surf = new Surface (width, height);*/
		}
		public IContext CreateContext(ISurface surf)
		{
			throw new NotImplementedException();
		}

		public IGradient CreateGradient(GradientType gradientType, Rectangle bounds)
		{
			throw new NotImplementedException();
		}

		public IRegion CreateRegion()
		{
			throw new NotImplementedException();
		}

		public ISurface CreateSurface(int width, int height)
		{
			throw new NotImplementedException();
		}

		public ISurface CreateSurface(byte[] data, int width, int height)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public void FlushUIFrame(IContext ctx)
		{
			throw new NotImplementedException();
		}

		public byte[] LoadBitmap(Stream stream, out Size dimensions)
		{
			throw new NotImplementedException();
		}

		public ISvgHandle LoadSvg(Stream stream)
		{
			throw new NotImplementedException();
		}

		public ISvgHandle LoadSvg(string svgFragment)
		{
			throw new NotImplementedException();
		}

		public IContext PrepareUIFrame(IContext existingContext, IRegion clipping)
		{
			throw new NotImplementedException();
		}

		public void ResizeMainSurface(int width, int height)
		{
			throw new NotImplementedException();
		}
	}
}
﻿using System;
using System.IO;
using Drawing2D;
using Glfw;

namespace Crow.CairoBackend
{
	public abstract class CairoBackendBase : CrowBackend {
		/// <summary>
		/// Main rendering surface, usualy an accelerated window surface
		/// </summary>
		protected Surface surf;
		protected IRegion clipping;
		/// <summary> Global font rendering settings for Cairo </summary>
		FontOptions FontRenderingOptions;
		/// <summary> Global font rendering settings for Cairo </summary>
		Antialias Antialias = Antialias.Subpixel;
		bool disposeContextOnFlush;
		protected CairoBackendBase (int width, int height, IntPtr nativeWindow)
		: base (width, height, nativeWindow)
		{
			FontRenderingOptions = new FontOptions ();
			FontRenderingOptions.Antialias = Antialias.Subpixel;
			FontRenderingOptions.HintMetrics = HintMetrics.On;
			FontRenderingOptions.HintStyle = HintStyle.Full;
			FontRenderingOptions.SubpixelOrder = SubpixelOrder.Default;
		}
		~CairoBackendBase ()
		{
			Dispose (false);
		}
		public override abstract ISurface CreateSurface(int width, int height);
		public override abstract ISurface CreateSurface(byte[] data, int width, int height);
		public override ISurface MainSurface => surf;
		public override IRegion CreateRegion () => new Region ();
		public override IContext CreateContext (ISurface surf)
		{
			Context gr = new Context (surf as Surface);
			gr.FontOptions = FontRenderingOptions;
			gr.Antialias = Antialias;
			return gr;
		}
		//IPattern CreatePattern (PatternType patternType);
		public override IGradient CreateGradient (GradientType gradientType, Rectangle bounds)
		{
			switch (gradientType) {
			case GradientType.Vertical:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
			case GradientType.Horizontal:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Top);
			case GradientType.Oblic:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
			case GradientType.Radial:
				throw new NotImplementedException ();
			}
			return null;
		}
		public override byte[] LoadBitmap (Stream stream, out Size dimensions)
		{
			byte[] image;
#if STB_SHARP
			StbImageSharp.ImageResult stbi = StbImageSharp.ImageResult.FromStream (stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
			image = new byte[stbi.Data.Length];
			//rgba to argb for cairo.
			for (int i = 0; i < stbi.Data.Length; i += 4) {
				image[i] = stbi.Data[i + 2];
				image[i + 1] = stbi.Data[i + 1];
				image[i + 2] = stbi.Data[i];
				image[i + 3] = stbi.Data[i + 3];
			}
			dimensions = new Size (stbi.Width, stbi.Height);
#else
			using (StbImage stbi = new StbImage (stream)) {
				image = new byte [stbi.Size];
				for (int i = 0; i < stbi.Size; i+=4) {
					//rgba to argb for cairo. ???? looks like bgra to me.
					image [i] = Marshal.ReadByte (stbi.Handle, i + 2);
					image [i + 1] = Marshal.ReadByte (stbi.Handle, i + 1);
					image [i + 2] = Marshal.ReadByte (stbi.Handle, i);
					image [i + 3] = Marshal.ReadByte (stbi.Handle, i + 3);
				}
				dimensions = new Size (stbi.Width, stbi.Height);
			}
#endif
			return image;
		}
		public override ISvgHandle LoadSvg(Stream stream)
		{
			using (BinaryReader sr = new BinaryReader (stream))
				return new SvgHandle (sr.ReadBytes ((int)stream.Length));
		}
		public override ISvgHandle LoadSvg(string svgFragment) =>
			new SvgHandle (System.Text.Encoding.Unicode.GetBytes (svgFragment));
		protected void clear(IContext ctx) {
			for (int i = 0; i < clipping.NumRectangles; i++)
				ctx.Rectangle (clipping.GetRectangle (i));

			ctx.ClipPreserve ();
			ctx.Operator = Operator.Clear;
			ctx.Fill ();
			ctx.Operator = Operator.Over;
		}
		public override IContext PrepareUIFrame(IContext existingContext, IRegion clipping)
		{
			this.clipping = clipping;
			IContext ctx = existingContext;
			if (ctx == null) {
				disposeContextOnFlush = true;
				ctx = new Context (surf);
			} else
				disposeContextOnFlush = false;
			return ctx;
		}
		public override void FlushUIFrame(IContext ctx)
		{
			if (disposeContextOnFlush)
				ctx.Dispose ();
			clipping = null;
		}
		public override void ResizeMainSurface (int width, int height)
		{
			surf.Resize (width, height);
		}

		#region IDispose implementation
		public override void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing)
		{
			if (!isDisposed && disposing) {
				surf.Dispose ();
				FontRenderingOptions.Dispose ();
			}
			isDisposed = true;
		}
		#endregion
	}
}


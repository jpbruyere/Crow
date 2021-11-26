// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Text;
using System.Linq;
using Drawing2D;
using SkiaSharp;

namespace Crow.SkiaBackend
{
	public class Context : IContext
	{
		VkSurface surf;
		internal SKCanvas canvas;
		SKPaint paint;
		SKPath path;
		FillRule fillRule = FillRule.NonZero;
		SKFont font;
		static Operator[] operatorLut = new Operator[] {
			Operator.Clear,
			Operator.Source,
			Operator.Dest,
			Operator.Over,
			Operator.DestOver,
			Operator.In,
			Operator.DestIn,
			Operator.Out,
			Operator.DestOut,
			Operator.Atop,
			Operator.DestAtop,
			Operator.Xor,
			Operator.Add,
			0, //SKBlendMode.Modulate,
			Operator.Screen,
			Operator.Overlay,
			Operator.Darken,
			Operator.Lighten,
			0, //SKBlendMode.ColorDodge,
			0, //SKBlendMode.ColorBurn,
			0, //SKBlendMode.HardLight,
			0, //SKBlendMode.SoftLight,
			0, //SKBlendMode.Difference,
			0, //SKBlendMode.Exclusion,
			Operator.Multiply,
			0, //SKBlendMode.Hue,
			Operator.Saturate,//mod
			0, //SKBlendMode.Color,
			0, //SKBlendMode.Luminosity
		};
		static SKBlendMode[] skBlendModeLut = new SKBlendMode[] {
			SKBlendMode.Clear,
			SKBlendMode.Src,
			SKBlendMode.SrcOver,
			SKBlendMode.SrcIn,
			SKBlendMode.SrcOut,
			SKBlendMode.SrcATop,
			SKBlendMode.Dst,
			SKBlendMode.DstOver,
			SKBlendMode.DstIn,
			SKBlendMode.DstOut,
			SKBlendMode.DstATop,
			SKBlendMode.Xor,
			SKBlendMode.Plus,
			SKBlendMode.Saturation,
			SKBlendMode.Multiply,
			SKBlendMode.Screen,
			SKBlendMode.Overlay,
			SKBlendMode.Darken,
			SKBlendMode.Lighten
		};

		internal Context (VkSurface surf)
		{
			this.surf = surf;
			canvas = surf.Canvas;
			paint = new SKPaint ();
		}
		~Context()
		{
			Dispose(false);
		}

		public double LineWidth {
			get => (float)paint.StrokeWidth;
			set => paint.StrokeWidth = (float)value;
		}
		public LineJoin LineJoin {
			get => (LineJoin)paint.StrokeJoin;
			set => paint.StrokeJoin = (SKStrokeJoin)value;
		}
		public LineCap LineCap {
			get => (LineCap)paint.StrokeCap;
			set => paint.StrokeCap = (SKStrokeCap)value;
		}
		public Operator Operator {
			get => operatorLut [(int)paint.BlendMode];
			set => paint.BlendMode = skBlendModeLut[(int)value];
		}
		public FillRule FillRule {
			get => fillRule;
			set => fillRule = value;
		}

		public FontExtents FontExtents {
			get {
				if (font == null)
					createDefaultFont ();
				paint.GetFontMetrics (out SKFontMetrics m);
				return new FontExtents (-m.Ascent, m.Descent, m.Descent - m.Ascent, m.MaxCharacterWidth, m.XHeight);
			}
		}
		public Antialias Antialias {
			get => paint.IsAntialias ? Antialias.Grey : Antialias.None;
			set => paint.IsAntialias = (value == Antialias.Grey);
		}

		void checkPath () {
			if (path == null)
				path = new SKPath ();
		}
		public void Arc(double xc, double yc, double radius, double a1, double a2)
		{
			checkPath ();

			float r = (float)radius;
			float x = (float)xc;
			float y = (float)yc;

			SKRect rect = new SKRect (x - r, y - r, x + r, y + r);
			path.ArcTo (rect, (float)a1, (float)a2 - (float)a1, false);
		}

		public void Arc(PointD center, double radius, double angle1, double angle2)
			=> Arc (center.X, center.Y, radius, angle1, angle2);
		public void ArcNegative(double xc, double yc, double radius, double angle1, double angle2)
			=> Arc (xc, yc, radius, angle2, angle1);

		public void ArcNegative(PointD center, double radius, double angle1, double angle2)
			=> Arc (center.X, center.Y, radius, angle2, angle1);

		public void Clear() => canvas.Clear ();
		public void Clip()
		{
			ClipPreserve ();
			path?.Dispose ();
			path = null;
		}

		public void ClipPreserve()
		{
			if (path == null)
				return;
			canvas.ClipPath (path,
				fillRule == FillRule.NonZero ? SKClipOperation.Intersect : SKClipOperation.Difference, paint.IsAntialias);
		}

		public void ClosePath() => path?.Close ();

		public void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
		{
			checkPath ();
			path.CubicTo ((float) x1, (float) y1, (float) x2, (float) y2, (float) x3, (float) y3);
		}
		public void Fill()
		{
			FillPreserve ();
			path?.Dispose ();
			path = null;
		}

		public void FillPreserve()
		{
			if (path == null)
				return;
			paint.IsStroke = false;
			canvas.DrawPath (path, paint);
		}

		public void Flush() => canvas.Flush ();
		public void LineTo(double x, double y)
		{
			checkPath ();
			path.LineTo ((float)x, (float)y);
		}
		public void LineTo(Point p) => LineTo (p.X, p.Y);
		public void LineTo(PointD p) => LineTo (p.X, p.Y);
		public void MoveTo(double x, double y)
		{
			checkPath ();
			path.MoveTo ((float)x, (float)y);
		}
		public void MoveTo(Point p) => MoveTo (p.X, p.Y);
		public void MoveTo(PointD p) => MoveTo (p.X, p.Y);
		public void NewPath()
		{
			path?.Dispose ();
			path = new SKPath ();
		}

		public void NewSubPath()
		{
			path = new SKPath (path);
		}

		public void Paint() => canvas.DrawPaint (paint);
		public void PaintWithAlpha(double alpha)
		{
//			throw new NotImplementedException();
			canvas.DrawPaint (paint);
		}

		public void PopGroupToSource()
		{
			throw new NotImplementedException();
		}

		public void PushGroup()
		{
			throw new NotImplementedException();
		}

		public void Rectangle(double x, double y, double width, double height)
		{
			checkPath ();
			SKRect r = new SKRect ((float) x, (float) y, (float) (x + width), (float) (y + height));
			path.AddRect (r, SKPathDirection.Clockwise);
		}

		public void Rectangle(Rectangle r) => Rectangle (r.X, r.Y, r.Width, r.Height);

		public void RelCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
		{
			if (path == null)
				return;
			double dx = path.LastPoint.X, dy = path.LastPoint.Y;
			CurveTo (dx + x1, dx + y1, dx + x2, dx + y2, dx + x3, dx + y3);
		}

		public void RelLineTo(double x, double y)
		{
			if (path == null)
				return;
			double dx = path.LastPoint.X, dy = path.LastPoint.Y;
			LineTo (dx + x, dx + y);
		}

		public void RelMoveTo(double x, double y)
		{
			if (path == null)
				return;
			double dx = path.LastPoint.X, dy = path.LastPoint.Y;
			MoveTo (dx + x, dx + y);
		}

		public void RenderSvg(ISvgHandle svgHandle, string subId = null)
			=> svgHandle.Render (this, subId);

		public void ResetClip()
		{
			if (!canvas.IsClipEmpty)
				canvas.ClipRegion (new SKRegion (new SKRectI (0,0, surf.Width, surf.Height)));
		}

		public void Restore() => canvas.Restore ();
		SKMatrix savedMat;
		public void RestoreTransformations()
		{
			savedMat = canvas.TotalMatrix;
		}

		public void Rotate(double alpha) => canvas.RotateRadians ((float)alpha);
		public void Save() => canvas.Save ();
		public void SaveTransformations() => canvas.SetMatrix (savedMat);
		public void Scale(double sx, double sy) => canvas.Scale ((float)sx, (float)sy);

		void updatePaintFont () {
			paint.Typeface = font.Typeface;
			paint.TextSize = font.Size;
		}
		void createDefaultFont () {
			font = new SKFont (SKTypeface.FromFamilyName ("mono"));
		}
		public void SelectFontFace(string family, FontSlant slant, FontWeight weight)
		{
			font?.Dispose ();
			font = new SKFont (SKTypeface.FromFamilyName (family,
				(weight == FontWeight.Normal) ? SKFontStyleWeight.Normal : SKFontStyleWeight.Bold,
				SKFontStyleWidth.Normal, (SKFontStyleSlant)slant));
			updatePaintFont ();
		}
		public void SetDash(double[] dashes, double offset = 0)
		{
			if (dashes == null || dashes.Length == 1) {
				paint.PathEffect = null;
				return;
			}
			paint.PathEffect = SKPathEffect.CreateDash (dashes.Cast<float>().ToArray(), (float)offset);
		}
		public void SetFontSize(double scale)
		{
			if (font == null)
				createDefaultFont ();
			font.Size = (float)scale;
			updatePaintFont ();
		}
		public void SetSource(IPattern pat)
		{
			if (pat is Gradient gr)
				paint.Shader = gr.shader;
		}
		public void SetSource(Color color)
		{
			paint.Shader = null;
			paint.Color = (UInt32)color;
		}
		public void SetSource(double r, double g, double b, double a = 1)
		{
			paint.Shader = null;
			paint.Color = (UInt32)new Color (a, r, g, b);
		}
		VkSurface sourceSurface;
		public void SetSource(ISurface surf, double x = 0, double y = 0)
		{
			VkSurface s = surf as VkSurface;
			s.Flush();
			paint.Shader = SKShader.CreateImage(s.SkSurf.Snapshot(),
				SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, SKMatrix.CreateTranslation ((float)x, (float)y));
			//canvas.DrawSurface (s.SkSurf, (float)x, (float)y);
		}

		public void Stroke()
		{
			StrokePreserve ();
			path?.Dispose ();
			path = null;
		}
		public void StrokePreserve()
		{
			if (path == null)
				return;
			paint.IsStroke = true;
			canvas.DrawPath (path, paint);
		}
		public Rectangle StrokeExtents()
		{
			throw new NotImplementedException();
		}

		public TextExtents TextExtents(ReadOnlySpan<char> s, int tabSize = 4)
		{
			TextExtents (s, tabSize, out Drawing2D.TextExtents e);
			return e;
		}
		public void TextExtents(ReadOnlySpan<char> s, int tabSize, out TextExtents extents)
		{
			if (s.Length == 0) {
				extents = default;
				return;
			}
			int size = s.Length * 4 + 1;
			Span<byte> bytes = size > 512 ? new byte[size] : stackalloc byte[size];
			int encodedBytes = s.ToUtf8 (bytes, tabSize);
			bytes[encodedBytes] = 0;
			TextExtents (bytes.Slice (0, encodedBytes + 1), out extents);
		}
		public void TextExtents(Span<byte> bytes, out TextExtents extents)
		{
			if (font == null)
				createDefaultFont ();
			paint.GetFontMetrics (out SKFontMetrics metrics);
			SKRect bounds = default;
			paint.MeasureText (bytes.Slice (0, bytes.Length - 1), ref bounds);
			extents = new TextExtents (0,0,bounds.Width, bounds.Height, bounds.Width, bounds.Height);
		}
		public void ShowText(string text)
		{
			ShowText (text.AsSpan ());
		}
		public void ShowText(ReadOnlySpan<char> s, int tabSize = 4)
		{
			int size = s.Length * 4 + 1;
			Span<byte> bytes = size > 512 ? new byte[size] : stackalloc byte[size];
			int encodedBytes = s.ToUtf8 (bytes, tabSize);
			bytes[encodedBytes] = 0;
			ShowText (bytes.Slice (0, encodedBytes + 1));
		}
		public void ShowText(Span<byte> bytes)
		{
			SKPoint origin = (path == null) ? SKPoint.Empty : path.LastPoint;
			if (font == null)
				createDefaultFont ();
			SKTextBlob tb = SKTextBlob.Create (bytes.Slice (0, bytes.Length - 1), SKTextEncoding.Utf8, font);

			canvas.DrawText (tb, origin.X, origin.Y, paint);
		}


		public void Translate(double dx, double dy) => canvas.Translate ((float)dx, (float)dy);
		public void Translate(PointD p) => Translate (p.X, p.Y);


		#region IDisposable implementation
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;
			paint?.Dispose ();
			path?.Dispose ();
			font?.Dispose ();
		}
		#endregion
	}
}


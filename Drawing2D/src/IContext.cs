// Copyright (c) 2018-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Drawing2D
{
	public interface IContext : IDisposable
	{

		IntPtr Handle { get; }

		double LineWidth { get; set; }
		LineJoin LineJoin { get; set; }
		LineCap LineCap { get; set; }
		void SetFontSize (double scale);
		void SelectFontFace (string family, FontSlant slant, FontWeight weight);
		Operator Operator { get; set; }
		FillRule FillRule { get; set; }
		FontExtents FontExtents { get; }
		Antialias Antialias  { get; set; }
		void SetDash (double [] dashes, double offset = 0);

		TextExtents TextExtents (ReadOnlySpan<char> s, int tabSize = 4);
		void TextExtents (ReadOnlySpan<char> s, int tabSize, out TextExtents extents);
		void TextExtents (Span<byte> bytes, out TextExtents extents);
		void ShowText (string text);
		void ShowText (ReadOnlySpan<char> s, int tabSize = 4);
		void ShowText (Span<byte> bytes);

		void Save();
		void Restore();
		void SaveTransformations ();
		void RestoreTransformations ();
		void Scale(double sx, double sy);
		void Translate(double dx, double dy);
		void Translate(PointD p);
		void Rotate(double alpha);

		void Flush();
		void Clear();

		void NewPath();
		void NewSubPath();
		void ClosePath();
		void Arc(double xc, double yc, double radius, double a1, double a2);
		void Arc (PointD center, double radius, double angle1, double angle2);
		void ArcNegative(double xc, double yc, double radius, double a1, double a2);
		void ArcNegative (PointD center, double radius, double angle1, double angle2);
		void MoveTo(Point p);
		void MoveTo(PointD p);
		void MoveTo(double x, double y);
		void RelMoveTo(double x, double y);
		void LineTo(double x, double y);
		void LineTo(Point p);
		void LineTo(PointD p);
		void RelLineTo(double x, double y);
		void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3);
		void RelCurveTo(double x1, double y1, double x2, double y2, double x3, double y3);
		void Rectangle(double x, double y, double width, double height);
		void Rectangle(Rectangle r);

		void Paint();
		void PaintWithAlpha (double alpha);
		void Fill();
		void FillPreserve();
		Rectangle StrokeExtents ();
		void Stroke();
		void StrokePreserve();
		void Clip();
		void ClipPreserve();
		void ResetClip();

		void SetSource(IPattern pat);
		void SetSource (Color color);
		void SetSource(double r, double g, double b, double a = 1.0);
		void SetSource(ISurface surf, double x = 0f, double y = 0f);
		void RenderSvg(IntPtr svgNativeHandle, string subId = null);

		void PushGroup ();
		void PopGroupToSource ();
	}
}


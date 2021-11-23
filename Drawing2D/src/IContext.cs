﻿// Copyright (c) 2018-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
		uint FontSize { get; set; }
		string FontFace { get; set; }
		Operator Operator { get; set; }
		FillRule FillRule { get; set; }
		FontExtents FontExtents { get; set; }
		Antialias Antialias  { get; set; }
		TextExtents TextExtents (ReadOnlySpan<char> s, int tabSize = 4);
		void TextExtents (ReadOnlySpan<char> s, int tabSize, out TextExtents extents);
		void TextExtents (Span<byte> bytes, out TextExtents extents);
		Matrix Matrix { get; set; }
		void ShowText (string text);
		void ShowText (ReadOnlySpan<char> s, int tabSize = 4);
		void ShowText (Span<byte> bytes);
		void Save();
		void Restore();
		void Flush();
		void Clear();
		void Paint();
		void PaintWithAlpha (double alpha);

		void Arc(float xc, float yc, float radius, float a1, float a2);
		void Arc(double xc, double yc, double radius, double a1, double a2);
		void Arc (PointD center, double radius, double angle1, double angle2);
		void ArcNegative (PointD center, double radius, double angle1, double angle2);
		void ArcNegative(float xc, float yc, float radius, float a1, float a2);
		void Rectangle(float x, float y, float width, float height);
		void Scale(float sx, float sy);
		void Translate(float dx, float dy);
		void Rotate(float alpha);
		void ArcNegative(double xc, double yc, double radius, double a1, double a2);
		void Rectangle(double x, double y, double width, double height);
		void Scale(double sx, double sy);
		void Translate(double dx, double dy);
		void Translate(PointD p);
		void Rotate(double alpha);
		void Fill();
		void FillPreserve();
		void Stroke();
		void StrokePreserve();
		void Clip();
		void ClipPreserve();
		void ResetClip();
		void NewPath();
		void NewSubPath();
		void ClosePath();
		void MoveTo(PointD p);
		void MoveTo(Point p);
		void MoveTo(float x, float y);
		void RelMoveTo(float x, float y);
		void LineTo(float x, float y);
		void LineTo(Point p);
		void LineTo(PointD p);
		void RelLineTo(float x, float y);
		void CurveTo(float x1, float y1, float x2, float y2, float x3, float y3);
		void RelCurveTo(float x1, float y1, float x2, float y2, float x3, float y3);
		void MoveTo(double x, double y);
		void RelMoveTo(double x, double y);
		void LineTo(double x, double y);
		void RelLineTo(double x, double y);
		void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3);
		void RelCurveTo(double x1, double y1, double x2, double y2, double x3, double y3);
		void SetSource(IPattern pat);
		void SetSource (Color color);
		void SetSource(float r, float g, float b, float a = 1f);
		void SetSource(double r, double g, double b, double a = 1.0);
		void SetSource(ISurface surf, float x = 0f, float y = 0f);
		void SetSourceSurface(ISurface surf, float x = 0f, float y = 0f);
		void RenderSvg(IntPtr svgNativeHandle, string subId = null);
		Rectangle StrokeExtents ();
		void SetDash (double [] dashes, double offset = 0);
		float[] Dashes { set; }

		//void PushGroup ();
		//void PopGroupToSource ();
	}
}


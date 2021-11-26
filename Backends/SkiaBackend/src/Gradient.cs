// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using Drawing2D;
using SkiaSharp;

namespace Crow.SkiaBackend
{
	public abstract class Gradient : IGradient
	{
		internal SKShader shader;
		protected SKPoint[] points;
		protected List<SKColor> colorStops = new List<SKColor> ();
		protected List<float> offsets = new List<float>();
		protected SKShaderTileMode tileMode = SKShaderTileMode.Clamp;
		protected Filter filter = Filter.Fast;
		protected Gradient (SKPoint p0, SKPoint p1) {
			points = new SKPoint[] {p0, p1};
		}
		~Gradient()
		{
			Dispose(false);
		}
		public Extend Extend {
			get => (Extend)tileMode;
			set => tileMode = (SKShaderTileMode)value;
		}
		public Filter Filter {
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public void AddColorStop (double offset, Color c) {
			shader?.Dispose();
			shader = null;
			offsets.Add ((float)offset);
			colorStops.Add (c.ToSkiaColor());
		}
		public void AddColorStop(float offset, float r, float g, float b, float a = 1f) {
			shader?.Dispose();
			shader = null;
			offsets.Add ((float)offset);
			colorStops.Add (new Color (a,r,g,b).Value);
		}
		internal abstract void Activate ();

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
			shader?.Dispose ();
		}
		#endregion
	}
	public class LinearGradient : Gradient {
		public LinearGradient (float x0, float y0, float x1, float y1)
		: base (new SKPoint (x0, y0), new SKPoint (x1, y1)) { }

		internal override void Activate()
		{
			if (shader != null)
				return;
			shader = SKShader.CreateLinearGradient (points[0], points[1],
				colorStops.ToArray(), offsets.ToArray(), tileMode);
		}
	}
	public class RadialGradient : Gradient {
		float[] radiuses;
		public RadialGradient (	float cx0, float cy0, float radius0,
								float cx1, float cy1, float radius1)
		: base (new SKPoint (cx0, cy0), new SKPoint (cx1, cy1)) {
			radiuses = new float[] {radius0, radius1};
		}

		internal override void Activate()
		{
			if (shader != null)
				return;
			shader = SKShader.CreateRadialGradient (points[0], radiuses[0],
				colorStops.ToArray(), offsets.ToArray(), tileMode);
		}
	}
}
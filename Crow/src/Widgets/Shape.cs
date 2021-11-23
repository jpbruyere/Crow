// Copyright (c) 2013-2022  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.IO;
using System.Text;

using Drawing2D;

namespace Crow
{
	/// <summary>
	/// String Parser for drawing shape
	/// </summary>
	/// <remarks>
	/// All fields are separated by one or more space. Each statement is composed of one letter followed by 0 to n
	/// double parameters. The following list enumerate available instructions:
	/// - M x y: Move to (x,y) absolute coordinate.
	/// - m x y: Relative move to by x in the horizontal direction and y in the vertical.
	/// - L x y: Line to (x,y)
	/// - l x y: trace line from current point by moving pen by x and y in each direction.
	/// - R x y w h: draw rectangle at (x,y) with width and height equal to w and h.
	/// - C x1 y1 x2 y2 x3 y3: draw bezier curve with current point as first control point, and parameters as others.
	/// - c x1 y1 x2 y2 x3 y3: draw bezier curve with control points relative to current position.
	/// - A x y r a1 a2: draw positive arc at (x,y) with radius r from angle a1 to angle a2.
	/// - N x y r a1 a2: draw negative arc at (x,y) with radius r from angle a1 to angle a2.
	/// - Z: close path
	/// - F: fill path.
	/// - f: fill preserve.
	/// - G: stroke path.
	/// - g: stroke preserve.
	/// - S x: set line width to x.
	/// - O r g b a: set solid color as source with rgba values.
	/// </remarks>
	public class PathParser : StringReader
	{
		public PathParser (string str) : base (str) { }
		char[] buffer = new char[20];
		double readDouble ()
		{
			int length = 0;

			while (Peek () >= 0) {
				buffer[length] = (char)Read ();
				if (buffer[length].IsWhiteSpaceOrNewLine ()) {
					if (length == 0)
						continue;
					else
						break;
				} else if (buffer[length] == ',')
					break;
				length++;
			}
			return double.Parse (buffer.AsSpan(0, length));
		}
		public void Draw (IContext gr, bool measure = false)
		{
			char c;

			try {
				while (Peek () >= 0) {
					c = (char)Read ();
					if (c.IsWhiteSpaceOrNewLine ())
						continue;
					switch (c) {
					case 'M':
						gr.MoveTo (readDouble (), readDouble ());
						break;
					case 'm':
						gr.RelMoveTo (readDouble (), readDouble ());
						break;
					case 'L':
						gr.LineTo (readDouble (), readDouble ());
						break;
					case 'l':
						gr.RelLineTo (readDouble (), readDouble ());
						break;
					case 'R':
						gr.Rectangle (readDouble (), readDouble (), readDouble (), readDouble ());
						break;
					case 'C':
						gr.CurveTo (readDouble (), readDouble (), readDouble (), readDouble (), readDouble (), readDouble ());
						break;
					case 'c':
						gr.RelCurveTo (readDouble (), readDouble (), readDouble (), readDouble (), readDouble (), readDouble ());
						break;
					case 'A':
						gr.Arc (readDouble (), readDouble (), readDouble (), readDouble (), readDouble ());
						break;
					case 'N':
						gr.ArcNegative (readDouble (), readDouble (), readDouble (), readDouble (), readDouble ());
						break;
					case 'Z':
						gr.ClosePath ();
						break;
					case 'f':
						if (measure)
							continue;
						gr.FillPreserve ();
						break;
					case 'g':
						if (measure)
							break;
						gr.StrokePreserve ();
						break;
					case 'F':
						if (measure)
							break;
						gr.Fill ();
						break;
					case 'G':
						if (measure)
							break;
						gr.Stroke ();
						break;
					case 'S':
						gr.LineWidth = readDouble ();
						break;
					case 'O':
						if (measure) {
							readDouble (); readDouble (); readDouble (); readDouble ();
							break;
						}
						gr.SetSource (readDouble (), readDouble (), readDouble (), readDouble ());
						break;
					default:
						throw new Exception ("Invalid character in path string of Shape control");
					}
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine ($"Error parsing path: {ex.Message}");
			}
		}
	}
	/// <summary>
	/// Widget for drawing a shape define with a path expression as defined in the PathParser.
	/// </summary>
	public class Shape : Scalable
	{
		#region CTOR
		protected Shape ()  { }
		public Shape (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		string path;
		double strokeWidth;
		Size size;

		/// <summary>
		/// Path expression, for syntax see 'PathParser'.
		/// </summary>
		public string Path {
			get { return path; }
			set {
				if (path == value)
					return;
				path = value;
				contentSize = default (Size);
				NotifyValueChangedAuto (path);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// Default stroke width, may be overriden by a 'S' command in the path string.
		/// </summary>
		/// <value>The width of the stoke.</value>
		[DefaultValue (1.0)]
		public double StrokeWidth {
			get { return strokeWidth; }
			set {
				if (strokeWidth == value)
					return;
				strokeWidth = value;
				contentSize = default (Size);
				NotifyValueChangedAuto (strokeWidth);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// View box
		/// </summary>
		[DefaultValue ("32,32")]
		public Size Size {
			get { return size; }
			set {
				if (size == value)
					return;
				size = value;
				contentSize = default;
				NotifyValueChangedAuto (size);
				//RegisterForLayouting (LayoutingType.Sizing);
				RegisterForGraphicUpdate ();
			}
		}

		protected override void onDraw (IContext gr)
		{
			base.onDraw (gr);

			if (string.IsNullOrEmpty (path))
				return;

			Rectangle cr = ClientRectangle;
			double widthRatio = 1f, heightRatio = 1f;

			double w = (double)(contentSize.Width == 0 ? size.Width : contentSize.Width);
			double h = (double)(contentSize.Height == 0 ? size.Height : contentSize.Height);

			if (Scaled) {
				widthRatio = cr.Width / w;
				heightRatio = cr.Height / h;
			}

			if (KeepProportions) {
				if (widthRatio < heightRatio)
					heightRatio = widthRatio;
				else
					widthRatio = heightRatio;
			}

			Matrix m = gr.Matrix;
			//gr.Save ();

			gr.Translate (cr.Left, cr.Top);
			gr.Scale (widthRatio, heightRatio);
			gr.Translate ((cr.Width / widthRatio - w) / 2, (cr.Height / heightRatio - h) / 2);

			gr.LineWidth = strokeWidth;
			Foreground.SetAsSource (IFace, gr, cr);

			using (PathParser parser = new PathParser (path))
				parser.Draw (gr);

			//gr.Restore ();
			gr.Matrix = m;
		}


		public override int measureRawSize (LayoutingType lt)
		{
			if ((lt == LayoutingType.Width && contentSize.Width == 0) || (lt == LayoutingType.Height && contentSize.Height == 0)) {
				if (size != default (Size))
					contentSize = size;
				else {
					using (IContext ctx = new Context (IFace.surf)) {
						using (PathParser parser = new PathParser (path))
							parser.Draw (ctx, true);
						Rectangle r = ctx.StrokeExtents ();
						contentSize = new Size (r.Right, r.Bottom);
					}
				}
			}
			return lt == LayoutingType.Width ?
				contentSize.Width + 2 * Margin : contentSize.Height + 2 * Margin;
		}
	}
}


#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Crow
{	
    public class Painter : ScrollingObject
    {
		#region CTOR
		protected Painter () { }
		public Painter (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		public class Point
        {
			public double X;
			public double Y;
		}
		
		public enum DrawMode
		{
			Select,
			Lines,
			Rect,
			Arc,
		}
		DrawMode currentDrawMode = DrawMode.Select;
		public DrawMode CurrentDrawMode {
			get => currentDrawMode;
			set {
				if (value == currentDrawMode)
					return;
				currentDrawMode = value;
				NotifyValueChanged ("CurrentDrawMode", currentDrawMode);
				updateMouseCursor ();
			}
		}

		void updateMouseCursor () {
			if (currentDrawMode == DrawMode.Select)
				MouseCursor = MouseCursor.arrow;
			else
				MouseCursor = MouseCursor.crosshair;
		}

		public double[] Zooms => new double[] { 0.5, 1.0, 2.0, 4.0, 8.0, 16.0 };

		string path;
		double strokeWidth;
		Size size;
		double zoom;
		Fill shapeBackground, shapeForeground;

		[DefaultValue(1.0)]
		public double Zoom {
			get => zoom;
			set {
				if (zoom == value)
					return;
				zoom = value;
				NotifyValueChangedAuto (zoom);
				RegisterForGraphicUpdate ();
				updateMaxScrolls ();
            }
        }

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
		/// View box size in pixel
		/// </summary>
		[DefaultValue ("32,32")]
		public Size Size {
			get { return size; }
			set {
				if (size == value)
					return;
				size = value;				
				NotifyValueChangedAuto (size);
				//RegisterForLayouting (LayoutingType.Sizing);
				RegisterForGraphicUpdate ();
				updateMaxScrolls ();
			}
		}
		
		[DefaultValue ("White")]
		public virtual Fill ShapeBackground {
			get => shapeBackground;
			set {
				if (shapeBackground == value)
					return;
				shapeBackground = value;
				NotifyValueChangedAuto (shapeBackground);
				RegisterForRedraw ();
			}
		}
		[DefaultValue ("Black")]
		public virtual Fill ShapeForeground {
			get => shapeForeground;
			set {
				if (shapeForeground == value)
					return;
				shapeForeground = value;
				NotifyValueChangedAuto (shapeForeground);
				RegisterForRedraw ();
			}
		}

		public override void OnLayoutChanges (LayoutingType layoutType) {
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height)
				NotifyValueChanged ("PageHeight", Slot.Height);
			else if (layoutType == LayoutingType.Width)
				NotifyValueChanged ("PageWidth", Slot.Width);
			else
				return;
			updateMaxScrolls ();
		}
        public override void onMouseEnter (object sender, MouseMoveEventArgs e) {
            base.onMouseEnter (sender, e);
			updateMouseCursor ();
        }
        protected override void onDraw (Context gr) {
			base.onDraw (gr);

			if (string.IsNullOrEmpty (path))
				return;

			Rectangle cr = ClientRectangle;
			
			gr.Save ();
			gr.Translate (-ScrollX, -ScrollY);
			gr.Scale (Zoom, Zoom);

			Rectangle r = new Rectangle (cr.Position, size);
			shapeBackground.SetAsSource (IFace, gr);
			gr.Rectangle (r);
			gr.Fill ();

			gr.LineWidth = strokeWidth;
			ShapeForeground.SetAsSource (IFace, gr, cr);

			using (PathParser parser = new PathParser (path))
				parser.Draw (gr);

			gr.Restore ();
		}

		public override void onMouseWheel (object sender, MouseWheelEventArgs e) {
			if (e.Delta > 0)
				Zoom *= 2.0;
			else
				Zoom /= 2.0;
			e.Handled = true;
            base.onMouseWheel (sender, e);
        }

		void updateMaxScrolls () {
			Rectangle cb = ClientRectangle;
			Size scalledSize = size * Zoom;

			MaxScrollX = scalledSize.Width - cb.Width;
			if (scalledSize.Width > 0)
				NotifyValueChanged ("ChildWidthRatio", (double)cb.Width / scalledSize.Width);

			MaxScrollY = scalledSize.Height - cb.Height;
			if (scalledSize.Height > 0)
				NotifyValueChanged ("ChildHeightRatio", (double)cb.Height / scalledSize.Height);
		}
	}
}

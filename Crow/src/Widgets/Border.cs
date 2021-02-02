// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;
using Crow.Cairo;

namespace Crow
{
	/// <summary>
	/// provide an easy way to get 3d border for buttons
	/// </summary>
	public enum BorderStyle {
		Normal,
		Raised,
		Sunken
	};

	/// <summary>
	/// simple container with border
	/// </summary>
	public class Border : Container
	{
		#region CTOR
		protected Border () {}
		public Border (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region private fields
		int _borderWidth;
		BorderStyle _borderStyle;
		Fill raisedColor = Colors.Grey;
		Fill sunkenColor = Colors.DimGrey;
		#endregion

		#region public properties
		/// <summary>
		/// use to define the colors of the 3d border
		/// </summary>		
		public virtual Fill RaisedColor {
			get { return raisedColor; }
			set {
				if (raisedColor == value)
					return;
				raisedColor = value;
				NotifyValueChangedAuto (raisedColor);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// use to define the colors of the 3d border
		/// </summary>		
		public virtual Fill SunkenColor {
			get { return sunkenColor; }
			set {
				if (sunkenColor == value)
					return;
				sunkenColor = value;
				NotifyValueChangedAuto (sunkenColor);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// border width in pixels
		/// </summary>
		[DefaultValue(1)]
		public virtual int BorderWidth {
			get { return _borderWidth; }
			set {
				_borderWidth = value;
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// allow 3d border effects
		/// </summary>
		[DefaultValue(BorderStyle.Normal)]
		public virtual BorderStyle BorderStyle {
			get { return _borderStyle; }
			set {
				if (_borderStyle == value)
					return;
				_borderStyle = value;
				RegisterForGraphicUpdate ();
			}
		}
		#endregion

		#region GraphicObject override
		[XmlIgnore]public override Rectangle ClientRectangle {
			get {
				Rectangle cb = base.ClientRectangle;
				cb.Inflate (- BorderWidth);
				return cb;
			}
		}

		public override int measureRawSize (LayoutingType lt)
		{
			int tmp = base.measureRawSize (lt);
			return tmp < 0 ? tmp : tmp + 2 * BorderWidth;
		}
		protected override void onDraw (Context gr)
		{
			drawborder2 (gr);

			gr.Save ();
			if (ClipToClientRect) {
				CairoHelpers.CairoRectangle (gr, ClientRectangle, Math.Max (0.0, CornerRadius - Margin));
				gr.Clip ();
			}

			if (child != null)
				child.Paint (gr);
			gr.Restore ();
		}
		void drawborder2(Context gr){
			Rectangle rBack = new Rectangle (Slot.Size);

			//rBack.Inflate (-Margin);
			//			if (BorderWidth > 0)
			//				rBack.Inflate (-BorderWidth / 2);

			Background.SetAsSource (IFace, gr, rBack);
			CairoHelpers.CairoRectangle(gr, rBack, CornerRadius);
			gr.Fill ();


			if (BorderStyle == BorderStyle.Normal) {
				if (BorderWidth > 0) {
					Foreground?.SetAsSource (IFace, gr, rBack);
					CairoHelpers.CairoRectangle (gr, rBack, CornerRadius, BorderWidth);
				}
			} else {
				gr.LineWidth = 1.0;
				if (CornerRadius > 0.0) {
					double radius = CornerRadius;
					if ((radius > rBack.Height / 2.0) || (radius > rBack.Width / 2.0))
						radius = Math.Min (rBack.Height / 2.0, rBack.Width / 2.0);
					gr.SetSource (sunkenColor);
					gr.MoveTo (0.5 + rBack.Left, -0.5 + rBack.Bottom - radius);
					gr.ArcNegative (0.5 + rBack.Left + radius, -0.5 + rBack.Bottom - radius, radius, Math.PI, Math.PI * 0.75);
					gr.MoveTo (0.5 + rBack.Left, -0.5 + rBack.Bottom - radius);
					gr.LineTo (0.5 + rBack.Left, 0.5 + rBack.Top + radius);
					gr.Arc (0.5 + rBack.Left + radius, 0.5 + rBack.Top + radius, radius, Math.PI, Math.PI * 1.5);
					gr.LineTo (-0.5 + rBack.Right - radius, 0.5 + rBack.Top);
					gr.Arc (-0.5 + rBack.Right - radius, 0.5 + rBack.Top + radius, radius, Math.PI * 1.5, Math.PI * 1.75);
					gr.Stroke ();
					if (BorderStyle == BorderStyle.Raised) {
						gr.MoveTo (-1.5 + rBack.Right, 1.5 + rBack.Top + radius);
						gr.ArcNegative (-0.5 + rBack.Right - radius, 0.5 + rBack.Top + radius, radius - 1.0, 0, -Math.PI * 0.25);
						gr.MoveTo (-1.5 + rBack.Right, 1.5 + rBack.Top + radius);
						gr.LineTo (-1.5 + rBack.Right, -1.5 + rBack.Bottom - radius);
						gr.Arc (-0.5 + rBack.Right - radius, -0.5 + rBack.Bottom - radius, radius - 1.0, 0, Math.PI / 2.0);
						gr.LineTo (1.5 + rBack.Left + radius, -1.5 + rBack.Bottom);
						gr.Arc (0.5 + rBack.Left + radius, -0.5 + rBack.Bottom - radius, radius - 1.0, Math.PI / 2.0, Math.PI * 0.75);
						gr.Stroke ();

						gr.SetSource (raisedColor);
						gr.MoveTo (1.5 + rBack.Left, -1.5 + rBack.Bottom - radius);
						gr.ArcNegative (0.5 + rBack.Left + radius, -0.5 + rBack.Bottom - radius, radius - 1.0, Math.PI, Math.PI * 0.75);
						gr.MoveTo (1.5 + rBack.Left, -1.5 + rBack.Bottom - radius);
						gr.LineTo (1.5 + rBack.Left, 1.5 + rBack.Top + radius);
						gr.Arc (0.5 + rBack.Left + radius, 0.5 + rBack.Top + radius, radius - 1.0, Math.PI, Math.PI * 1.5);
						gr.LineTo (-1.5 + rBack.Right - radius, 1.5 + rBack.Top);
						gr.Arc (-0.5 + rBack.Right - radius, 0.5 + rBack.Top + radius, radius - 1.0, Math.PI * 1.5, Math.PI * 1.75);
					} else {
						gr.Stroke ();
						gr.SetSource (raisedColor);
					}
					gr.MoveTo (-0.5 + rBack.Right, 0.5 + rBack.Top + radius);
					gr.ArcNegative (-0.5 + rBack.Right - radius, 0.5 + rBack.Top + radius, radius, 0, -Math.PI * 0.25);
					gr.MoveTo (-0.5 + rBack.Right, 0.5 + rBack.Top + radius);
					gr.LineTo (-0.5 + rBack.Right, -0.5 + rBack.Bottom - radius);
					gr.Arc (-0.5 + rBack.Right - radius, -0.5 + rBack.Bottom - radius, radius, 0, Math.PI / 2.0);
					gr.LineTo (0.5 + rBack.Left + radius, -0.5 + rBack.Bottom);
					gr.Arc (0.5 + rBack.Left + radius, -0.5 + rBack.Bottom - radius, radius, Math.PI / 2.0, Math.PI * 0.75);
					gr.Stroke ();
				} else {
					gr.SetSource (sunkenColor);
					gr.MoveTo (0.5 + rBack.Left, rBack.Bottom);
					gr.LineTo (0.5 + rBack.Left, 0.5 + rBack.Y);
					gr.LineTo (rBack.Right, 0.5 + rBack.Y);
					if (BorderStyle == BorderStyle.Raised) {
						gr.MoveTo (-1.5 + rBack.Right, 2.0 + rBack.Y);
						gr.LineTo (-1.5 + rBack.Right, -1.5 + rBack.Bottom);
						gr.LineTo (2.0 + rBack.Left, -1.5 + rBack.Bottom);
						gr.Stroke ();
						gr.SetSource (raisedColor);
						gr.MoveTo (1.5 + rBack.Left, -1.0 + rBack.Bottom);
						gr.LineTo (1.5 + rBack.Left, 1.5 + rBack.Y);
						gr.LineTo (rBack.Right, 1.5 + rBack.Y);
					} else {
						gr.Stroke ();
						gr.SetSource (raisedColor);
					}
					gr.MoveTo (-0.5 + rBack.Right, 1.5 + rBack.Y);
					gr.LineTo (-0.5 + rBack.Right, -0.5 + rBack.Bottom);
					gr.LineTo (1.0 + rBack.Left, -0.5 + rBack.Bottom);
					gr.Stroke ();
				}
			}
		}
		void drawborder1(Context gr){
			Rectangle rBack = new Rectangle (Slot.Size);

			//rBack.Inflate (-Margin);
			//			if (BorderWidth > 0)
			//				rBack.Inflate (-BorderWidth / 2);

			Background.SetAsSource (IFace, gr, rBack);
			CairoHelpers.CairoRectangle(gr, rBack, CornerRadius);
			gr.Fill ();

			double bw = _borderWidth;
			double crad = CornerRadius;

			if (bw > 0) {
				if (BorderStyle == BorderStyle.Normal)
					Foreground.SetAsSource (IFace, gr, rBack);
				else {
					if (BorderStyle == BorderStyle.Sunken)
						gr.SetSource (raisedColor);
					else
						gr.SetSource (sunkenColor);

					CairoHelpers.CairoRectangle (gr, rBack, crad, bw);

					if (BorderStyle == BorderStyle.Sunken)
						gr.SetSource (sunkenColor);
					else
						gr.SetSource (raisedColor);

					bw /= 2.0;
					rBack.Width -= (int)Math.Round(bw);
					rBack.Height -= (int)Math.Round(bw);
				}

				CairoHelpers.CairoRectangle (gr, rBack, crad, bw);
			}
		}
		#endregion
	}
}


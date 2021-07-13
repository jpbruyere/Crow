// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif
namespace Crow {

public class CircleMeter : Gauge {
		#region CTOR
		protected CircleMeter () {}
		public CircleMeter (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		double startAngle, endAngle;
		int lineWidth, backgroundLineWidth;		
		/// <summary>
		/// Starting andle in degree corresponding to the minimum value
		/// </summary>
		[DefaultValue (0.0)]
		public double StartAngle {
			get => startAngle;
			set {
				if (startAngle == value)
					return;
				startAngle = value;
				NotifyValueChangedAuto (startAngle);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Ending angle in degree corresponding to the maximum value
		/// </summary>
		[DefaultValue (360.0)]
		public double EndAngle {
			get => endAngle;
			set {
				if (endAngle == value)
					return;
				endAngle = value;
				NotifyValueChangedAuto (endAngle);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Line width used to draw the value.
		/// </summary>
		/// <value></value>
		[DefaultValue (10)]
		public int LineWidth {
			get => lineWidth;
			set {
				if (lineWidth == value)
					return;
				lineWidth = value;
				NotifyValueChangedAuto (lineWidth);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Line width used to draw the background arc from start to end angle
		/// </summary>		
		[DefaultValue (10)]
		public int BackgroundLineWidth {
			get => backgroundLineWidth;
			set {
				if (backgroundLineWidth == value)
					return;
				backgroundLineWidth = value;
				NotifyValueChangedAuto (backgroundLineWidth);
				RegisterForRedraw ();
			}
		}
		const double rad = Math.PI * 2.0 / 360.0;
		protected override void onDraw (Context gr) {
			DbgLogger.StartEvent (DbgEvtType.GODraw, this);

			/*Rectangle r = new Rectangle (Slot.Size);

			Background.SetAsSource (IFace, gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill ();*/

			Rectangle r = ClientRectangle;

			double radius = 0.5 * (Math.Min (r.Width, r.Height) - Math.Max (backgroundLineWidth, lineWidth));


			double valueTot = Maximum - Minimum;
			double absValue = Value - Minimum;
			double valRatio = absValue / valueTot;

			double sArad = rad * (startAngle - 90);
			double sErad = rad * (endAngle - 90);


			gr.NewPath ();

			bool clockwise = startAngle < endAngle;
				double valAngle = (sErad - sArad) * valRatio;
				Background?.SetAsSource (IFace, gr);
				gr.LineWidth = backgroundLineWidth;
				if (clockwise)
					gr.Arc (r.CenterD, radius, sArad, sErad);
				else
					gr.ArcNegative (r.CenterD, radius, sArad, sErad);
				gr.Stroke ();
				gr.LineWidth = lineWidth;
				Foreground?.SetAsSource (IFace, gr);
				if (clockwise)
					gr.Arc (r.CenterD, radius, sArad, sArad + valAngle);
				else
					gr.ArcNegative (r.CenterD, radius, sArad, sArad + valAngle);
				gr.Stroke ();				
			/*} else {
				double valAngle = (sArad - sErad) * valRatio;
			}*/

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}

	}
}
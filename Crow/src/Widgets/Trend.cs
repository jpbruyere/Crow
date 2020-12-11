// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Collections.Generic;
using System.ComponentModel;
using Crow.Cairo;

namespace Crow
{
	public class Trend : Widget
	{
		#region private fields
		double minValue, maxValue, lowThreshold, highThreshold;
		Fill lowThresholdFill, highThresholdFill;
		int nbValues;
		List<double> values = new List<double>();
		#endregion

		#region CTOR
		protected Trend () {}
		public Trend (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		public virtual void AddValue (double _value)
		{
			values.Add (_value);
			while (values.Count > nbValues)
				values.RemoveAt (0);
			RegisterForRedraw ();
		}

		[XmlIgnore]public virtual int NewValue {
			set {
				AddValue (value);
			}
		}
		[DefaultValue(400)]
		public virtual int NbValues {
			get { return nbValues; }
			set {
				if (nbValues == value)
					return;

				nbValues = value;
				NotifyValueChangedAuto (minValue);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(0.0)]
		public virtual double Minimum {
			get { return minValue; }
			set {
				if (minValue == value)
					return;

				minValue = value;
				NotifyValueChangedAuto (minValue);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(100.0)]
		public virtual double Maximum
		{
			get { return maxValue; }
			set {
				if (maxValue == value)
					return;

				maxValue = value;
				NotifyValueChangedAuto (maxValue);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(1.0)]
		public virtual double LowThreshold {
			get { return lowThreshold; }
			set {
				if (lowThreshold == value)
					return;
				lowThreshold = value;
				NotifyValueChangedAuto (lowThreshold);
				RegisterForGraphicUpdate ();
			}
		}
		[DefaultValue(80.0)]
		public virtual double HighThreshold {
			get { return highThreshold; }
			set {
				if (highThreshold == value)
					return;
				highThreshold = value;
				NotifyValueChangedAuto (highThreshold);
				RegisterForGraphicUpdate ();
			}
		}
		[DefaultValue("DarkRed")]
		public virtual Fill LowThresholdFill {
			get { return lowThresholdFill; }
			set {
				if (lowThresholdFill == value)
					return;
				lowThresholdFill = value;
				NotifyValueChangedAuto (lowThresholdFill);
				RegisterForRedraw ();
			}
		}
		[DefaultValue("DarkGreen")]
		public virtual Fill HighThresholdFill {
			get { return highThresholdFill; }
			set {
				if (highThresholdFill == value)
					return;
				highThresholdFill = value;
				NotifyValueChangedAuto (highThresholdFill);
				RegisterForRedraw ();
			}
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			if (values.Count == 0)
				return;
			Rectangle r = ClientRectangle;

			int i = values.Count -1;

			double ptrX = (double)r.Right;
			double scaleY = (double)r.Height / (Maximum - Minimum);
			double stepX = (double)r.Width / (double)(nbValues-1);

			gr.LineWidth = 1.0;
			gr.SetDash (new double[]{ 1.0 },0.0);



			LowThresholdFill.SetAsSource (IFace, gr);
			gr.MoveTo (r.Left, r.Bottom - LowThreshold * scaleY);
			gr.LineTo (r.Right, r.Bottom - LowThreshold * scaleY);
//			gr.Rectangle (r.Left, r.Bottom - LowThreshold * scaleY, r.Width, LowThreshold * scaleY);
			gr.Stroke();

			HighThresholdFill.SetAsSource (IFace, gr);
			gr.MoveTo (r.Left, (Maximum - HighThreshold) * scaleY);
			gr.LineTo (r.Right, (Maximum - HighThreshold) * scaleY);
//			gr.Rectangle (r.Left, r.Top, r.Width, (Maximum - HighThreshold) * scaleY);
			gr.Stroke();

			gr.MoveTo (ptrX, values [i] * scaleY);

			Foreground.SetAsSource (IFace, gr);
			gr.SetDash (new double[]{ }, 0.0);

			while (i >= 0) {
					gr.LineTo (ptrX, r.Bottom - values [i] * scaleY);
				ptrX -= stepX;
				i--;
			}
			gr.Stroke ();
		}
	}
}


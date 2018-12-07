//
// Trend.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using Cairo;

namespace Crow
{
	public class Trend : GraphicObject
	{
		#region private fields
		double minValue, maxValue, lowThreshold, highThreshold;
		Fill lowThresholdFill, highThresholdFill;
		int nbValues;
		List<double> values = new List<double>();
		#endregion



		public virtual void AddValue(double _value)
		{
			values.Add (_value);
			while (values.Count > nbValues)
				values.RemoveAt (0);
			RegisterForRedraw ();
		}
		#region CTOR
		protected Trend () : base()
		{
		}
		#endregion
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
				NotifyValueChanged ("NbValues", minValue);
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
				NotifyValueChanged ("Minimum", minValue);
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
				NotifyValueChanged ("Maximum", maxValue);
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
				NotifyValueChanged ("LowThreshold", lowThreshold);
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
				NotifyValueChanged ("HighThreshold", highThreshold);
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
				NotifyValueChanged ("LowThresholdFill", lowThresholdFill);
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
				NotifyValueChanged ("HighThresholdFill", highThresholdFill);
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



			LowThresholdFill.SetAsSource (gr);
			gr.MoveTo (r.Left, r.Bottom - LowThreshold * scaleY);
			gr.LineTo (r.Right, r.Bottom - LowThreshold * scaleY);
//			gr.Rectangle (r.Left, r.Bottom - LowThreshold * scaleY, r.Width, LowThreshold * scaleY);
			gr.Stroke();

			HighThresholdFill.SetAsSource (gr);
			gr.MoveTo (r.Left, (Maximum - HighThreshold) * scaleY);
			gr.LineTo (r.Right, (Maximum - HighThreshold) * scaleY);
//			gr.Rectangle (r.Left, r.Top, r.Width, (Maximum - HighThreshold) * scaleY);
			gr.Stroke();

			gr.MoveTo (ptrX, values [i] * scaleY);

			Foreground.SetAsSource (gr);
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


//
//  Trend.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;

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

		public Trend ()
		{
		}
		[XmlIgnore]public virtual int NewValue {
			set {
				AddValue (value);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(100)]
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
		[XmlAttributeAttribute()][DefaultValue(0.0)]
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
		[XmlAttributeAttribute()][DefaultValue(100.0)]
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
		[XmlAttributeAttribute()][DefaultValue(20.0)]
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
		[XmlAttributeAttribute()][DefaultValue(80.0)]
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
		[XmlAttributeAttribute()][DefaultValue("DarkRed")]
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
		[XmlAttributeAttribute()][DefaultValue("DarkGreen")]
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
		protected override void onDraw (Cairo.Context gr)
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


// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


using System;
using System.ComponentModel;

namespace Crow {
	public class NumericControl : TemplatedControl
	{
		#region CTOR
		protected NumericControl () {}
		public NumericControl (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region protected fields
		protected double actualValue, minValue, maxValue, smallStep, bigStep;
		protected int decimals;
		#endregion

		#region public properties
		[DefaultValue(2)]
		public int Decimals
		{
			get { return decimals; }
			set
			{
				if (value == decimals)
					return;
				decimals = value;
				NotifyValueChangedAuto (decimals);
				RegisterForGraphicUpdate();
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
		public virtual double SmallIncrement
		{
			get { return smallStep; }
			set {
				if (smallStep == value)
					return;

				smallStep = value;
				NotifyValueChangedAuto (smallStep);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(5.0)]
		public virtual double LargeIncrement
		{
			get { return bigStep; }
			set {
				if (bigStep == value)
					return;

				bigStep = value;
				NotifyValueChangedAuto (bigStep);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(0.0)]
		public virtual double Value
		{
			get { return actualValue; }
			set
			{
				if (value == actualValue)
					return;

				if (value < minValue)
					actualValue = minValue;
				else if (value > maxValue)
					actualValue = maxValue;
				else                    
					actualValue = value;

				actualValue = Math.Round (actualValue, decimals);

				NotifyValueChangedAuto (actualValue);
				RegisterForGraphicUpdate();
			}
		}
		#endregion

	}
}


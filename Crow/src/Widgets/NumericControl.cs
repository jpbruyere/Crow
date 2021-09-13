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
			get => decimals;
			set
			{
				if (value == decimals)
					return;
				decimals = value;
				NotifyValueChangedAuto (decimals);
				registerUpdate ();
			}
		}
		[DefaultValue(0.0)]
		public virtual double Minimum {
			get => minValue;
			set {
				if (minValue == value)
					return;

				minValue = value;
				NotifyValueChangedAuto (minValue);
				registerUpdate ();
			}
		}
		[DefaultValue(100.0)]
		public virtual double Maximum
		{
			get => maxValue;
			set {
				if (maxValue == value)
					return;

				maxValue = value;
				NotifyValueChangedAuto (maxValue);

				if (Value > maxValue)
					Value = maxValue;

				registerUpdate ();
			}
		}
		[DefaultValue(1.0)]
		public virtual double SmallIncrement
		{
			get => smallStep;
			set {
				if (smallStep == value)
					return;

				smallStep = value;
				NotifyValueChangedAuto (smallStep);

				if (Value < minValue)
					Value = minValue;

				registerUpdate ();
			}
		}
		[DefaultValue(5.0)]
		public virtual double LargeIncrement
		{
			get => bigStep;
			set {
				if (bigStep == value)
					return;

				bigStep = value;
				NotifyValueChangedAuto (bigStep);
				registerUpdate ();
			}
		}
		[DefaultValue(0.0)]
		public virtual double Value
		{
			get => actualValue;
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
				registerUpdate ();
			}
		}
		#endregion

		protected virtual void registerUpdate ()
			=> RegisterForRedraw ();

		protected virtual void onUp (object sender, MouseButtonEventArgs e)
		{
			if (IFace.Ctrl)
				Value += SmallIncrement;
			else
				Value += LargeIncrement;
		}
		protected virtual void onDown (object sender, MouseButtonEventArgs e)
		{
			if (IFace.Ctrl)
				Value -= SmallIncrement;
			else
				Value -= LargeIncrement;
		}
	}
}


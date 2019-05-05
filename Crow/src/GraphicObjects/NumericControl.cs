﻿//
// NumericControl.cs
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
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	public abstract class NumericControl : TemplatedControl
	{
		#region CTOR
		protected NumericControl () : base(){}
		public NumericControl (Interface iface) : base(iface)
		{
		}
//		public NumericControl(double minimum, double maximum, double step)
//			: base()
//		{
//		}
		#endregion

		#region private fields
		double _actualValue, minValue, maxValue, smallStep, bigStep;
		int _decimals;
		#endregion

		#region public properties
		[DefaultValue(2)]
		public int Decimals
		{
			get { return _decimals; }
			set
			{
				if (value == _decimals)
					return;
				_decimals = value;
				NotifyValueChanged("Decimals",  _decimals);
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
		public virtual double SmallIncrement
		{
			get { return smallStep; }
			set {
				if (smallStep == value)
					return;

				smallStep = value;
				NotifyValueChanged ("SmallIncrement", smallStep);
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
				NotifyValueChanged ("LargeIncrement", bigStep);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(0.0)]
		public double Value
		{
			get { return _actualValue; }
			set
			{
				if (value == _actualValue)
					return;

				if (value < minValue)
					_actualValue = minValue;
				else if (value > maxValue)
					_actualValue = maxValue;
				else                    
					_actualValue = value;

				_actualValue = Math.Round (_actualValue, _decimals);

				NotifyValueChanged("Value",  _actualValue);
				RegisterForGraphicUpdate();
			}
		}
		#endregion

	}
}


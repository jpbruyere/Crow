using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
{
	public abstract class NumericControl : TemplatedControl
	{
		#region CTOR
		public NumericControl () : base()
		{
		}
		public NumericControl(double minimum, double maximum, double step)
			: base()
		{
			minValue = minimum;
			maxValue = maximum;
			smallStep = step;
			bigStep = step * 5;
		}
		#endregion

		#region event handlers
		public EventHandler<ValueChangeEventArgs> ValueChanged;

		public virtual void onValueChanged(object sender, ValueChangeEventArgs e)
		{			
			ValueChanged.Raise (sender, e);
		}
		#endregion

		#region private fields
		double _actualValue, minValue, maxValue, smallStep, bigStep;
		#endregion

		#region public properties
		[XmlAttributeAttribute()][DefaultValue(0.0)]
		public virtual double Minimum {
			get { return minValue; }
			set {
				if (minValue == value)
					return;

				minValue = value;

			}
		}
		[XmlAttributeAttribute()][DefaultValue(10.0)]
		public virtual double Maximum
		{
			get { return maxValue; }
			set {
				if (maxValue == value)
					return;

				maxValue = value;

			}
		}
		[XmlAttributeAttribute()][DefaultValue(0.5)]
		public virtual double SmallIncrement
		{
			get { return smallStep; }
			set {
				if (smallStep == value)
					return;

				smallStep = value;

			}
		}
		[XmlAttributeAttribute()][DefaultValue(2.0)]
		public virtual double LargeIncrement
		{
			get { return bigStep; }
			set {
				if (bigStep == value)
					return;

				bigStep = value;

			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
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

				onValueChanged(this,new ValueChangeEventArgs("Value", Convert.ToInt32( _actualValue)));
				registerForGraphicUpdate();
			}
		}
		#endregion

	}
}


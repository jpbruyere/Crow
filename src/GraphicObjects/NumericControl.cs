using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
{
	public class NumericControl : GraphicObject
	{
		#region CTOR
		public NumericControl () : base()
		{
		}
		public NumericControl(double minimum, double maximum, double step)
			: base()
		{
			_minValue = minimum;
			_maxValue = maximum;
			_smallStep = step;
			_bigStep = step * 5;
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
		double _actualValue, _minValue, _maxValue, _smallStep, _bigStep;
		#endregion

		#region public properties
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual double Minimum {
			get { return _minValue; }
			set {
				if (_minValue == value)
					return;

				_minValue = value;

			}
		}
		[XmlAttributeAttribute()][DefaultValue(10.0)]
		public virtual double Maximum
		{
			get { return _maxValue; }
			set {
				if (_maxValue == value)
					return;

				_maxValue = value;

			}
		}
		[XmlAttributeAttribute()][DefaultValue(0.5)]
		public virtual double SmallIncrement
		{
			get { return _smallStep; }
			set {
				if (_smallStep == value)
					return;

				_smallStep = value;

			}
		}
		[XmlAttributeAttribute()][DefaultValue(2.0)]
		public virtual double LargeIncrement
		{
			get { return _bigStep; }
			set {
				if (_bigStep == value)
					return;

				_bigStep = value;

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

				Decimal oldV = (Decimal)_actualValue;

				if (value < _minValue)
					_actualValue = _minValue;
				else if (value > _maxValue)
					_actualValue = _maxValue;
				else                    
					_actualValue = value;

				onValueChanged(this,new ValueChangeEventArgs(oldV,(Decimal)_actualValue));
				registerForGraphicUpdate();
			}
		}
		#endregion

	}
}


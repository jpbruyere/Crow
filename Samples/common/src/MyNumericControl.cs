//Tutorial
using Crow;

namespace MyNumericControl {
	public class MyNumericControl : TemplatedControl {
		protected double actualValue, minValue, maxValue;
		public double Minimum {
			get => minValue;
			set {
				if (minValue == value)
					return;
				minValue = value;
				if (Value < minValue)
					Value = minValue;
				NotifyValueChangedAuto (minValue);
			}
		}
		public double Maximum {
			get => maxValue;
			set {
				if (maxValue == value)
					return;
				maxValue = value;
				NotifyValueChangedAuto (maxValue);

				if (Value > maxValue)
					Value = maxValue;

			}
		}
		public virtual double Value	{
			get => actualValue;
			set	{
				if (actualValue == value)
					return;
				if (value < minValue)
					actualValue = minValue;
				else if (value > maxValue)
					actualValue = maxValue;
				else
					actualValue = value;
				NotifyValueChangedAuto (actualValue);
			}
		}
	}
}
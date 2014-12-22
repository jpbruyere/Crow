using System;

namespace go
{
	public class ValueChangeEventArgs: EventArgs
	{
		public Decimal OldValue;
		public Decimal NewValue;

		public ValueChangeEventArgs (Decimal _oldValue, Decimal _newValue) : base()
		{
			OldValue = _oldValue;
			NewValue = _newValue;
		}
	}
}


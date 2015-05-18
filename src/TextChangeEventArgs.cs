using System;

namespace go
{
	public class ValueChangeEventArgs: EventArgs
	{
		public string MemberName;
		public object OldValue;
		public object NewValue;


		public ValueChangeEventArgs (string _memberName, object _oldValue, object _newValue) : base()
		{
			MemberName = _memberName;
			OldValue = _oldValue;
			NewValue = _newValue;
		}
	}
}


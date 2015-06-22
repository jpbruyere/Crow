using System;

namespace go
{
	public class ValueChangeEventArgs: EventArgs
	{
		public string MemberName;
		public object NewValue;


		public ValueChangeEventArgs (string _memberName, object _newValue) : base()
		{
			MemberName = _memberName;
			NewValue = _newValue;
		}
	}
}


using System;

namespace go
{
	public class SelectionChangeEventArgs: EventArgs
	{		
		public object NewValue;


		public SelectionChangeEventArgs (object _newValue) : base()
		{
			NewValue = _newValue;
		}
	}
}


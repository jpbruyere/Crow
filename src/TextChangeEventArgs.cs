using System;

namespace Crow
{
	public class TextChangeEventArgs: EventArgs
	{
		public String Text;

		public TextChangeEventArgs (string _newValue) : base()
		{
			Text = _newValue;
		}
	}
}


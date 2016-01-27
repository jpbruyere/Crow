using System;

namespace Crow
{
	public class LayoutChangeEventArgs: EventArgs
	{
		public LayoutingType  LayoutType;

		public LayoutChangeEventArgs (LayoutingType  _layoutType) : base()
		{
			LayoutType = _layoutType;
		}
	}
}


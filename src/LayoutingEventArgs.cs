using System;

namespace Crow
{
	public class LayoutingEventArgs: EventArgs
	{
		public LayoutingType  LayoutType;

		public LayoutingEventArgs (LayoutingType  _layoutType) : base()
		{
			LayoutType = _layoutType;
		}
	}
}


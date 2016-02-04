using System;
using System.Collections.Generic;

namespace Crow
{
	public interface ILayoutable
	{
		ILayoutable Parent { get; set; }

		Rectangle ClientRectangle { get; }
		Rectangle getSlot();
		Rectangle getBounds();

		IGOLibHost HostContainer { get; }

		List<LayoutingQueueItem> RegisteredLQIs { get; }
		void RegisterForLayouting(int layoutType);
		void UpdateLayout(LayoutingType layoutType);


		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


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

		List<LinkedListNode<LayoutingQueueItem>> RegisteredLQINodes { get; }
		void RegisterForLayouting(LayoutingType layoutType);
		void UpdateLayout(LayoutingType layoutType);


		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


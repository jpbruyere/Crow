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

		LayoutingType RegisteredLayoutings { get; set; }
		void RegisterForLayouting(LayoutingType layoutType);
		bool UpdateLayout(LayoutingType layoutType);


		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


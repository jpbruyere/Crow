using System;

namespace Crow
{
	public interface ILayoutable
	{
		ILayoutable Parent { get; set; }

		Rectangle ClientRectangle { get; }
		Rectangle getSlot();
		Rectangle getBounds();

		IGOLibHost TopContainer { get; }

		void RegisterForLayouting(int layoutType);
		void UpdateLayout(LayoutingType layoutType);


		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


using System;

namespace go
{
	public interface ILayoutable
	{
		ILayoutable Parent { get; set; }

		Rectangle ClientRectangle { get; }
		Rectangle getSlot();
		Rectangle getBounds();

		IGOLibHost TopContainer { get; }

		void InvalidateLayout ();
		void UpdateLayout(LayoutingType layoutType);


		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


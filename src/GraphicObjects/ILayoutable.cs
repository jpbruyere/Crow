using System;

namespace go
{
	public interface ILayoutable
	{
		ILayoutable Parent { get; set; }

		bool SizeIsValid { get; set; }
		bool WIsValid { get; set; }
		bool HIsValid { get; set; }
		bool PositionIsValid { get; set; }
		bool XIsValid { get; set; }
		bool YIsValid { get; set; }
		bool LayoutIsValid { get; set; }

		Rectangle ClientRectangle { get; }
		Rectangle getSlot();
		Rectangle getBounds();

		IGOLibHost TopContainer { get; }

		void InvalidateLayout ();


		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


using System;
using System.Collections.Generic;

namespace Crow
{
	public interface ILayoutable
	{
		/// <summary> Unsuccessfull UpdateLayout and requeueing count </summary>
		int LayoutingTries { get; set; }
		/// <summary> Parent in the graphic tree </summary>
		ILayoutable Parent { get; set; }
		/// <summary> The logical parent (used mainly for bindings) as opposed
		///  to the parent in the graphic tree </summary>
		ILayoutable LogicalParent { get; set; }

		Rectangle ClientRectangle { get; }
		Rectangle getSlot();
		Rectangle getBounds();

		IGOLibHost HostContainer { get; }

		LayoutingType RegisteredLayoutings { get; set; }
		void RegisterForLayouting(LayoutingType layoutType);
		void RegisterClip(Rectangle clip);
		bool UpdateLayout(LayoutingType layoutType);


		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


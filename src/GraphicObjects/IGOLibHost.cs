using System;
using System.Collections.Generic;

namespace go
{
	public interface IGOLibHost
	{
		Rectangles redrawClip { get; set; }
		List<GraphicObject> gobjsToRedraw { get; }
		GraphicObject activeWidget { get; set; }
		GraphicObject hoverWidget { get; set; }
		GraphicObject FocusedWidget { get; set; }
		void AddWidget (GraphicObject g);
		void DeleteWidget(GraphicObject g);
	}
}


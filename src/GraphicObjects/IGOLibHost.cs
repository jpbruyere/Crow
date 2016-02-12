using System;
using System.Collections.Generic;

namespace Crow
{
	public interface IGOLibHost
	{
		List<GraphicObject> gobjsToRedraw { get; }
		GraphicObject activeWidget { get; set; }
		GraphicObject hoverWidget { get; set; }
		GraphicObject FocusedWidget { get; set; }
		void AddWidget (GraphicObject g);
		void DeleteWidget(GraphicObject g);
		void PutOnTop (GraphicObject g);
		void Quit ();
	}
}


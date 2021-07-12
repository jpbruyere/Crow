// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;

namespace Crow
{
	public interface ILayoutable
	{
		/// <summary> Parent in the graphic tree </summary>
		ILayoutable Parent { get; set; }
		/// <summary> The logical parent (used mainly for bindings) as opposed
		///  to the parent in the graphic tree </summary>
		ILayoutable LogicalParent { get; set; }

		Rectangle ClientRectangle { get; }
		Rectangle getSlot();

		bool ArrangeChildren { get; }
		LayoutingType RegisteredLayoutings { get; set; }
		LayoutingType RequiredLayoutings { get; set; }
		void ChildrenLayoutingConstraints(ILayoutable layoutable, ref LayoutingType layoutType);
		void RegisterForLayouting(LayoutingType layoutType);
		void RegisterClip(Rectangle clip);
		bool UpdateLayout(LayoutingType layoutType);
		bool PointIsIn(ref Point m);

		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


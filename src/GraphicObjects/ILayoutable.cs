//
// ILayoutable.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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

		Interface IFace { get; }

		Rectangle ClientRectangle { get; }
		Rectangle getSlot();

		bool ArrangeChildren { get; }
		LayoutingType RegisteredLayoutings { get; set; }
		void RegisterForLayouting(LayoutingType layoutType);
		void RegisterClip(Rectangle clip);
		bool UpdateLayout(LayoutingType layoutType);
		bool PointIsIn(ref Point m);

		Rectangle ContextCoordinates(Rectangle r);
		Rectangle ScreenCoordinates (Rectangle r);

	}
}


//
// LayoutingEventArgs.cs
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

namespace Crow
{
	public class DragDropEventArgs: EventArgs
	{
		/// <summary>
		/// Source of the drag and drop operation
		/// </summary>
		public GraphicObject DragSource;
		/// <summary>
		/// Target of the drag and drop operation
		/// </summary>
		public GraphicObject DropTarget;

		//public DragDropEventArgs (GraphicObject source, GraphicObject target = null) : base()
		public DragDropEventArgs (GraphicObject source = null, GraphicObject target = null) : base()
		{
			DragSource = source;
			DropTarget = target;
		}

		public override string ToString ()
		{
			return string.Format ("{0} => {1}", DragSource,DropTarget);
		}
	}
}


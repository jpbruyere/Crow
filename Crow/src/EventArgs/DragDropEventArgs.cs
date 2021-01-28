// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow
{
	public class DragDropEventArgs: EventArgs
	{
		/// <summary>
		/// Source of the drag and drop operation
		/// </summary>
		public Widget DragSource;
		/// <summary>
		/// Target of the drag and drop operation
		/// </summary>
		public Widget DropTarget;

		/// <summary>
		/// Create a new instance of DragDropEventArgs.
		/// </summary>
		/// <param name="source">the widget instance source of the event</param>
		/// <param name="target">the target widget of the event</param>
		public DragDropEventArgs (Widget source = null, Widget target = null) : base()
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


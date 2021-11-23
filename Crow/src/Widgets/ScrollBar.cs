// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;
using Drawing2D;

namespace Crow
{
	/// <summary>
	/// templeted numeric control
	/// </summary>
	public class ScrollBar : Slider
	{
		#region CTOR
		protected ScrollBar () {}
		public ScrollBar(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		double cursorRatio;
		/// <summary>
		/// Ratio of CusorSize / CursorContainerSize, -1 if not in use.
		/// </summary>
		[DefaultValue(-1.0)]
		public double CursorRatio {
			get => cursorRatio;
			set {
				if (cursorRatio == value)
					return;
				if (double.IsFinite(value))
					cursorRatio = value;
				else
					cursorRatio = -1;
				updateCursor ();
			}
        }

		void updateCursor () {
			if (cursorRatio < 0 || !double.IsFinite(cursorRatio))
				return;
			ILayoutable l = cursor?.Parent;
			if (l == null)
				return;
			Rectangle r = cursor.Parent.ClientRectangle;
			if (Orientation == Orientation.Horizontal)
				CursorSize = (int)(cursorRatio * r.Width);
			else
				CursorSize = (int)(cursorRatio * r.Height);
		}
        protected override void HandleCursorContainerLayoutChanged (object sender, LayoutingEventArgs e) {
            base.HandleCursorContainerLayoutChanged (sender, e);
			updateCursor ();
        }
    }
}

// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;

using Drawing2D;

namespace Crow
{	public class TestWidget : Widget
	{
		#region CTOR
		protected TestWidget () {}
		public TestWidget (Interface iface, string style = null) : base (iface, style) { }
        #endregion
        protected override void onDraw(IContext gr)
        {
            base.onDraw(gr);
			Rectangle rBack = new Rectangle (Slot.Size);
			gr.SetSource(Foreground);
			rBack.Position = new Point(-100,-100);
			gr.Rectangle(rBack);
			gr.FillPreserve();
			gr.Stroke();
        }
	}
}


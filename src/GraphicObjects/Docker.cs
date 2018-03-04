//
// DocksView.cs
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
using System.Xml.Serialization;
using System.ComponentModel;
using Crow.IML;
using System.Collections.Generic;
using System.Diagnostics;

namespace Crow
{
	public class Docker : Group
	{		
		#region CTOR
		static Docker () {
		}
		public Docker () : base ()
		{
		}
		#endregion

		int dockingThreshold;

		public DockStack SubStack = null;
		public GraphicObject CenterDockedObj = null;

		[XmlAttributeAttribute][DefaultValue(10)]
		public virtual int DockingThreshold {
			get { return dockingThreshold; }
			set {
				if (dockingThreshold == value)
					return;
				dockingThreshold = value; 
				NotifyValueChanged ("DockingThreshold", dockingThreshold);

			}
		}

		protected override void onInitialized (object sender, EventArgs e)
		{
			base.onInitialized (sender, e);
			CenterDockedObj = new GraphicObject (IFace) { IsEnabled = false };
		}

		public override void AddChild (GraphicObject g)
		{				
			base.AddChild (g);
			g.LogicalParent = this;
		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{			
			if (IFace.DragAndDropOperation?.DragSource as DockWindow != null) {
				DockWindow dw = IFace.DragAndDropOperation.DragSource as DockWindow;
				if (!dw.IsDocked)
					dw.MoveAndResize (e.XDelta, e.YDelta);
				if (SubStack == null) {				
					SubStack = new DockStack (IFace);
					InsertChild (0, SubStack);
					SubStack.LogicalParent = this;
				}
			}
			base.onMouseMove (sender, e);
		}
	}
}


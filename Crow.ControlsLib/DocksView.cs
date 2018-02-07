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
using System.Collections.Generic;

namespace Crow
{
	public class DocksView : Group
	{
		List<DockingView> childViews = new List<DockingView>();
		GenericStack rootStack = null;

		public override void AddChild (GraphicObject g)
		{
			DockingView dv = g as DockingView;
			if (g == null)
				throw new Exception ("DocksView accept only DockingView as child");
			base.AddChild (g);
			childViews.Add (dv);
			dv.docker = this;
		}
		public override void RemoveChild (GraphicObject child)
		{
			DockingView dv = child as DockingView;
			if (child == null)
				throw new Exception ("DocksView accept only DockingView as child");
			base.RemoveChild (child);
			childViews.Remove (dv);
			dv.docker = this;
		}
		public void Dock (DockingView dv, Alignment pos){
			switch (pos) {
			case Alignment.Top:
				if (rootStack?.Orientation != Orientation.Vertical)
				this.Width = Measure.Stretched;
				break;
			case Alignment.Bottom:
				this.Width = Measure.Stretched;
				break;
			case Alignment.Left:
				this.Height = Measure.Stretched;
				break;
			case Alignment.Right:
				this.Height = Measure.Stretched;
				break;
			}
		}
		public void Undock (DockingView dv){
		}

		int dockingThreshold;

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
	}
}


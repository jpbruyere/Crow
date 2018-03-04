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

		//GenericStack rootStack = null;
		public DockStack SubStack = null;
		int dockingThreshold;

		List<DockWindow> windows = new List<DockWindow>();

		public override void AddChild (GraphicObject g)
		{				
			base.AddChild (g);
			g.LogicalParent = this;
		}

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

		//bool showDockingTarget = false;
		//Alignment dockingDirection = Alignment.Center;
		//int dockingDiv = 8;
		//bool showDock = false;

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
		/*
		protected override void onDragEnter (object sender, DragDropEventArgs e)
		{
			base.onDragEnter (sender, e);
			showDock = true;
			Console.WriteLine ("showdock=" + showDock);
		}
		protected override void onDragLeave (object sender, DragDropEventArgs e)
		{
			base.onDragLeave (sender, e);
			showDock = false;
			Console.WriteLine ("showdock=" + showDock);
		}

		protected override void onDraw (Cairo.Context gr)
		{
			gr.Save ();

			Rectangle rBack = new Rectangle (Slot.Size);

			Background.SetAsSource (gr, rBack);
			CairoHelpers.CairoRectangle (gr, rBack, CornerRadius);
			gr.Fill ();

			if (ClipToClientRect) {
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			childrenRWLock.EnterReadLock ();

			foreach (GraphicObject g in Children) {
//				if (IFace.DragAndDropOperation?.DragSource == g)
//					continue;
				g.Paint (ref gr);
			}

			childrenRWLock.ExitReadLock ();

			if (showDockingTarget) {
				Rectangle r;
				if (stack == null)
					r = ClientRectangle;
				else
					r = stack.ClientRectangle;
				
				switch (dockingDirection) {
				case Alignment.Top:
					gr.Rectangle (r.Left, r.Top, r.Width, r.Height / dockingDiv);
					break;
				case Alignment.Bottom:
					gr.Rectangle (r.Left, r.Bottom - r.Height / dockingDiv, r.Width, r.Height / dockingDiv);
					break;
				case Alignment.Left:
					gr.Rectangle (r.Left, r.Top, r.Width / dockingDiv, r.Height);
					break;
				case Alignment.Right:
					gr.Rectangle (r.Right - r.Width / dockingDiv, r.Top, r.Width / dockingDiv, r.Height);
					break;
				}
				gr.LineWidth = 1;
				gr.SetSourceRGBA (0.4, 0.4, 0.9, 0.4);
				gr.FillPreserve ();
				gr.SetSourceRGBA (0.9, 0.9, 1.0, 0.8);
				gr.Stroke ();
			}

			gr.Restore ();	
		}
*/

		/*
		public void Dock(DockWindow dw){
			checkInstantiators();

			if (dockingDirection == Alignment.Center)
				return;
			
			lock (IFace.UpdateMutex) {

				Splitter splitter = instSplit.CreateInstance<Splitter> ();
				Rectangle r = ClientRectangle;

				if (stack == null) {
					stack = instStack.CreateInstance<GenericStack> ();
					this.AddChild (stack);
					this.putWidgetOnBottom (stack);
				} 
				 
				int vTreshold = r.Height / dockingDiv;
				int hTreshold = r.Width / dockingDiv;

				switch (dockingDirection) {
				case Alignment.Top:						
					dw.Height = vTreshold;
					stack.Orientation = Orientation.Vertical;
					if (stack.Children.Count == 0) {
						stack.AddChild (dw);
						stack.AddChild (splitter);
						stack.AddChild (instSpacer.CreateInstance ());
					} else {
						stack.AddChild (splitter);
						stack.AddChild (dw);
					}
					break;
				case Alignment.Bottom:
					dw.Height = vTreshold;
					stack.Orientation = Orientation.Vertical;
					if (stack.Children.Count == 0) {
						stack.AddChild (instSpacer.CreateInstance ());
						stack.AddChild (splitter);
						stack.AddChild (dw);
					} else {
						stack.InsertChild (0, dw);
						stack.InsertChild (0, splitter);
					}
					break;
				case Alignment.Left:
					dw.Width = hTreshold;
					stack.Orientation = Orientation.Horizontal;
					if (stack.Children.Count == 0) {
						stack.AddChild (dw);
						stack.AddChild (splitter);
						stack.AddChild (instSpacer.CreateInstance ());
					} else {
						stack.AddChild (splitter);
						stack.AddChild (dw);
					}
					break;
				case Alignment.Right:
					dw.Width = hTreshold;
					stack.Orientation = Orientation.Horizontal;
					if (stack.Children.Count == 0) {
						stack.AddChild (instSpacer.CreateInstance ());
						stack.AddChild (splitter);
						stack.AddChild (dw);
					} else {
						stack.InsertChild (0, dw);
						stack.InsertChild (0, splitter);
					}
					break;
				}
			}
		}*/
	}
}


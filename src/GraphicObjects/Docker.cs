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

namespace Crow
{
	public class Docker : Group
	{
		static Instantiator instStack, instSplit, instSpacer;
		#region CTOR
		static Docker () {
			instStack = Instantiator.CreateFromImlFragment(@"<GenericStack Background='Blue' AllowDrop='true' DragEnter='onStackDragEnter'/>");
			instSplit = Instantiator.CreateFromImlFragment(@"<Splitter/>");
			instSpacer = Instantiator.CreateFromImlFragment(@"<GraphicObject Background='Red' IsEnabled='false'/>");
		}
		public Docker () : base ()
		{
		}
		#endregion

		GenericStack rootStack = null;

		public override void AddChild (GraphicObject g)
		{
			DockWindow dw = g as DockWindow;
			if (dw == null)
				throw new Exception ("Docker accept only DockWindow as child");
			
			base.AddChild (g);

			dw.LogicalParent = this;
		}
		public override void RemoveChild (GraphicObject child)
		{
			child.LogicalParent = null;


			base.RemoveChild (child);
		}

		GenericStack mainStack = null;
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

		bool showDockingTarget = false;
		Alignment dockingDirection = Alignment.Center;
		int dockingDiv = 5;

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{			
			if (CurrentInterface.DragAndDropOperation?.DragSource as DockWindow != null) {
				DockWindow dw = CurrentInterface.DragAndDropOperation?.DragSource as DockWindow;
				if (CurrentInterface.DragAndDropOperation.DragSource.Parent == this && !dw.IsDocked)
					dw.MoveAndResize (e.XDelta, e.YDelta);

				Rectangle r = ClientRectangle;
				int vTreshold = r.Height / dockingDiv;
				int hTreshold = r.Width / dockingDiv;


				bool showDock = true;

				if (dw.Slot.Left < hTreshold)
					dockingDirection = Alignment.Left;
				else if (dw.Slot.Right > r.Right - hTreshold)
					dockingDirection = Alignment.Right;
				else if (dw.Slot.Top < vTreshold)
					dockingDirection = Alignment.Top;
				else if (dw.Slot.Bottom > r.Bottom - vTreshold)
					dockingDirection = Alignment.Bottom;
				else {
					dockingDirection = Alignment.Center;
					showDock = false;
				}

				showDockingTarget = showDock;

				RegisterForGraphicUpdate ();

				//System.Diagnostics.Debug.WriteLine ("Dock: {0}", dockingDirection);
			} else
				showDockingTarget = false;

			base.onMouseMove (sender, e);
		}
		protected override void onDragEnter (object sender, DragDropEventArgs e)
		{
			base.onDragEnter (sender, e);
		}
		void onStackDragEnter (object sender, DragDropEventArgs e)
		{
			base.onDragEnter (sender, e);

			mainStack = sender as GenericStack;
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

			lock (Children) {
				foreach (GraphicObject g in Children) {
					if (CurrentInterface.DragAndDropOperation?.DragSource == g)
						continue;
					g.Paint (ref gr);
				}
			}

			if (showDockingTarget) {
				Rectangle r;
				if (mainStack == null)
					r = ClientRectangle;
				else
					r = mainStack.ClientRectangle;
				
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

			if (CurrentInterface.DragAndDropOperation != null)
				CurrentInterface.DragAndDropOperation.DragSource.Paint (ref gr);


			gr.Restore ();	
		}

		public void Dock(DockWindow dw){
			if (dockingDirection == Alignment.Center)
				return;
			lock (CurrentInterface.UpdateMutex) {

				Splitter splitter = instSplit.CreateInstance<Splitter> (CurrentInterface);

				dw.Resizable = false;
				dw.Left = dw.Top = 0;
				this.RemoveChild (dw);

				Rectangle r;
				if (mainStack == null) {
					mainStack = instStack.CreateInstance<GenericStack> (CurrentInterface);
					this.AddChild (mainStack);
					this.putWidgetOnBottom (mainStack);
					r = ClientRectangle;
				} else
					r = mainStack.ClientRectangle;				
				 
				int vTreshold = r.Height / dockingDiv;
				int hTreshold = r.Width / dockingDiv;

				switch (dockingDirection) {
				case Alignment.Top:						
					dw.Width = Measure.Stretched;
					mainStack.Orientation = Orientation.Vertical;
					mainStack.AddChild (dw);
					mainStack.AddChild (splitter);
					//mainStack.AddChild (instSpacer.CreateInstance (CurrentInterface));
					break;
				case Alignment.Bottom:
					dw.Width = Measure.Stretched;
					mainStack.Orientation = Orientation.Vertical;
					//mainStack.AddChild (instSpacer.CreateInstance (CurrentInterface));
					mainStack.AddChild (splitter);
					mainStack.AddChild (dw);
					break;
				case Alignment.Left:
					dw.Height = Measure.Stretched;
					mainStack.Orientation = Orientation.Horizontal;
					mainStack.AddChild (dw);
					mainStack.AddChild (splitter);
					//mainStack.AddChild (instSpacer.CreateInstance (CurrentInterface));
					break;
				case Alignment.Right:
					dw.Height = Measure.Stretched;
					mainStack.Orientation = Orientation.Horizontal;
					//mainStack.AddChild (instSpacer.CreateInstance (CurrentInterface));
					mainStack.AddChild (splitter);
					mainStack.AddChild (dw);
					break;
				}

			}
		}
		protected override void onDrop (object sender, DragDropEventArgs e)
		{
			base.onDrop (sender, e);
		}
	}
}


//
// DockStack.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using Crow.IML;

namespace Crow
{
	public class DockStack : GenericStack
	{		
		int dockingDiv = 6;
		GraphicObject subStack = null;

		Docker rootDock { get { return LogicalParent as Docker; }}

		public GraphicObject SubStack {
			get { return subStack;}
			set{ subStack=value; }
		}

		#region CTor
		public DockStack ()	{}
		public DockStack (Interface iface) : base (iface) {}
		#endregion

		public override void AddChild (GraphicObject g)
		{
			base.AddChild (g);
			g.LogicalParent = this.LogicalParent;
		}
		public override void InsertChild (int idx, GraphicObject g)
		{
			base.InsertChild (idx, g);
			g.LogicalParent = this.LogicalParent;
		}

		public override bool PointIsIn (ref Point m)
		{			
			if (!base.PointIsIn(ref m))
				return false;

			Group p = Parent as Group;
			if (p != null) {
				lock (p.Children) {
					for (int i = p.Children.Count - 1; i >= 0; i--) {
						if (p.Children [i] == this)
							break;
						if (p.Children [i].IsDragged)
							continue;
						if (p.Children [i].Slot.ContainsOrIsEqual (m)) {						
							return false;
						}
					}
				}
			}
			return Slot.ContainsOrIsEqual(m);
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			if (IsDropTarget) {				
				DockWindow dw = IFace.DragAndDropOperation.DragSource as DockWindow;
				if (dw.IsDocked) {
					if (!dw.CheckUndock (e.Position)) {
						base.onMouseMove (sender, e);
						return;
					}						
				}
				Point lm = ScreenPointToLocal (e.Position);

				Rectangle r = ClientRectangle;
				int vTreshold = r.Height / dockingDiv;
				int hTreshold = r.Width / dockingDiv;

				Alignment curDockPos = dw.DockingPosition;

				if (lm.X < hTreshold)
					dw.DockingPosition = Alignment.Left;
				else if (lm.X > r.Right - hTreshold)
					dw.DockingPosition = Alignment.Right;
				else if (lm.Y < vTreshold)
					dw.DockingPosition = Alignment.Top;
				else if (lm.Y > r.Bottom - vTreshold)
					dw.DockingPosition = Alignment.Bottom;
				else if (this.Children.Contains (rootDock.CenterDockedObj) && !(rootDock.CenterDockedObj is DockWindow)) {
					r.Inflate (-r.Width / 3, -r.Height / 3);
					if (r.ContainsOrIsEqual (lm))
						dw.DockingPosition = Alignment.Center;
					else
						dw.DockingPosition = Alignment.Undefined;
				} else
					dw.DockingPosition = Alignment.Undefined;

				if (curDockPos != dw.DockingPosition)
					RegisterForGraphicUpdate ();
			}
			base.onMouseMove (sender, e);
		}

		protected override void onDragEnter (object sender, DragDropEventArgs e)
		{
			base.onDragEnter (sender, e);
			RegisterForGraphicUpdate ();
		}
		protected override void onDragLeave (object sender, DragDropEventArgs e)
		{
			base.onDragLeave (sender, e);
			RegisterForGraphicUpdate ();
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

			foreach (GraphicObject g in Children)
				g.Paint (ref gr);			

			childrenRWLock.ExitReadLock ();


			if (!IsDropTarget) {
				gr.Restore ();
				return;
			}

			DockWindow dw = IFace.DragAndDropOperation.DragSource as DockWindow;
			if (!dw.IsDocked) {
				
				Rectangle r;

				if ((dw.DockingPosition.GetOrientation () == Orientation || SubStack == null)&&dw.DockingPosition != Alignment.Center) {
					r = ClientRectangle;
					Console.WriteLine ("Same rect substack=" + SubStack);
				}else {
					r = subStack.ClientRectangle + subStack.Slot.Position + ClientRectangle.TopLeft;
					Console.WriteLine ("sub rect");
				}
				
				switch (dw.DockingPosition) {
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
				case Alignment.Center:
					r.Inflate (-Math.Min (r.Width, r.Height) / dockingDiv);
					gr.Rectangle (r);
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

		public void Undock (DockWindow dw){
			int idx = Children.IndexOf(dw);

			RemoveChild(dw);

			if (rootDock.CenterDockedObj == dw) {				
				rootDock.CenterDockedObj = new GraphicObject (IFace) { IsEnabled = false };
				InsertChild (idx, rootDock.CenterDockedObj);
				SubStack = rootDock.CenterDockedObj;
			}else if (dw.DockingPosition == Alignment.Left || dw.DockingPosition == Alignment.Top)				
				RemoveChild (idx);
			else
				RemoveChild (idx - 1);

			if (Children.Count > 1)
				return;

			DockStack dsp = Parent as DockStack;
			if (dsp == null)
				return;

			RemoveChild (0);
			idx = dsp.Children.IndexOf (this);
			dsp.RemoveChild (this);
			dsp.InsertChild (idx, SubStack);
			dsp.SubStack = SubStack;
			return;
		}
		public void Dock(DockWindow dw){			
			Rectangle r = ClientRectangle;

			int vTreshold = r.Height / dockingDiv;
			int hTreshold = r.Width / dockingDiv;

			DockStack activeStack = this;
			Console.WriteLine ("******* Dockingtack {0}", this.Name);
			if (Children.Count == 1) {
				activeStack = this;
				Orientation = dw.DockingPosition.GetOrientation ();
			}else if (dw.DockingPosition.GetOrientation() != Orientation) {				
				activeStack = new DockStack (IFace);
				int ci = Children.IndexOf (rootDock.CenterDockedObj);
				if (ci  <0 ){
					DockStack dsp = Parent as DockStack;
					if (dsp != null) {
						int idx = dsp.Children.IndexOf (this);
						dsp.RemoveChild (this);
						dsp.SubStack = activeStack;
						dsp.InsertChild (idx, activeStack);
						activeStack.SubStack = this;
						activeStack.AddChild (this);
					} else {
						Docker dk = Parent as Docker;
						dk.RemoveChild (this);
						dk.InsertChild (0, activeStack);
						dk.SubStack = activeStack;
						activeStack.AddChild (this);
						activeStack.SubStack = this;
					}
				}else{
					int i = Children.IndexOf (SubStack);
					RemoveChild (SubStack);
					activeStack.SubStack = SubStack;
					SubStack = activeStack;
					InsertChild(i, activeStack);
					activeStack.AddChild (activeStack.SubStack);
				}
				activeStack.Orientation = dw.DockingPosition.GetOrientation ();
			}
			Console.WriteLine ("Docking {0} in {1}", dw.Name, activeStack.Name);
			switch (dw.DockingPosition) {
			case Alignment.Top:						
				dw.Height = vTreshold;
				dw.Width = Measure.Stretched;
				activeStack.InsertChild (0, dw);
				activeStack.InsertChild (1, new Splitter(IFace));
				break;
			case Alignment.Bottom:
				dw.Height = vTreshold;
				dw.Width = Measure.Stretched;
				activeStack.AddChild (new Splitter(IFace));
				activeStack.AddChild (dw);
				break;
			case Alignment.Left:
				dw.Width = hTreshold;
				dw.Height = Measure.Stretched;
				activeStack.InsertChild (0, dw);
				activeStack.InsertChild (1, new Splitter(IFace));
				break;
			case Alignment.Right:
				dw.Width = hTreshold;
				dw.Height = Measure.Stretched;
				activeStack.AddChild (new Splitter(IFace));
				activeStack.AddChild (dw);
				break;
			case Alignment.Center:
				dw.Width = dw.Height = Measure.Stretched;				 
				int i = activeStack.Children.IndexOf (rootDock.CenterDockedObj);
				activeStack.DeleteChild (i);
				activeStack.InsertChild (i, dw);
				activeStack.SubStack = dw;
				rootDock.CenterDockedObj= dw;
				break;
			}
		}
	}
}


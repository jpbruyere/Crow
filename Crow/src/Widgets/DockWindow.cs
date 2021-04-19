// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using Crow.Cairo;
using Glfw;

namespace Crow
{
	public class DockWindow : Window
	{
		#region CTOR
		public DockWindow () {}
		public DockWindow (Interface iface) : base (iface) {}
		#endregion

		int undockThreshold = 10;
		bool isDocked = false;
		Alignment docking = Alignment.Undefined;

		Point undockingMousePosOrig; //mouse pos when docking was donne, use for undocking on mouse move
		internal Rectangle savedSlot;	//last undocked slot recalled when view is undocked
		internal bool wasResizable;

		public bool IsDocked {
			get { return isDocked; }
			set {
				if (isDocked == value)
					return;
				isDocked = value;
				NotifyValueChangedAuto (isDocked);
				NotifyValueChanged ("IsFloating", IsFloating);
				NotifyValueChanged ("IsDockedInTabView", IsDockedInTabView);
				NotifyValueChanged ("IsDockedInStack", IsDockedInStack);
			}
		}
		[XmlIgnore] public bool IsFloating => !isDocked;
		[XmlIgnore] public bool IsDockedInTabView => LogicalParent is TabView;
		[XmlIgnore] public bool IsDockedInStack => Parent is DockStack;

		public Alignment DockingPosition {
			get { return docking; }
			set {
				if (docking == value)
					return;
				docking = value;
				NotifyValueChangedAuto (DockingPosition);
			}
		}
		/*public override bool PointIsIn (ref Point m)
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
		}*/
		
		bool tryGetTargetDockStack (DockWindow dw, out DockStack ds) {
			if (dw.Parent is DockStack dwp)
				ds = dwp;
			else if (dw.LogicalParent is TabView)
				ds = dw.LogicalParent.Parent as DockStack;
			else
				ds = null;
			return ds != null;
		}
		bool dockParentParent = false;
		public override void onDrag (object sender, MouseMoveEventArgs e)
		{			
			if (isDocked)
				CheckUndock (e.Position);
			else
				moveAndResize (e.XDelta, e.YDelta, currentDirection);
			
			base.onDrag (sender, e);

			if (isDocked)
				return;

			Alignment dockingPosSave = DockingPosition;
			bool dockParentParentSave = dockParentParent;
			dockParentParent = false;
			Rectangle r = default;

			Console.WriteLine ($"onDrag target={IFace.DragAndDropOperation.DropTarget}");

			if (IFace.DragAndDropOperation.DropTarget is DockStack ds) {
				ds.onDragMouseMove (this, e);
				r = ds.ScreenCoordinates (ds.LastPaintedSlot);
			}else if (IFace.DragAndDropOperation.DropTarget is DockWindow dw && dw.IsDocked == true) {
				Point m = dw.ScreenPointToLocal (e.Position);
				r = dw.ClientRectangle;
				Rectangle dwCb = r;
				dwCb.Inflate (dwCb.Width / -5, dwCb.Height / -5);
				if (dwCb.ContainsOrIsEqual(m)) {
					DockingPosition = Alignment.Center;
					r = dw.ScreenCoordinates (dw.LastPaintedSlot);
				} else {
					dwCb = r;
					dwCb.Inflate (-4,-4);
										
					if (tryGetTargetDockStack (dw, out DockStack targetStack)) {
						Console.WriteLine ($"exterior: {!dwCb.ContainsOrIsEqual (m)} targetStack.Parent: {targetStack.Parent.GetType()}");
						if (dwCb.ContainsOrIsEqual (m)) {
							r = dw.ScreenCoordinates (dw.LastPaintedSlot);
						} else if (targetStack.Parent is DockStack) {
							dockParentParent = true;
							targetStack = targetStack.Parent as DockStack;
							r = targetStack.ScreenCoordinates (targetStack.LastPaintedSlot);
						} else
							r = dw.ScreenCoordinates (dw.LastPaintedSlot);
						
						targetStack.onDragMouseMove (this, e);
					} else
						System.Diagnostics.Debugger.Break ();

					/*if (dw.Parent is DockStack dsp) {
						if (dsp.focusedChild == null)
							r = dsp.ScreenCoordinates (dsp.LastPaintedSlot);
						else
							r = dsp.focusedChild.ScreenCoordinates (dsp.focusedChild.LastPaintedSlot);
					} else if (dw.LogicalParent is TabView tv && dw.LogicalParent.Parent is DockStack dspp) {
						dspp.onDragMouseMove (this, e);
						r = tv.ScreenCoordinates (tv.LastPaintedSlot);
					}*/
				}
			}else
				DockingPosition = Alignment.Undefined;

			if (DockingPosition != dockingPosSave || dockParentParent != dockParentParentSave) {
				if (DockingPosition == Alignment.Undefined) {
					IFace.ClearDragImage ();
					return;
				}
				switch (DockingPosition) {
				case Alignment.Top:
					r.Height /= 4;
					break;
				case Alignment.Bottom:
					r.Y += r.Height - r.Height / 4;
					r.Height /= 4;
					break;
				case Alignment.Left:
					r.Width /= 4;
					break;
				case Alignment.Right:
					r.X += r.Width - r.Width / 4;
					r.Width /= 4;
					break;
				case Alignment.Center:
					r.Inflate (r.Width / -3, r.Height / -3);
					break;
				}
	            Surface dragImg = IFace.surf.CreateSimilar (Crow.Cairo.Content.ColorAlpha,	r.Width, r.Height);
				using (Crow.Cairo.Context gr = new Crow.Cairo.Context(dragImg)) {
					gr.LineWidth = 1;
					gr.Rectangle (0,0,r.Width,r.Height);
					gr.SetSource (0.2,0.3,0.9,0.5);
					gr.FillPreserve ();
					gr.SetSource (0.1,0.2,1);
					gr.Stroke ();
				}
				IFace.CreateDragImage (dragImg, r, false);
			}
		}
        protected override void onDragEnter (object sender, DragDropEventArgs e) {
            base.onDragEnter (sender, e);
        }
        public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			base.onMouseDown (sender, e);

			if (this.HasFocus && IsDocked && e.Button == MouseButton.Left)
				undockingMousePosOrig = e.Position;
		}
		public bool CheckUndock (Point mousePos) {
			//if (DockingPosition == Alignment.Center)
			//	return false;
			System.Diagnostics.Debug.WriteLine ($"{mousePos.X},{mousePos.Y}");
			if (Math.Abs (mousePos.X - undockingMousePosOrig.X) < undockThreshold ||
			    Math.Abs (mousePos.X - undockingMousePosOrig.X) < undockThreshold)
				return false;
			Undock ();
			return true;
		}

		protected override void onStartDrag (object sender, DragDropEventArgs e)
		{
			if (currentDirection == Direction.None)
				base.onStartDrag (sender, e);

			undockingMousePosOrig = IFace.MousePosition;
		}
		public override void onDrop (object sender, DragDropEventArgs e)
		{
			if (!(isDocked || DockingPosition == Alignment.Undefined)) {
				if (e.DropTarget is DockStack ds)
					Dock (ds);
				else if (e.DropTarget is DockWindow dw) {
					if (DockingPosition == Alignment.Center)
						Dock (dw);
					else if (tryGetTargetDockStack (dw, out DockStack targetStack)) {
						if (dockParentParent)
							targetStack = targetStack.Parent as DockStack;
						Dock (targetStack);						
					}else
						System.Diagnostics.Debugger.Break ();
				}else
					System.Diagnostics.Debugger.Break ();
			}
			base.onDrop (sender, e);
			IFace.ClearDragImage ();
		}
		public void Undock () {
			lock (IFace.UpdateMutex) {
				if (LogicalParent is TabView tv) {
					tv.RemoveItem (this, false);
					if (tv.Items.Count == 1) {
						Widget w = tv.Items[0];
						tv.RemoveItem (w, false);
						DockStack ds = tv.Parent as DockStack;
						int idx = ds.Children.IndexOf (tv);
						ds.RemoveChild (tv);
						ds.InsertChild (idx, w);
						w.Width = tv.Width;
						w.Height = tv.Height;
						if (ds.stretchedChild == tv)
							ds.stretchedChild = w;
						tv.Dispose();
						w.IsVisible = true;
						ds.checkAlignments();
					}
				} else if (Parent is DockStack ds) {
					ds.Undock (this);
				} else
					throw new Exception ("docking error");				
				
				IFace.AddWidget (this);

				Left = IFace.MousePosition.X - 10;
				Top = IFace.MousePosition.Y - 10;
				Width = savedSlot.Width;
				Height = savedSlot.Height;

				IsDocked = false;
				DockingPosition = Alignment.Undefined;
				Resizable = wasResizable;
			}
		}

		void dock () {
			IFace.RemoveWidget (this);

			undockingMousePosOrig = IFace.MousePosition;
			//undockingMousePosOrig = lastMousePos;
			savedSlot = this.LastPaintedSlot;
			wasResizable = Resizable;
			Resizable = false;
			LastSlots = LastPaintedSlot = Slot = default(Rectangle);
			Left = Top = 0;
		}
		public void Dock (DockWindow target) {
			lock (IFace.UpdateMutex) {
				//IsDocked = true;
				dock ();

				if (target.LogicalParent is TabView tv) {
					tv.AddItem (this);
					DockingPosition = Alignment.Center;
					this.Width = this.Height = Measure.Stretched;
					IsDocked = true;
				} else if (target.Parent is DockStack ds) {
					int idx = ds.Children.IndexOf (target);
					ds.RemoveChild (target);
					TabView tv2 = new TabView(IFace, "DockingTabView");
					ds.InsertChild (idx, tv2);
					tv2.Width = target.Width;
					tv2.Height = target.Height;
					if (ds.stretchedChild == target)
						ds.stretchedChild = tv2;
					tv2.AddItem (target);
					tv2.AddItem (this);
					target.Width = target.Height = this.Width = this.Height = Measure.Stretched;
					target.DockingPosition = this.DockingPosition = Alignment.Center;
					IsDocked = true;
				}				
			}
		}
		public void Dock (DockStack target){
			lock (IFace.UpdateMutex) {				
				dock ();

				target.Dock (this);
			}
		}

		protected override void close ()
		{
			if (isDocked)
				Undock ();
			base.close ();
		}

		internal string GetConfigString () =>
			string.Format($"WIN;{Name};{Width};{Height};{DockingPosition};{savedSlot};{wasResizable};");
	}
}


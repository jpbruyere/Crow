// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
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
				NotifyValueChanged ("IsFloating", !isDocked);
			}
		}
		[XmlIgnore] public bool IsFloating { get { return !isDocked; }}

		public Alignment DockingPosition {
			get { return docking; }
			set {
				if (docking == value)
					return;
				docking = value;
				NotifyValueChangedAuto (DockingPosition);
			}
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

		public override void onDrag (object sender, MouseMoveEventArgs e)
		{			
			if (isDocked)
				CheckUndock (e.Position);
			else
				moveAndResize (e.XDelta, e.YDelta, currentDirection);
			
			base.onDrag (sender, e);
			if (IFace.DragAndDropOperation.DropTarget is DockStack ds)
				ds.onDragMouseMove (this, e);
			else if (IFace.DragAndDropOperation.DropTarget is DockWindow dw)
				(dw.Parent as DockStack)?.onDragMouseMove (this, e);
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
				else if (e.DropTarget is DockWindow dw)
					Dock (dw.Parent as DockStack);
			}
			base.onDrop (sender, e);
		}
		public void Undock () {
			lock (IFace.UpdateMutex) {
				DockStack ds = Parent as DockStack;
				ds.Undock (this);

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

		public void Dock (DockStack target){
			lock (IFace.UpdateMutex) {
				//IsDocked = true;
				undockingMousePosOrig = IFace.MousePosition;
				//undockingMousePosOrig = lastMousePos;
				savedSlot = this.LastPaintedSlot;
				wasResizable = Resizable;
				Resizable = false;
				LastSlots = LastPaintedSlot = Slot = default(Rectangle);
				Left = Top = 0;

				IFace.RemoveWidget (this);

				target.Dock (this);
			}
		}

		protected override void close ()
		{
			if (isDocked)
				Undock ();
			base.close ();
		}
	}
}


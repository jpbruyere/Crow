//
// DockingView.cs
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
				NotifyValueChanged ("IsDocked", isDocked);
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
				NotifyValueChanged ("DockingPosition", DockingPosition);
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

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
//			if (this.HasFocus && e.Mouse.IsButtonDown (MouseButton.Left) && IsDocked) {
//				if (Math.Abs (e.Position.X - undockingMousePosOrig.X) > 10 ||
//				    Math.Abs (e.Position.X - undockingMousePosOrig.X) > 10)
//					Undock ();
//			}

			if (this.HasFocus && e.Mouse.IsButtonDown (MouseButton.Left) && IsDocked)
				CheckUndock (e.Position);

			base.onMouseMove (sender, e);
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
			Console.WriteLine ($"{mousePos.X},{mousePos.Y}");
			if (Math.Abs (mousePos.X - undockingMousePosOrig.X) < undockThreshold ||
			    Math.Abs (mousePos.X - undockingMousePosOrig.X) < undockThreshold)
				return false;
			Undock ();
			return true;
		}

		protected override void onStartDrag (object sender, DragDropEventArgs e)
		{
			base.onStartDrag (sender, e);

			undockingMousePosOrig = IFace.Mouse.Position;
		}
		protected override void onDrop (object sender, DragDropEventArgs e)
		{
			if (!isDocked && DockingPosition != Alignment.Undefined && e.DropTarget is DockStack)
				Dock (e.DropTarget as DockStack);
			base.onDrop (sender, e);
		}
		public void Undock () {
			lock (IFace.UpdateMutex) {
				DockStack ds = Parent as DockStack;
				ds.Undock (this);

				IFace.AddWidget (this);

				Left = savedSlot.Left;
				Top = savedSlot.Top;
				Width = savedSlot.Width;
				Height = savedSlot.Height;

				IsDocked = false;
				DockingPosition = Alignment.Undefined;
				Resizable = wasResizable;
			}
		}

		public void Dock (DockStack target){
			lock (IFace.UpdateMutex) {
				IsDocked = true;
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


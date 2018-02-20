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

namespace Crow
{
	public class DockWindow : Window
	{
		#region CTOR
		public DockWindow () : base ()
		{
		}
		#endregion

		bool isDocked = false;
		Alignment docking = Alignment.Center;

		Point lastMousePos;	//last known mouse pos in this control
		Point undockingMousePosOrig; //mouse pos when docking was donne, use for undocking on mouse move
		Rectangle savedSlot;	//last undocked slot recalled when view is undocked
		bool wasResizable;

		public bool IsDocked {
			get { return isDocked; }
			set {
				if (isDocked == value)
					return;
				isDocked = value;
				NotifyValueChanged ("IsDocked", isDocked);
			}
		}
		public Alignment DockingPosition {
			get { return docking; }
			set {
				if (docking == value)
					return;
				docking = value;
				NotifyValueChanged ("DockingPosition", DockingPosition);
			}
		}
		protected override void onDrop (object sender, DragDropEventArgs e)
		{
			base.onDrop (sender, e);
			Docker dv = Parent as Docker;
			dv.Dock (this);
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
						if (p.Children [i].Slot.ContainsOrIsEqual (m)) {						
							return false;
						}
					}
				}
			}
			return Slot.ContainsOrIsEqual(m);
		}

//		public override void OnLayoutChanges (LayoutingType layoutType)
//		{
//			base.OnLayoutChanges (layoutType);
//
//			if (isDocked)
//				return;
			
//			Docker dv = Parent as Docker;
//			if (dv == null)
//				return;
//
//			Rectangle dvCliRect = dv.ClientRectangle;
//
//			if (layoutType == LayoutingType.X) {
//				if (Slot.X < dv.DockingThreshold)
//					dock (Alignment.Left);
//				else if (Slot.Right > dvCliRect.Width - dv.DockingThreshold)
//					dock (Alignment.Right);
//			}else if (layoutType == LayoutingType.Y) {
//				if (Slot.Y < dv.DockingThreshold)
//					dock (Alignment.Top);
//				else if (Slot.Bottom > dvCliRect.Height - dv.DockingThreshold)
//					dock (Alignment.Bottom);
//			}
//		}
//
//		public override void onMouseMove (object sender, MouseMoveEventArgs e)
//		{
//			lastMousePos = e.Position;
//
//			if (this.HasFocus && e.Mouse.IsButtonDown (MouseButton.Left) && IsDocked) {
//				Docker dv = Parent as Docker;
//				if (docking == Alignment.Left) {
//					if (lastMousePos.X - undockingMousePosOrig.X > dv.DockingThreshold)
//						undock ();
//				} else if (docking == Alignment.Right) {
//					if (undockingMousePosOrig.X - lastMousePos.X > dv.DockingThreshold)
//						undock ();
//				} else if (docking == Alignment.Top) {
//					if (lastMousePos.Y - undockingMousePosOrig.Y > dv.DockingThreshold)
//						undock ();
//				} else if (docking == Alignment.Bottom) {
//					if (undockingMousePosOrig.Y - lastMousePos.Y > dv.DockingThreshold)
//						undock ();
//				}
//				return;
//			}
//
//			base.onMouseMove (sender, e);
//		}
//		public override void onMouseDown (object sender, MouseButtonEventArgs e)
//		{
//			base.onMouseDown (sender, e);
//
//			if (this.HasFocus && IsDocked && e.Button == MouseButton.Left)
//				undockingMousePosOrig = lastMousePos;
//		}

//		protected override void onBorderMouseEnter (object sender, MouseMoveEventArgs e)
//		{
//			base.onBorderMouseEnter (sender, e);
//
//			if (isDocked) {
//				switch (docking) {
//				case Alignment.Top:
//					if (this.currentDirection != Window.Direction.S)
//						onBorderMouseLeave (this, null);
//					break;
//				case Alignment.Left:
//					if (this.currentDirection != Window.Direction.E)
//						onBorderMouseLeave (this, null);
//					break;
//				case Alignment.TopLeft:
//					break;
//				case Alignment.Right:
//					if (this.currentDirection != Window.Direction.W)
//						onBorderMouseLeave (this, null);
//					break;
//				case Alignment.TopRight:
//					break;
//				case Alignment.Bottom:
//					if (this.currentDirection != Window.Direction.N)
//						onBorderMouseLeave (this, null);					
//					break;
//				case Alignment.BottomLeft:
//					break;
//				case Alignment.BottomRight:
//					break;
//				case Alignment.Center:
//					break;
//				default:
//					break;
//				}
//			}
//		}

		void undock () {
			this.Left = savedSlot.Left;
			this.Top = savedSlot.Top;
			this.Width = savedSlot.Width;
			this.Height = savedSlot.Height;

			IsDocked = false;
			Resizable = wasResizable;
		}
		void dock (Alignment align){
			IsDocked = true;
			docking = align;
			undockingMousePosOrig = lastMousePos;
			savedSlot = this.LastPaintedSlot;
			wasResizable = Resizable;
			Resizable = false;

			this.Left = this.Top = 0;

			switch (align) {
			case Alignment.Top:
				this.HorizontalAlignment = HorizontalAlignment.Left;
				this.VerticalAlignment = VerticalAlignment.Top;
				this.Width = Measure.Stretched;
				break;
			case Alignment.Left:
				this.HorizontalAlignment = HorizontalAlignment.Left;
				this.VerticalAlignment = VerticalAlignment.Top;
				this.Height = Measure.Stretched;
				break;
			case Alignment.TopLeft:
				break;
			case Alignment.Right:
				this.HorizontalAlignment = HorizontalAlignment.Right;
				this.VerticalAlignment = VerticalAlignment.Top;
				this.Height = Measure.Stretched;
				break;
			case Alignment.TopRight:
				break;
			case Alignment.Bottom:
				this.HorizontalAlignment = HorizontalAlignment.Left;
				this.VerticalAlignment = VerticalAlignment.Bottom;
				this.Width = Measure.Stretched;
				break;
			case Alignment.BottomLeft:
				break;
			case Alignment.BottomRight:
				break;
			case Alignment.Center:
				break;
			default:
				break;
			}
			
		}
	}
}


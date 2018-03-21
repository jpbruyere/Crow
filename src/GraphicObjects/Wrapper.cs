//
// Wrapper.cs
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
using Cairo;

namespace Crow
{
	/// <summary>
	/// group control that arrange its children in a direction and jump to
	/// the next line or row when no room is left
	/// </summary>
	public class Wrapper : GenericStack
	{
		#region CTOR
		protected Wrapper() : base(){}
		public Wrapper (Interface iface) : base(iface){}
		#endregion

		#region Group Overrides
		public override void ChildrenLayoutingConstraints (ref LayoutingType layoutType)
		{
			layoutType &= (~LayoutingType.Positioning);
		}
		public override void ComputeChildrenPositions()
		{
			int dx = 0;
			int dy = 0;

			if (Orientation == Orientation.Vertical) {
				int tallestChild = 0;
				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;
					if (dx + c.Slot.Width > ClientRectangle.Width) {
						dx = 0;
						dy += tallestChild + Spacing;
						c.Slot.X = dx;
						c.Slot.Y = dy;
						tallestChild = c.Slot.Height;
					} else {
						if (tallestChild < c.Slot.Height)
							tallestChild = c.Slot.Height;
						c.Slot.X = dx;
						c.Slot.Y = dy;
					}
					dx += c.Slot.Width + Spacing;
				}
			} else {
				int largestChild = 0;
				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;
					if (dy + c.Slot.Height > ClientRectangle.Height) {
						dy = 0;
						dx += largestChild + Spacing;
						c.Slot.X = dx;
						c.Slot.Y = dy;
						largestChild = c.Slot.Width;
					} else {
						if (largestChild < c.Slot.Width)
							largestChild = c.Slot.Width;
						c.Slot.X = dx;
						c.Slot.Y = dy;
					}
					dy += c.Slot.Height + Spacing;
				}
			}
			IsDirty = true;
		}
		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			//children can't stretch in a wrapper
			GraphicObject go = sender as GraphicObject;
			//System.Diagnostics.Debug.WriteLine ("wrapper child layout change: " + go.LastSlots.ToString() + " => " + go.Slot.ToString());
			switch (arg.LayoutType) {
			case LayoutingType.Width:
				if (Orientation == Orientation.Horizontal && go.Width.Units == Unit.Percent){
					go.Width = Measure.Fit;
					return;
				}
				this.RegisterForLayouting (LayoutingType.Width);
				break;
			case LayoutingType.Height:
				if (Orientation == Orientation.Vertical && go.Height.IsRelativeToParent) {
					go.Height = Measure.Fit;
					return;
				}
				this.RegisterForLayouting (LayoutingType.Height);
				break;
			default:
				return;
			}
			this.RegisterForLayouting (LayoutingType.ArrangeChildren);
		}
		#endregion

		#region GraphicObject Overrides
		public override int measureRawSize(LayoutingType lt)
		{
			int tmp = 0;
			//Wrapper can't fit in the opposite direction of the wrapper, this func is called only if Fit
			if (lt == LayoutingType.Width) {
				if (Orientation == Orientation.Vertical) {
					Width = Measure.Stretched;
					return -1;
				} else if (RegisteredLayoutings.HasFlag (LayoutingType.Height))
					return -1;
				else {
					int dy = 0;
					int largestChild = 0;

					childrenRWLock.EnterReadLock();

					foreach (GraphicObject c in Children) {
						if (!c.Visible)
							continue;
						if (c.Height.IsRelativeToParent &&
						    c.RegisteredLayoutings.HasFlag (LayoutingType.Height)) {
							childrenRWLock.ExitReadLock();
							return -1;
						}
						if (dy + c.Slot.Height > ClientRectangle.Height) {
							dy = 0;
							tmp += largestChild + Spacing;
							largestChild = c.Slot.Width;
						} else if (largestChild < c.Slot.Width)
							largestChild = c.Slot.Width;

						dy += c.Slot.Height + Spacing;
					}

					childrenRWLock.ExitReadLock ();

					if (dy == 0)
						tmp -= Spacing;
					return tmp + largestChild + 2 * Margin;
				}
			} else if (Orientation == Orientation.Horizontal) {
				Height = Measure.Stretched;
				return -1;
			} else if (RegisteredLayoutings.HasFlag (LayoutingType.Width))
				return -1;
			else {
				int dx = 0;
				int tallestChild = 0;

				childrenRWLock.EnterReadLock();

				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;
					if (c.Width.IsRelativeToParent &&
					    c.RegisteredLayoutings.HasFlag (LayoutingType.Width)) {
						childrenRWLock.ExitReadLock();
						return -1;
					}
					if (dx + c.Slot.Width > ClientRectangle.Width) {
						dx = 0;
						tmp += tallestChild + Spacing;
						tallestChild = c.Slot.Height;
					} else if (tallestChild < c.Slot.Height)
						tallestChild = c.Slot.Height;

					dx += c.Slot.Width + Spacing;
				}

				childrenRWLock.ExitReadLock();

				if (dx == 0)
					tmp -= Spacing;
				return tmp + tallestChild + 2 * Margin;
			}
		}
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren) {
				if ((RegisteredLayoutings & LayoutingType.Sizing) != 0)
					return false;

				ComputeChildrenPositions ();

				EnqueueForRepaint ();

				return true;
			}

			return base.UpdateLayout(layoutType);
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			switch (layoutType) {
			case LayoutingType.Width:
				childrenRWLock.EnterReadLock ();
				foreach (GraphicObject c in Children) {
					if (c.Width.IsRelativeToParent)
						c.RegisterForLayouting (LayoutingType.Width);
				}
				childrenRWLock.ExitReadLock ();
				if (Height == Measure.Fit)
					RegisterForLayouting (LayoutingType.Height);
				RegisterForLayouting (LayoutingType.X);
				break;
			case LayoutingType.Height:
				childrenRWLock.EnterReadLock ();
				foreach (GraphicObject c in Children) {
					if (c.Height.IsRelativeToParent)
						c.RegisterForLayouting (LayoutingType.Height);
				}
				childrenRWLock.ExitReadLock ();
				if (Width == Measure.Fit)
					RegisterForLayouting (LayoutingType.Width);
				RegisterForLayouting (LayoutingType.Y);
				break;
			default:
				return;
			}
			RegisterForLayouting (LayoutingType.ArrangeChildren);
			raiseLayoutChanged (new LayoutingEventArgs (layoutType));
		}
		#endregion
	}
}


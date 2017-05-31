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

namespace Crow
{
	public class Wrapper : GenericStack
	{
		public Wrapper () : base()
		{}

		#region Group Overrides
		public override void ChildrenLayoutingConstraints (ref LayoutingType layoutType)
		{
			layoutType &= (~LayoutingType.Positioning);
		}
		unsafe public override void ComputeChildrenPositions()
		{
			int dx = 0;
			int dy = 0;

			if (Orientation == Orientation.Vertical) {
				int tallestChild = 0;
				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;
					if (dx + c.nativeHnd->Slot.Width > ClientRectangle.Width) {
						dx = 0;
						dy += tallestChild + Spacing;
						c.nativeHnd->Slot.X = dx;
						c.nativeHnd->Slot.Y = dy;
						tallestChild = c.nativeHnd->Slot.Height;
					} else {
						if (tallestChild < c.nativeHnd->Slot.Height)
							tallestChild = c.nativeHnd->Slot.Height;
						c.nativeHnd->Slot.X = dx;
						c.nativeHnd->Slot.Y = dy;
					}
					dx += c.nativeHnd->Slot.Width + Spacing;
				}
			} else {
				int largestChild = 0;
				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;
					if (dy + c.nativeHnd->Slot.Height > ClientRectangle.Height) {
						dy = 0;
						dx += largestChild + Spacing;
						c.nativeHnd->Slot.X = dx;
						c.nativeHnd->Slot.Y = dy;
						largestChild = c.nativeHnd->Slot.Width;
					} else {
						if (largestChild < c.nativeHnd->Slot.Width)
							largestChild = c.nativeHnd->Slot.Width;
						c.nativeHnd->Slot.X = dx;
						c.nativeHnd->Slot.Y = dy;
					}
					dy += c.nativeHnd->Slot.Height + Spacing;
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
				if (Orientation == Orientation.Vertical && go.Height.Units == Unit.Percent) {
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
		unsafe protected override int measureRawSize (LayoutingType lt)
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
					lock (Children) {
						foreach (GraphicObject c in Children) {
							if (!c.Visible)
								continue;
							if (c.Height.Units == Unit.Percent &&
								c.RegisteredLayoutings.HasFlag (LayoutingType.Height))
								return -1;
							if (dy + c.nativeHnd->Slot.Height > ClientRectangle.Height) {
								dy = 0;
								tmp += largestChild + Spacing;
								largestChild = c.nativeHnd->Slot.Width;
							} else if (largestChild < c.nativeHnd->Slot.Width)
								largestChild = c.nativeHnd->Slot.Width;

							dy += c.nativeHnd->Slot.Height + Spacing;
						}
						if (dy == 0)
							tmp -= Spacing;
						return tmp + largestChild + 2 * Margin;
					}
				}
			} else if (Orientation == Orientation.Horizontal) {
				Height = Measure.Stretched;
				return -1;
			} else if (RegisteredLayoutings.HasFlag (LayoutingType.Width))
				return -1;
			else {
				int dx = 0;
				int tallestChild = 0;
				lock (Children) {
					foreach (GraphicObject c in Children) {
						if (!c.Visible)
							continue;
						if (c.Width.Units == Unit.Percent &&
							c.RegisteredLayoutings.HasFlag (LayoutingType.Width))
							return -1;
						if (dx + c.nativeHnd->Slot.Width > ClientRectangle.Width) {
							dx = 0;
							tmp += tallestChild + Spacing;
							tallestChild = c.nativeHnd->Slot.Height;
						} else if (tallestChild < c.nativeHnd->Slot.Height)
							tallestChild = c.nativeHnd->Slot.Height;

						dx += c.nativeHnd->Slot.Width + Spacing;
					}
					if (dx == 0)
						tmp -= Spacing;
					return tmp + tallestChild + 2 * Margin;
				}
			}
		}

		public override bool UpdateLayout (LayoutingType layoutType)
		{
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren) {
				if ((RegisteredLayoutings & LayoutingType.Sizing) != 0)
					return false;

				ComputeChildrenPositions ();

				//if no layouting remains in queue for item, registre for redraw
				if (RegisteredLayoutings == LayoutingType.None && IsDirty)
					CurrentInterface.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout(layoutType);
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			#if DEBUG_LAYOUTING
//			CurrentInterface.currentLQI.Slot = LastSlots;
//			CurrentInterface.currentLQI.Slot = Slot;
			#endif
			switch (layoutType) {
			case LayoutingType.Width:
				foreach (GraphicObject c in Children) {
					if (c.Width.Units == Unit.Percent)
						c.RegisterForLayouting (LayoutingType.Width);
				}
				if (Height == Measure.Fit)
					RegisterForLayouting (LayoutingType.Height);
				RegisterForLayouting (LayoutingType.X);
				break;
			case LayoutingType.Height:
				foreach (GraphicObject c in Children) {
					if (c.Height.Units == Unit.Percent)
						c.RegisterForLayouting (LayoutingType.Height);
				}
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


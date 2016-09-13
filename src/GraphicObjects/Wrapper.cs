//
//  Wrapper.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
			bmp = null;
		}
		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			//children can't stretch in a wrapper
			GraphicObject go = sender as GraphicObject;
			//Debug.WriteLine ("child layout change: " + go.LastSlots.ToString() + " => " + go.Slot.ToString());
			switch (arg.LayoutType) {
			case LayoutingType.Width:
				if (Orientation == Orientation.Horizontal && go.Width.Units == Unit.Percent) {
					go.Width = Measure.Fit;
					return;
				}
				break;
			case LayoutingType.Height:
				if (Orientation == Orientation.Vertical && go.Height.Units == Unit.Percent) {
					go.Height = Measure.Fit;
					return;
				}
				break;
			default:
				return;
			}
			this.RegisterForLayouting (LayoutingType.ArrangeChildren);
		}
		#endregion

		#region GraphicObject Overrides
		protected override int measureRawSize (LayoutingType lt)
		{
			int tmp = 0;
			//Wrapper can't fit in the direction of the wrapper
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
							if (dy + c.Slot.Height > ClientRectangle.Height) {
								dy = 0;
								tmp += largestChild + Spacing;
								largestChild = c.Slot.Width;
							} else if (largestChild < c.Slot.Width)
								largestChild = c.Slot.Width;

							dy += c.Slot.Height + Spacing;
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
						if (dx + c.Slot.Width > ClientRectangle.Width) {
							dx = 0;
							tmp += tallestChild + Spacing;
							tallestChild = c.Slot.Height;
						} else if (tallestChild < c.Slot.Height)
							tallestChild = c.Slot.Height;

						dx += c.Slot.Width + Spacing;
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
				if (RegisteredLayoutings == LayoutingType.None && bmp == null)
					CurrentInterface.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout(layoutType);
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			#if DEBUG_LAYOUTING
			CurrentInterface.currentLQI.Slot = LastSlots;
			CurrentInterface.currentLQI.Slot = Slot;
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
			//LayoutChanged.Raise (this, new LayoutingEventArgs (layoutType));
		}
		#endregion
	}
}


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

			if (Orientation == Orientation.Horizontal) {
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
					} else if (largestChild < c.Slot.Width){
						largestChild = c.Slot.Width;
						c.Slot.X = dx;
						c.Slot.Y = dy;
						dy += c.Slot.Height + Spacing;
					}
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
				if (Orientation == Orientation.Vertical && go.Width == Measure.Stretched) {
					go.Width = Measure.Fit;
					return;
				}
				break;
			case LayoutingType.Height:
				if (Orientation == Orientation.Horizontal && go.Height == Measure.Stretched) {
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
			//Wrapper can't fit
			if (lt == LayoutingType.Width)
				Width = Measure.Stretched;
			else
				Height = Measure.Stretched;
			return -1;				
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
					Interface.CurrentInterface.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout(layoutType);
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			#if DEBUG_LAYOUTING
			LayoutingQueueItem.currentLQI.Slot = LastSlots;
			LayoutingQueueItem.currentLQI.Slot = Slot;
			#endif

			switch (layoutType) {
			case LayoutingType.Width:
				foreach (GraphicObject c in Children) {
					if (c.Width.Units == Unit.Percent)
						c.RegisterForLayouting (LayoutingType.Width);
				}
				break;
			case LayoutingType.Height:
				foreach (GraphicObject c in Children) {
					if (c.Height.Units == Unit.Percent)
						c.RegisterForLayouting (LayoutingType.Height);
				}
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


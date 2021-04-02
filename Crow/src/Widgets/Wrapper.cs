// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using static Crow.Logger;
namespace Crow
{
	/// <summary>
	/// group control that arrange its children in a direction and jump to
	/// the next line or row when no room is left
	/// </summary>
	public class Wrapper : GenericStack
	{
		#region CTOR
		protected Wrapper() {}
		public Wrapper (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region Group Overrides
		public override void ChildrenLayoutingConstraints (ILayoutable layoutable, ref LayoutingType layoutType)
		{
			layoutType &= (~LayoutingType.Positioning);
		}
		public override void ComputeChildrenPositions()
		{
			int dx = 0;
			int dy = 0;

			if (Orientation == Orientation.Vertical) {
				int tallestChild = 0;
				foreach (Widget c in Children) {
					if (!c.IsVisible)
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
				foreach (Widget c in Children) {
					if (!c.IsVisible)
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
			Widget go = sender as Widget;
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
		public override int measureRawSize (LayoutingType lt)
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

					foreach (Widget c in Children) {
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

				foreach (Widget c in Children) {
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

				//if no layouting remains in queue for item, registre for redraw
				if (RegisteredLayoutings == LayoutingType.None && IsDirty)
					IFace.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout(layoutType);
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			switch (layoutType) {
			case LayoutingType.Width:
				foreach (Widget c in Children) {
					if (c.Width.IsRelativeToParent)
						c.RegisterForLayouting (LayoutingType.Width);
				}
				if (Height == Measure.Fit)
					RegisterForLayouting (LayoutingType.Height);
				RegisterForLayouting (LayoutingType.X);
				break;
			case LayoutingType.Height:
				foreach (Widget c in Children) {
					if (c.Height.IsRelativeToParent)
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


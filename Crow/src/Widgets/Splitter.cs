// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using Drawing2D;

namespace Crow
{
	/// <summary>
	/// control to add between children of a Stack to allow them to be resized
	/// with the pointer
	/// </summary>
	public class Splitter : Widget
	{
		#region CTOR
		protected Splitter() {}
		public Splitter (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		int thickness;

		[DefaultValue(1)]
		public virtual int Thickness {
			get { return thickness; }
			set {
				if (thickness == value)
					return;
				thickness = value;
				NotifyValueChangedAuto (thickness);
				RegisterForLayouting (LayoutingType.Sizing);
				RegisterForGraphicUpdate ();
			}
		}

		#region Widget override
		public override ILayoutable Parent {
			get => base.Parent;
			set {
				if (value != null) {
					GenericStack gs = value as GenericStack;
					if (gs == null)
						throw new Exception ("Splitter may only be child of stack");

				}
				base.Parent = value;
			}
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if ((Parent as GenericStack).Orientation == Orientation.Horizontal)
				IFace.MouseCursor = MouseCursor.sb_h_double_arrow;
			else
				IFace.MouseCursor = MouseCursor.sb_v_double_arrow;
		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			GenericStack gs = Parent as GenericStack;
			Rectangle gsRect = gs.ClientRectangle;
			Point m = gs.ScreenPointToLocal (e.Position);
			int ptrSplit = gs.Children.IndexOf (this);

			if (IFace.IsDown (Glfw.MouseButton.Left) && ptrSplit > 0 && ptrSplit < gs.Children.Count - 1) {
				lock (IFace.UpdateMutex) {
					Widget w0 = gs.Children[ptrSplit - 1];
					Widget w1 = gs.Children[ptrSplit + 1];
					if (gs.Orientation == Orientation.Horizontal) {
						int x = m.X - Slot.Width / 2 - gs.Spacing;

						if (x > w0.Slot.Left + w0.MinimumSize.Width &&
							x + Slot.Width + 2 * gs.Spacing < w1.Slot.Right - w1.MinimumSize.Width) {
							if (w0.Width != Measure.Stretched) {
								if (w0.Width.IsRelativeToParent)
									w0.Width = new Measure((int)Math.Round (100.0 * (x - w0.Slot.X) / (double)gsRect.Width), Unit.Percent);
								else
									w0.Width = x - w0.Slot.X;
							}
							if (w1.Width != Measure.Stretched) {
								x += Slot.Width + 2 * gs.Spacing;
								if (w1.Width.IsRelativeToParent)
									w1.Width = new Measure((int)Math.Round (100.0 * (w1.Slot.Right - x) / (double)gsRect.Width), Unit.Percent);
								else
									w1.Width = w1.Slot.Right - x;
							}
						}
					} else {
						int y = m.Y - Slot.Height / 2 - gs.Spacing;

						if (y > w0.Slot.Top + w0.MinimumSize.Height &&
							y + Slot.Height + 2 * gs.Spacing < w1.Slot.Bottom - w1.MinimumSize.Height) {
							if (w0.Height != Measure.Stretched) {
								if (w0.Height.IsRelativeToParent)
									w0.Height = new Measure((int)Math.Round (100.0 * (y - w0.Slot.Top) / (double)gsRect.Height), Unit.Percent);
								else
									w0.Height = y - w0.Slot.Top;
							}
							if (w1.Height != Measure.Stretched) {
								y += Slot.Height + 2 * gs.Spacing;
								if (w1.Height.IsRelativeToParent)
									w1.Height = new Measure((int)Math.Round (100.0 * (w1.Slot.Bottom - y) / (double)gsRect.Height), Unit.Percent);
								else
									w1.Height = w1.Slot.Bottom - y;
							}
						}
					}
				}
				e.Handled = true;
			}


			base.onMouseMove (sender, e);
		}

		public override bool UpdateLayout (LayoutingType layoutType)
		{
			GenericStack gs = Parent as GenericStack;
			if (layoutType == LayoutingType.Width){
				if (gs.Orientation == Orientation.Horizontal)
					Width = thickness;
				else
					Width = Measure.Stretched;
			} else if (layoutType == LayoutingType.Height){
				if (gs.Orientation == Orientation.Vertical)
					Height = thickness;
				else
					Height = Measure.Stretched;
			}
			return base.UpdateLayout (layoutType);
		}
		#endregion
	}
}


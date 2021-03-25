// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Linq;
using static Crow.Logger;
namespace Crow {
	/// <summary>
	/// group container that stacked its children horizontally or vertically
	/// </summary>
	public class GenericStack : Group {
		#region CTOR
		protected GenericStack () { }
		public GenericStack (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region Private fields
		int spacing;
		Orientation orientation;
		#endregion

		#region Public Properties
		[DefaultValue (2)]
		public int Spacing {
			get => spacing;
			set {
				if (spacing == value)
					return;
				spacing = value;
				NotifyValueChangedAuto (spacing);
				RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue (Orientation.Horizontal)]
		public virtual Orientation Orientation {
			get => orientation;
			set {
				if (orientation == value)
					return;
				orientation = value;
				NotifyValueChangedAuto (orientation);
				RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			}
		}
		#endregion

		#region GraphicObject Overrides
		public override bool ArrangeChildren => true;
		public override void ChildrenLayoutingConstraints (ILayoutable layoutable, ref LayoutingType layoutType) {
			//Prevent child repositionning in the direction of stacking
			if (Orientation == Orientation.Horizontal)
				layoutType &= (~LayoutingType.X);
			else
				layoutType &= (~LayoutingType.Y);
		}
		public override int measureRawSize (LayoutingType lt) {
			int totSpace = Math.Max (0, Spacing * (Children.Count (c => c.Visible) - 1));
			if (lt == LayoutingType.Width) {
				if (Orientation == Orientation.Horizontal)
					return contentSize.Width + totSpace + 2 * Margin;
			} else if (Orientation == Orientation.Vertical)
				return contentSize.Height + totSpace + 2 * Margin;

			return base.measureRawSize (lt);
		}
		public virtual void ComputeChildrenPositions () {
			int d = 0;
			if (Orientation == Orientation.Horizontal) {
				foreach (Widget c in Children) {
					if (!c.Visible)
						continue;
					c.Slot.X = d;
					d += c.Slot.Width + Spacing;
				}
			} else {
				foreach (Widget c in Children) {
					if (!c.Visible)
						continue;
					c.Slot.Y = d;
					d += c.Slot.Height + Spacing;
				}
			}
			IsDirty = true;
		}
		Widget stretchedGO = null;
		public override bool UpdateLayout (LayoutingType layoutType) {
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren) {
				//allow 1 child to have size to 0 if stack has fixed or streched size policy,
				//this child will occupy remaining space
				//if stack size policy is Fit, no child may have stretch enabled
				//in the direction of stacking.
				ComputeChildrenPositions ();

				//if no layouting remains in queue for item, registre for redraw
				if (RegisteredLayoutings == LayoutingType.None && IsDirty)
					IFace.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout (layoutType);
		}
		//force computed slot for child
		protected void setChildWidth (Widget w, int newW) {
			if (w.MaximumSize.Width > 0)
				newW = Math.Min (newW, w.MaximumSize.Width);
				
			if (newW == w.Slot.Width)
				return;
			
			w.Slot.Width = newW;
			w.IsDirty = true;
			w.LayoutChanged -= OnChildLayoutChanges;
			w.OnLayoutChanges (LayoutingType.Width);
			w.LayoutChanged += OnChildLayoutChanges;
			w.LastSlots.Width = w.Slot.Width;
		}
		protected void setChildHeight (Widget w, int newH) {
			if (w.MaximumSize.Height > 0)
				newH = Math.Min (newH, w.MaximumSize.Height);
				
			if (newH == w.Slot.Height)
				return;

			w.Slot.Height = newH;
			w.IsDirty = true;
			w.LayoutChanged -= OnChildLayoutChanges;
			w.OnLayoutChanges (LayoutingType.Height);
			w.LayoutChanged += OnChildLayoutChanges;
			w.LastSlots.Height = w.Slot.Height;
		}
		void adjustStretchedGo (LayoutingType lt) {
			if (stretchedGO == null)
				return;
			if (lt == LayoutingType.Width)
				setChildWidth (stretchedGO, Math.Max (
					ClientRectangle.Width - contentSize.Width - Spacing * (Children.Count - 1),	stretchedGO.MinimumSize.Width));				
			else
				setChildHeight (stretchedGO, Math.Max (
					ClientRectangle.Height - contentSize.Height - Spacing * (Children.Count - 1), stretchedGO.MinimumSize.Height));			
		}

		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg) {
			Widget go = sender as Widget;
			//Debug.WriteLine ("child layout change: " + go.LastSlots.ToString() + " => " + go.Slot.ToString());
			switch (arg.LayoutType) {
			case LayoutingType.Width:
				if (Orientation == Orientation.Horizontal) {
					if (go.Width == Measure.Stretched) {
						if (stretchedGO == null && Width != Measure.Fit) {
							stretchedGO = go;
							contentSize.Width -= go.LastSlots.Width;
						} else if (stretchedGO != go) {
							go.Slot.Width = 0;
							go.Width = Measure.Fit;
							return;
						}
					} else if (stretchedGO == go) {
						stretchedGO = null;
						contentSize.Width += go.Slot.Width;
					} else
						contentSize.Width += go.Slot.Width - go.LastSlots.Width;


					adjustStretchedGo (LayoutingType.Width);

					if (Width == Measure.Fit)
						this.RegisterForLayouting (LayoutingType.Width);

					this.RegisterForLayouting (LayoutingType.ArrangeChildren);
					return;
				}
				break;
			case LayoutingType.Height:
				if (Orientation == Orientation.Vertical) {
					if (go.Height == Measure.Stretched) {
						if (stretchedGO == null && Height != Measure.Fit) {
							stretchedGO = go;
							contentSize.Height -= go.LastSlots.Height;
						} else if (stretchedGO != go) {
							go.Slot.Height = 0;
							go.Height = Measure.Fit;
							return;
						}
					} else if (stretchedGO == go) {
						stretchedGO = null;
						contentSize.Height += go.Slot.Height;
					} else
						contentSize.Height += go.Slot.Height - go.LastSlots.Height;

					adjustStretchedGo (LayoutingType.Height);

					if (Height == Measure.Fit)
						this.RegisterForLayouting (LayoutingType.Height);

					this.RegisterForLayouting (LayoutingType.ArrangeChildren);
					return;
				}
				break;
			}
			base.OnChildLayoutChanges (sender, arg);
		}
		#endregion

		public override void RemoveChild (Widget child) {
			if (child != stretchedGO) {
				if (Orientation == Orientation.Horizontal)
					contentSize.Width -= child.LastSlots.Width;
				else
					contentSize.Height -= child.LastSlots.Height;
			}
			base.RemoveChild (child);
			if (child == stretchedGO) {
				stretchedGO = null;
				RegisterForLayouting (LayoutingType.Sizing);
			} else if (Orientation == Orientation.Horizontal)
				adjustStretchedGo (LayoutingType.Width);
			else
				adjustStretchedGo (LayoutingType.Height);
		}

		public override void ClearChildren () {
			base.ClearChildren ();
			stretchedGO = null;
		}
	}
}

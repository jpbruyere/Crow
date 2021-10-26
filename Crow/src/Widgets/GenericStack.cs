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

		#region Widget Overrides
		public override bool ArrangeChildren => true;
		public override void ChildrenLayoutingConstraints (ILayoutable layoutable, ref LayoutingType layoutType) {
			//Prevent child repositionning in the direction of stacking
			if (Orientation == Orientation.Horizontal)
				layoutType &= (~LayoutingType.X);
			else
				layoutType &= (~LayoutingType.Y);
		}
		public override int measureRawSize (LayoutingType lt) {
			DbgLogger.StartEvent(DbgEvtType.GOMeasure, this, lt);
			try {

				int totSpace = Math.Max (0, Spacing * (Children.Count (c => c.IsVisible) - 1));
				if (lt == LayoutingType.Width) {
					if (Orientation == Orientation.Horizontal)
						//return contentSize.Width + totSpace + 2 * Margin;
						return stretchedGO == null ?
							contentSize.Width + totSpace + 2 * Margin :
							contentSize.Width + stretchedGO.measureRawSize(lt) + totSpace + 2 * Margin;
				} else if (Orientation == Orientation.Vertical)
					//return contentSize.Height + totSpace + 2 * Margin;
					return stretchedGO == null ?
						contentSize.Height + totSpace + 2 * Margin :
						contentSize.Height + stretchedGO.measureRawSize(lt) + totSpace + 2 * Margin;

				return base.measureRawSize (lt);
			} finally {
				DbgLogger.EndEvent(DbgEvtType.GOMeasure);
			}
		}
		public virtual void ComputeChildrenPositions () {
			DbgLogger.StartEvent(DbgEvtType.GOComputeChildrenPositions, this);
			int d = 0;
			if (Orientation == Orientation.Horizontal) {
				foreach (Widget c in Children) {
					if (!c.IsVisible)
						continue;
					if (c.Slot.X != d) {
						c.Slot.X = d;
						c.OnLayoutChanges (LayoutingType.X);
						c.LastSlots.X = c.Slot.X;
						IsDirty = true;
					}
					d += c.Slot.Width + Spacing;
				}
			} else {
				foreach (Widget c in Children) {
					if (!c.IsVisible)
						continue;
					if (c.Slot.Y != d) {
						c.Slot.Y = d;
						c.OnLayoutChanges (LayoutingType.Y);
						c.LastSlots.Y = c.Slot.Y;
						IsDirty = true;
					}
					d += c.Slot.Height + Spacing;
				}
			}
			DbgLogger.EndEvent(DbgEvtType.GOComputeChildrenPositions);
		}
		Widget stretchedGO = null;
		public override bool UpdateLayout (LayoutingType layoutType) {
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren) {
				//allow 1 child to have stretched size,
				//this child will occupy remaining space
				//if stack size policy is Fit, no child may have stretch enabled
				//in the direction of stacking.
				ComputeChildrenPositions ();
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
			DbgLogger.SetMsg(DbgEvtType.GOAdjustStretchedGo, $"new width={newW}");
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
			DbgLogger.SetMsg(DbgEvtType.GOAdjustStretchedGo, $"new height={newH}");
		}
		internal void adjustStretchedGo (LayoutingType lt) {
			if (stretchedGO == null)
				return;
			//Console.WriteLine ($"adjust stretched go: {stretchedGO} {lt}");
			DbgLogger.StartEvent(DbgEvtType.GOAdjustStretchedGo, this);
			if (lt == LayoutingType.Width)
				setChildWidth (stretchedGO, Math.Max (
					ClientRectangle.Width - contentSize.Width - Spacing * (Children.Count - 1),	stretchedGO.MinimumSize.Width));
			else
				setChildHeight (stretchedGO, Math.Max (
					ClientRectangle.Height - contentSize.Height - Spacing * (Children.Count - 1), stretchedGO.MinimumSize.Height));
			DbgLogger.EndEvent(DbgEvtType.GOAdjustStretchedGo);
		}

		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg) {
			DbgLogger.StartEvent(DbgEvtType.GOOnChildLayoutChange, this);
			try {
				Widget go = sender as Widget;
				switch (arg.LayoutType) {
				case LayoutingType.Width:
					if (Orientation == Orientation.Horizontal) {
						if (go.Width == Measure.Stretched) {
							if (stretchedGO == null && Width != Measure.Fit) {
								stretchedGO = go;
								contentSize.Width -= go.LastSlots.Width;
								DbgLogger.SetMsg (DbgEvtType.GOOnChildLayoutChange, $"new stretched go: {stretchedGO}");
							} else if (stretchedGO != go) {
								go.Slot.Width = 0;
								go.Width = Measure.Fit;
								DbgLogger.SetMsg (DbgEvtType.GOOnChildLayoutChange, $"force stretched width to Fit: {go}");
								return;
							}
						} else if (stretchedGO == go) {
							stretchedGO = null;
							contentSize.Width += go.Slot.Width;
							DbgLogger.SetMsg (DbgEvtType.GOOnChildLayoutChange, $"reset stretched go");
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
								DbgLogger.SetMsg (DbgEvtType.GOOnChildLayoutChange, $"new stretched go: {stretchedGO}");
							} else if (stretchedGO != go) {
								go.Slot.Height = 0;
								go.Height = Measure.Fit;
								DbgLogger.SetMsg (DbgEvtType.GOOnChildLayoutChange, $"force stretched width to Fit: {go}");
								return;
							}
						} else if (stretchedGO == go) {
							stretchedGO = null;
							contentSize.Height += go.Slot.Height;
							DbgLogger.SetMsg (DbgEvtType.GOOnChildLayoutChange, $"reset stretched go");
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
			} finally {
				DbgLogger.EndEvent(DbgEvtType.GOOnChildLayoutChange);
			}
		}
		#endregion

		public override void AddChild (Widget child) {
			base.AddChild (child);
			if (Orientation == Orientation.Horizontal) {
				if (child.Width == Measure.Stretched)
					child.RegisterForLayouting (LayoutingType.Width);
				else
					contentSize.Width += child.LastSlots.Width;
			}else if (child.Height == Measure.Stretched)
				child.RegisterForLayouting (LayoutingType.Height);
			else
				contentSize.Height += child.LastSlots.Height;
		}
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
		protected override string LogName => "gs";
	}
}

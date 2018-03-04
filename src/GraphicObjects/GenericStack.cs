//
// GenericStack.cs
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

using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using System;

namespace Crow
{
	/// <summary>
	/// group container that stacked its children horizontally or vertically
	/// </summary>
	public class GenericStack : Group
    {
		#region CTOR
		protected GenericStack() : base(){}
		public GenericStack(Interface iface) : base(iface){}
		#endregion

		#region Private fields
        int _spacing;
        Orientation _orientation;
		#endregion

		#region Public Properties
        [XmlAttributeAttribute()][DefaultValue(2)]
        public int Spacing
        {
			get { return _spacing; }
            set { 
				if (_spacing == value)
					return;
				_spacing = value; 
				NotifyValueChanged ("Spacing", Spacing);
				RegisterForLayouting (LayoutingType.Sizing|LayoutingType.ArrangeChildren);
			}
        }
        [XmlAttributeAttribute()][DefaultValue(Orientation.Horizontal)]
        public virtual Orientation Orientation
        {
            get { return _orientation; }
            set { _orientation = value; }
        }
		#endregion

		#region GraphicObject Overrides
		public override bool ArrangeChildren { get { return true; } }
		public override void ChildrenLayoutingConstraints (ref LayoutingType layoutType)
		{
			//Prevent child repositionning in the direction of stacking
			if (Orientation == Orientation.Horizontal)
				layoutType &= (~LayoutingType.X);
			else
				layoutType &= (~LayoutingType.Y);			
		}
		protected override int measureRawSize (LayoutingType lt)
		{
			int totSpace = 0;
			for (int i = 0; i < Children.Count; i++) {
				if (Children [i].Visible)
					totSpace += Spacing;
			}
			if (totSpace > 0)
				totSpace -= Spacing;
			if (lt == LayoutingType.Width) {
				if (Orientation == Orientation.Horizontal)
					return contentSize.Width + totSpace + 2 * Margin;
			}else if (Orientation == Orientation.Vertical)
				return contentSize.Height + totSpace + 2 * Margin;							
			
			return base.measureRawSize (lt);
		}
		public virtual void ComputeChildrenPositions()
		{
			int d = 0;
			if (Orientation == Orientation.Horizontal) {
				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;
					c.Slot.X = d;
					d += c.Slot.Width + Spacing;
				}
			} else {
				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;					
					c.Slot.Y = d;
					d += c.Slot.Height + Spacing;
				}
			}
			IsDirty = true;
		}
		GraphicObject stretchedGO = null;
		public override bool UpdateLayout (LayoutingType layoutType)
        {
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

			return base.UpdateLayout(layoutType);
        }

		void adjustStretchedGo (LayoutingType lt){
			if (stretchedGO == null)
				return;
			if (lt == LayoutingType.Width) {
				int newW = Math.Max (
					           this.ClientRectangle.Width - contentSize.Width - Spacing * (Children.Count - 1),
					           stretchedGO.MinimumSize.Width);
				if (stretchedGO.MaximumSize.Width > 0)
					newW = Math.Min (newW, stretchedGO.MaximumSize.Width);
				if (newW != stretchedGO.Slot.Width) {							
					stretchedGO.Slot.Width = newW;
					stretchedGO.IsDirty = true;
					#if DEBUG_LAYOUTING
				Debug.WriteLine ("\tAdjusting Width of " + stretchedGO.ToString());
					#endif
					stretchedGO.LayoutChanged -= OnChildLayoutChanges;
					stretchedGO.OnLayoutChanges (LayoutingType.Width);
					stretchedGO.LayoutChanged += OnChildLayoutChanges;
					stretchedGO.LastSlots.Width = stretchedGO.Slot.Width;
				}
			} else {
				int newH = Math.Max (
					this.ClientRectangle.Height - contentSize.Height - Spacing * (Children.Count - 1),
					stretchedGO.MinimumSize.Height);
				if (stretchedGO.MaximumSize.Height > 0)
					newH = Math.Min (newH, stretchedGO.MaximumSize.Height);
				if (newH != stretchedGO.Slot.Height) {
					stretchedGO.Slot.Height = newH;
					stretchedGO.IsDirty = true;
					#if DEBUG_LAYOUTING
					Debug.WriteLine ("\tAdjusting Height of " + stretchedGO.ToString());
					#endif
					stretchedGO.LayoutChanged -= OnChildLayoutChanges;
					stretchedGO.OnLayoutChanges (LayoutingType.Height);
					stretchedGO.LayoutChanged += OnChildLayoutChanges;
					stretchedGO.LastSlots.Height = stretchedGO.Slot.Height;
				}				
			}
		}

		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			GraphicObject go = sender as GraphicObject;
			//Debug.WriteLine ("child layout change: " + go.LastSlots.ToString() + " => " + go.Slot.ToString());
			switch (arg.LayoutType) {
			case LayoutingType.Width:
				if (Orientation == Orientation.Horizontal) {
					if (go.Width == Measure.Stretched) {
						if (stretchedGO == null && Width != Measure.Fit)
							stretchedGO = go;
						else if (stretchedGO != go) {
							go.Slot.Width = 0;
							go.Width = Measure.Fit;
							return;
						}
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
						if (stretchedGO == null && Height != Measure.Fit)
							stretchedGO = go;
						else if (stretchedGO != go){
							go.Slot.Height = 0;
							go.Height = Measure.Fit;
							return;
						}
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

    	public override void RemoveChild (GraphicObject child)
		{
			base.RemoveChild (child);
			if (child == stretchedGO) {
				stretchedGO = null;
				RegisterForLayouting (LayoutingType.Sizing);
				return;
			}
			if (Orientation == Orientation.Horizontal) {
				contentSize.Width -= child.LastSlots.Width;
				adjustStretchedGo (LayoutingType.Width);
			} else {
				contentSize.Height -= child.LastSlots.Height;
				adjustStretchedGo (LayoutingType.Height);
			}
		}
	}
}

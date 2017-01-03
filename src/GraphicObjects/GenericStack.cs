using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using System;

namespace Crow
{
	public class GenericStack : Group
    {
		#region CTOR
		public GenericStack()
			: base()
		{            
		}
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
					CurrentInterface.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout(layoutType);
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

					if (stretchedGO != null) {
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
					}
					
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
					
					if (stretchedGO != null) {
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

    
	}
}

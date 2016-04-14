using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;

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
			int tmp = 0;
			switch (lt) {
			case LayoutingType.Width:
				if (Orientation == Orientation.Horizontal) {
					if (Children.Count > 0) {
						foreach (GraphicObject c in Children) {
							if (!c.Visible)
								continue;
							if (c.RegisteredLayoutings.HasFlag (LayoutingType.Width))
								return -1;
							tmp += c.Slot.Width + Spacing;
						}
						tmp -= Spacing;
					}
					break;
				} 
				return base.measureRawSize (lt);				
			case LayoutingType.Height:
				if (Orientation == Orientation.Vertical) {
					if (Children.Count > 0) {
						foreach (GraphicObject c in Children) {
							if (!c.Visible)
								continue;
							if (c.RegisteredLayoutings.HasFlag (LayoutingType.Height))
								return -1;
							tmp += c.Slot.Height + Spacing;
						}
						tmp -= Spacing;
					}
					break;
				}
				return base.measureRawSize (lt);
			}

			return tmp + 2 * Margin;
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
					c.RegisterForLayouting (LayoutingType.Y);
				}
			} else {
				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;					
					c.Slot.Y = d;
					d += c.Slot.Height + Spacing;
					c.RegisterForLayouting (LayoutingType.X);
				}
			}
			bmp = null;
		}
			
		public override bool UpdateLayout (LayoutingType layoutType)
        {
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren) {
				//allow 1 child to have size to 0 if stack has fixed or streched size policy,
				//this child will occupy remaining space
				//if stack size policy is Fit, no child may have stretch enabled
				//in the direction of stacking.
				if (Orientation == Orientation.Horizontal) {
					GraphicObject stretchedGO = null;
					int tmpWidth = Slot.Width;
					int cptChildren = 0;
					for (int i = 0; i < Children.Count; i++) {
						if (!Children [i].Visible)
							continue;
						//requeue Positionning if child is not layouted
						if (Children [i].RegisteredLayoutings.HasFlag (LayoutingType.Width))
							return false;
						cptChildren++;
						if (Children [i].Width == Measure.Stretched) {
							if (!(stretchedGO == null && Width != Measure.Fit)) {
								//change size policy of other stretched children
								Children [i].Width = Measure.Fit;
								return false;
							}
							stretchedGO = Children [i];
							if (i < Children.Count - 1)
								tmpWidth -= Spacing;
							continue;
						}
						tmpWidth -= Children [i].Slot.Width + Spacing;
					}
					if (stretchedGO != null && Width != Measure.Fit) {
						tmpWidth += (Spacing - 2 * Margin);
						if (tmpWidth < MinimumSize.Width)
							tmpWidth = MinimumSize.Width;
						else if (tmpWidth > MaximumSize.Width && MaximumSize.Width > 0)
							tmpWidth = MaximumSize.Width;
						if (stretchedGO.LastSlots.Width != tmpWidth) {
							stretchedGO.Slot.Width = tmpWidth;
							stretchedGO.bmp = null;
							#if DEBUG_LAYOUTING
							Debug.WriteLine ("\tAdjusting Width of " + stretchedGO.ToString());
							#endif
							stretchedGO.OnLayoutChanges (LayoutingType.Width);
							stretchedGO.LastSlots.Width = stretchedGO.Slot.Width;
						}
					}
				} else {
					GraphicObject stretchedGO = null;
					int tmpHeight = Slot.Height;
					int cptChildren = 0;
					for (int i = 0; i < Children.Count; i++) {
						if (!Children [i].Visible)
							continue;
						if (Children [i].RegisteredLayoutings.HasFlag (LayoutingType.Height))
							return false;
						cptChildren++;
						if (Children [i].Height == Measure.Stretched) {
							if (!(stretchedGO == null && Height != Measure.Fit)){
								Children [i].Height = Measure.Fit;
								return false;
							}
							stretchedGO = Children [i];
							if (i < Children.Count - 1)
								tmpHeight -= Spacing;
							continue;
						}
						tmpHeight -= Children[i].Slot.Height + Spacing;
					}
					if (stretchedGO != null && Height != Measure.Fit) {
						tmpHeight += (Spacing - 2 * Margin);
						if (tmpHeight < MinimumSize.Height)
							tmpHeight = MinimumSize.Height;
						else if (tmpHeight > MaximumSize.Height && MaximumSize.Height > 0)
							tmpHeight = MaximumSize.Height;
						if (stretchedGO.LastSlots.Height != tmpHeight) {
							stretchedGO.Slot.Height = tmpHeight;
							stretchedGO.bmp = null;
							#if DEBUG_LAYOUTING
							Debug.WriteLine ("\tAdjusting Height of " + stretchedGO.ToString());
							#endif
							stretchedGO.OnLayoutChanges (LayoutingType.Height);
							stretchedGO.LastSlots.Height = stretchedGO.Slot.Height;
						}
					}
				}

				ComputeChildrenPositions ();

				//if no layouting remains in queue for item, registre for redraw
				if (RegisteredLayoutings == LayoutingType.None && bmp==null)
					this.AddToRedrawList ();

				return true;
			}

			return base.UpdateLayout(layoutType);
        }

		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			base.OnChildLayoutChanges (sender, arg);

			switch (arg.LayoutType) {
			case LayoutingType.Width:
				if (Orientation == Orientation.Horizontal) {
					if (Width == Measure.Fit)
						this.RegisterForLayouting (LayoutingType.Width);
					this.RegisterForLayouting (LayoutingType.ArrangeChildren);
				}
				break;
			case LayoutingType.Height:
				if (Orientation == Orientation.Vertical) {
					if (Height == Measure.Fit)
						this.RegisterForLayouting (LayoutingType.Height);
					this.RegisterForLayouting (LayoutingType.ArrangeChildren);
				}
				break;
			}
		}
		#endregion

    
	}
}

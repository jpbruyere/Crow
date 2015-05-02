using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
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

		public override T addChild<T> (T child)
		{
			T tmp = base.addChild (child);
			this.RegisterForLayouting ((int)LayoutingType.PositionChildren);
			return tmp;
		}
		public override void removeChild (GraphicObject child)
		{
			base.removeChild (child);
			this.RegisterForLayouting ((int)LayoutingType.PositionChildren);
		}

		#region Public Properties
        [XmlAttributeAttribute()][DefaultValue(2)]
        public int Spacing
        {
			get { return _spacing; }
            set { _spacing = value; }
        }
        [XmlAttributeAttribute()][DefaultValue(Orientation.Horizontal)]
        public virtual Orientation Orientation
        {
            get { return _orientation; }
            set { _orientation = value; }
        }
		#endregion

		#region GraphicObject Overrides
		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Width {
			get { return base.Width; }
			set { base.Width = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}

		protected override Size measureRawSize ()
		{
			Size tmp = new Size ();

			if (Orientation == Orientation.Horizontal) {
				foreach (GraphicObject c in Children) {
					tmp.Width += c.Slot.Width + Spacing;
					tmp.Height = Math.Max (tmp.Height, c.Slot.Bottom);
				}
			} else {
				foreach (GraphicObject c in Children) {
					tmp.Width = Math.Max (tmp.Width, c.Slot.Right);
					tmp.Height += c.Slot.Height + Spacing;
				}
			}

			tmp.Width += 2*Margin;
			tmp.Height += 2*Margin;

			return tmp;
		}
		public virtual void ComputeChildrenPositions()
		{
			int d = 0;
			if (Orientation == Orientation.Horizontal) {
				foreach (GraphicObject c in Children) {
					c.Slot.X = d;
					d += c.Slot.Width + Spacing;
				}
			} else {
				foreach (GraphicObject c in Children) {
					c.Slot.Y = d;
					d += c.Slot.Height + Spacing;
				}
			}
		}

		public override void RegisterForLayouting (int layoutType)
		{			
			base.RegisterForLayouting (layoutType);

			if ((layoutType & (int)LayoutingType.PositionChildren) > 0)
				Interface.LayoutingQueue.Enqueue (LayoutingType.PositionChildren, this);
		}
		public override void UpdateLayout (LayoutingType layoutType)
        {            
			if (layoutType == LayoutingType.PositionChildren)
				ComputeChildrenPositions ();
			else
				base.UpdateLayout(layoutType);
        }
		#endregion

    
	}
}

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
		bool childrenArePositionned = false;
        int _spacing;
        Orientation _orientation;
		#endregion

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
		[XmlIgnore]public override bool LayoutIsValid {
			get { return childrenArePositionned && base.LayoutIsValid; }
			set { base.LayoutIsValid = value; }
		}

		public override void InvalidateLayout ()
		{
			childrenArePositionned = false;
			base.InvalidateLayout ();
		}
		protected override Size measureRawSize ()
		{
			Size raw = Bounds.Size;
			Size tmp = new Size ();

			if (raw.Width >= 0 && raw.Height >= 0)
				return raw;
				
			foreach (GraphicObject c in Children) {
				if (raw.Width < 0) {
					if (c.WIsValid) {
						if (Orientation == Orientation.Horizontal && c.Bounds.Width != 0)
							tmp.Width += c.Slot.Width + Spacing;
						else
							tmp.Width = Math.Max (tmp.Width, c.Slot.Right);
					}else
						return raw;
				}
				if (raw.Height < 0) {
					if (c.HIsValid) {
						if (Orientation == Orientation.Vertical && c.Bounds.Height != 0)
							tmp.Height += c.Slot.Height + Spacing;
						else
							tmp.Height = Math.Max (tmp.Height, c.Slot.Bottom);
					}else
						return raw;
				}
			}

			if (raw.Width < 0)
				tmp.Width += 2*Margin;
			if (raw.Height < 0)
				tmp.Height += 2*Margin;

			return tmp;
		}
		public virtual void ComputeChildrenPositions()
		{
			int d = 0;
			if (Orientation == Orientation.Horizontal) {
				foreach (GraphicObject c in Children) {
					if (c.Bounds.Width == 0)
						continue;

					c.Slot.X = d;
					c.XIsValid = true;
					d += c.Slot.Width + Spacing;
				}
			} else {
				foreach (GraphicObject c in Children) {
					if (c.Bounds.Height == 0)
						continue;

					c.Slot.Y = d;
					c.YIsValid = true;
					d += c.Slot.Height + Spacing;
				}
			}
			childrenArePositionned = true;
		}

        public override void UpdateLayout()
        {            
            base.UpdateLayout();

			ComputeChildrenPositions ();

            if (LayoutIsValid)
                registerForRedraw();
        }
		#endregion

		//
//        bool enoughtSpaceForWidget(GraphicObject w)
//        {
//            if (!SizeToContent)
//            {
//                int nextXForWidget = 0;
//                int nextYForWidget = 0;
//
//                if (Orientation == Orientation.Horizontal)
//                    nextXForWidget = currentXForWidget + w.Slot.Width;
//                else
//                    nextYForWidget = nextYForWidget + w.Slot.Height;
//
//
//                if (nextXForWidget > ClientRectangle.Right )
//                    return false;
//                if (currentYForWidget > ClientRectangle.Bottom )
//                    return false;
//            }
//            return true;
//        }
//        void advance(GraphicObject w)
//        {
//            if (Orientation == Orientation.Horizontal)
//                currentXForWidget = currentXForWidget + WidgetSpacing + w.Slot.Width;
//            else
//                currentYForWidget = currentYForWidget + WidgetSpacing + w.Slot.Height;
//
//        }
    
	}
}

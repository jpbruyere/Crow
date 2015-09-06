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
				foreach (GraphicObject c in Children.Where(ch=>ch.Visible)) {
					tmp.Width += c.Slot.Width + Spacing;
					tmp.Height = Math.Max (tmp.Height, c.Slot.Bottom);
				}
				if (tmp.Width > 0)
					tmp.Width -= Spacing;
			} else {
				foreach (GraphicObject c in Children.Where(ch=>ch.Visible)) {
					tmp.Width = Math.Max (tmp.Width, c.Slot.Right);
					tmp.Height += c.Slot.Height + Spacing;
				}
				if (tmp.Height > 0)
					tmp.Height -= Spacing;
			}

			tmp.Width += 2 * Margin;
			tmp.Height += 2 * Margin;

			return tmp;
		}
		public virtual void ComputeChildrenPositions()
		{
			int d = 0;
			if (Orientation == Orientation.Horizontal) {
				foreach (GraphicObject c in Children.Where(ch=>ch.Visible)) {
					c.Slot.X = d;
					d += c.Slot.Width + Spacing;
					c.RegisterForLayouting ((int)LayoutingType.Y);
				}
			} else {
				foreach (GraphicObject c in Children.Where(ch=>ch.Visible)) {
					c.Slot.Y = d;
					d += c.Slot.Height + Spacing;
					c.RegisterForLayouting ((int)LayoutingType.X);
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
			if (layoutType == LayoutingType.PositionChildren) {
				//allow 1 child to have size to 0 if stack has fixed or streched size,
				//this child will occupy remaining space
				if (Orientation == Orientation.Horizontal) {
					if (Width >= 0) {
						GraphicObject[] gobjs = Children.Where (c => c.Width == 0 && c.Visible).ToArray();
						if (gobjs.Length > 1)
							throw new Exception ("Only one child in stack may have size to stretched");
						else if (gobjs.Length == 1) {
							int sz = Children.Where(ch=>ch.Visible).Except (gobjs).Sum (g => g.Slot.Width);
							if (sz < Slot.Width) {
								gobjs [0].Slot.Width = Slot.Width - sz - (Children.Count-1) * Spacing;
								int idx = Children.IndexOf (gobjs [0]);
								if (idx > 0 && idx < Children.Count - 1)
									gobjs [0].Slot.Width -= Spacing;
								if (gobjs [0].LastSlots.Width != gobjs [0].Slot.Width) {
									gobjs [0].bmp = null;
									//gobjs [0].OnLayoutChanges (LayoutingType.Width);
									gobjs [0].LastSlots.Width = gobjs [0].Slot.Width;
								}
							}
						}
					}					
				} else {
					if (Height >= 0) {
						GraphicObject[] gobjs = Children.Where(ch=>ch.Visible).Where (c => c.Height == 0).ToArray();
						if (gobjs.Length > 1)
							throw new Exception ("Only one child in stack may have size to stretched");
						else if (gobjs.Length == 1) {
							int sz = Children.Where(ch=>ch.Visible).Except (gobjs).Sum (g => g.Slot.Height);
							if (sz < Slot.Height) {
								gobjs [0].Slot.Height = Slot.Height - sz- (Children.Count-1) * Spacing;
								int idx = Children.IndexOf (gobjs [0]);
								if (idx > 0 && idx < Children.Count - 1)
									gobjs [0].Slot.Height -= Spacing;
								if (gobjs [0].LastSlots.Height != gobjs [0].Slot.Height) {
									gobjs [0].bmp = null;
									gobjs [0].LastSlots.Height = gobjs [0].Slot.Height;
								}
							}
						}
					}
				}				
				ComputeChildrenPositions ();
				//if no layouting remains in queue for item, registre for redraw
				if (Interface.LayoutingQueue.Where (lq => lq.GraphicObject == this).Count () <= 0 && bmp==null)
					this.RegisterForRedraw ();
			}else
				base.UpdateLayout(layoutType);
        }
		#endregion

    
	}
}

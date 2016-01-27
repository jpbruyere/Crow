using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// Simple grid container
	/// Allow symetric placement of children on a grid,
	/// excedental child (above grid sizing) are ignored
	/// and invisible child keep their place in the grid
	/// </summary>
    public class Grid : Group
    {
		#region CTOR
		public Grid()
			: base()
		{            
		}
		#endregion

		#region Private fields
        int _spacing;
		int _columnCount;
		int _rowCount;
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
        [XmlAttributeAttribute()][DefaultValue(1)]
        public virtual int ColumnCount
        {
            get { return _columnCount; }
            set { 
				if (_columnCount == value)
					return;

				_columnCount = value; 

				NotifyValueChanged ("ColumnCount", ColumnCount);
				this.RegisterForLayouting ((int)LayoutingType.PositionChildren);
			}
        }
		[XmlAttributeAttribute()][DefaultValue(1)]
		public virtual int RowCount
		{
			get { return _rowCount; }
			set { 
				if (_rowCount == value)
					return;

				_rowCount = value; 

				NotifyValueChanged ("RowCount", RowCount);
				this.RegisterForLayouting ((int)LayoutingType.PositionChildren);
			}
		}
		public virtual int CaseWidth {
			get { return (Slot.Width - (ColumnCount - 1) * Spacing) / ColumnCount; }
		}
		public virtual int CaseHeight {
			get { return (Slot.Height - (RowCount - 1) * Spacing) / RowCount; }
		}

		#endregion

		#region GraphicObject Overrides
//		protected override Size measureRawSize ()
//		{
//			Size tmp = new Size ();
//
//			foreach (GraphicObject c in Children.Where(ch=>ch.Visible)) {
//				tmp.Width = Math.Max (tmp.Width, c.Slot.Width);
//				tmp.Height = Math.Max (tmp.Height, c.Slot.Height);
//			}
//
//			tmp.Width *= (ColumnCount - 1) * Spacing / ColumnCount;;
//			tmp.Height *= (RowCount - 1) * Spacing / RowCount;
//			tmp.Width += 2 * Margin;
//			tmp.Height += 2 * Margin;
//
//			return tmp;
//		}
		public virtual void ComputeChildrenPositions()
		{
			int slotWidth = CaseWidth;
			int slotHeight = CaseHeight;
			for (int curY = 0; curY < RowCount; curY++) {
				for (int curX = 0; curX < ColumnCount; curX++) {
					int idx = curY * ColumnCount + curX;
					if (idx >= Children.Count)
						return;
					GraphicObject c = Children [idx];
					if (!c.Visible)
						continue;
					//ensure Item are not realigned
					c.HorizontalAlignment = HorizontalAlignment.Left;
					c.VerticalAlignment = VerticalAlignment.Top;
					c.Left = curX * (slotWidth + Spacing);
					c.Top = curY * (slotHeight + Spacing);
					c.Width = slotWidth;
					c.Height = slotHeight;
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

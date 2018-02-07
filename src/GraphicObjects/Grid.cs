//
// Grid.cs
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
		protected Grid () : base(){}
		public Grid(Interface iface) : base(iface)
		{            
		}
		#endregion

		#region Private fields
        int _spacing;
		int _columnCount;
		int _rowCount;
		#endregion

		#region Public Properties
        [XmlAttributeAttribute()][DefaultValue(2)]
        public int Spacing
        {
			get { return _spacing; }
            set { _spacing = value; }
        }
        [XmlAttributeAttribute()][DefaultValue(2)]
        public virtual int ColumnCount
        {
            get { return _columnCount; }
            set { 
				if (_columnCount == value)
					return;

				_columnCount = value; 

				NotifyValueChanged ("ColumnCount", ColumnCount);
				this.RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
        }
		[XmlAttributeAttribute()][DefaultValue(2)]
		public virtual int RowCount
		{
			get { return _rowCount; }
			set { 
				if (_rowCount == value)
					return;

				_rowCount = value; 

				NotifyValueChanged ("RowCount", RowCount);
				this.RegisterForLayouting (LayoutingType.ArrangeChildren);
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
		public override void ChildrenLayoutingConstraints (ref LayoutingType layoutType)
		{
			//Prevent child repositionning
			layoutType &= (~LayoutingType.Positioning);
		}
		public override bool ArrangeChildren { get { return true; } }
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
					c.Slot.X = curX * (slotWidth + Spacing);
					c.Slot.Y = curY * (slotHeight + Spacing);
					//c.Slot.Width = slotWidth;
					//c.Slot.Height = slotHeight;
				}
			}
			IsDirty = true;
		}
		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			//base.OnChildLayoutChanges (sender, arg);
		}

		public override bool UpdateLayout (LayoutingType layoutType)
		{
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren) {				

				ComputeChildrenPositions ();

				//if no layouting remains in queue for item, registre for redraw
				if (RegisteredLayoutings == LayoutingType.None && IsDirty)
					CurrentInterface.EnqueueForRepaint (this);
				
				return true;
			}

			return base.UpdateLayout(layoutType);
		}
		#endregion

    
	}
}

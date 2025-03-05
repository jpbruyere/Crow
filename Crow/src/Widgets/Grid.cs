// Copyright (c) 2013-2025  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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
		public Grid(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region Private fields
		int spacing;
		int columnCount;
		int rowCount;
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
				RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue(2)]
		public virtual int ColumnCount
		{
			get { return columnCount; }
			set {
				if (columnCount == value)
					return;

				columnCount = value;

				NotifyValueChangedAuto (ColumnCount);
				RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue(2)]
		public virtual int RowCount
		{
			get { return rowCount; }
			set {
				if (rowCount == value)
					return;

				rowCount = value;

				NotifyValueChangedAuto (RowCount);
				RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		public virtual int CaseWidth => (Slot.Width - (ColumnCount - 1) * Spacing) / ColumnCount;
		public virtual int CaseHeight => (Slot.Height - (RowCount - 1) * Spacing) / RowCount;
		#endregion

		#region Widget Overrides
//		protected override Size measureRawSize ()
//		{
//			Size tmp = new Size ();
//
//			foreach (Widget c in Children.Where(ch=>ch.Visible)) {
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
		public override void ChildrenLayoutingConstraints (ILayoutable layoutable, ref LayoutingType layoutType)
		{
			//Prevent child repositionning
			layoutType &= (~LayoutingType.Sizing);
		}
		public override bool ArrangeChildren => true;
		public virtual void ComputeChildrenPositions()
		{
			int slotWidth = CaseWidth;
			int slotHeight = CaseHeight;
			for (int curY = 0; curY < RowCount; curY++) {
				for (int curX = 0; curX < ColumnCount; curX++) {
					int idx = curY * ColumnCount + curX;
					if (idx >= Children.Count)
						return;
					Widget c = Children [idx];
					if (!c.IsVisible)
						continue;
					int x = curX * (slotWidth + Spacing);
					if (c.Slot.X != x) {
						c.Slot.X = x;
						c.OnLayoutChanges (LayoutingType.X);
						c.LastSlots.X = c.Slot.X;
						IsDirty = true;
					}
					int y = curY * (slotHeight + Spacing);
					if (c.Slot.Y != y) {
						c.Slot.Y = y;
						c.OnLayoutChanges (LayoutingType.Y);
						c.LastSlots.Y = c.Slot.Y;
						IsDirty = true;
					}
				}
			}
			IsDirty = true;
		}
        public override void RegisterForLayouting(LayoutingType layoutType)
        {
            base.RegisterForLayouting(layoutType);
        }
        public override void OnLayoutChanges(LayoutingType layoutType)
        {
            base.OnLayoutChanges(layoutType);
        }
        public override void OnChildLayoutChanges(object sender, LayoutingEventArgs arg)
        {
            base.OnChildLayoutChanges(sender, arg);
        }

        public override bool UpdateLayout (LayoutingType layoutType)
		{
			if (layoutType == LayoutingType.ArrangeChildren) {
				RegisteredLayoutings &= (~layoutType);

				ComputeChildrenPositions ();
				return true;
			}

			return base.UpdateLayout(layoutType);
		}
		#endregion


	}
}

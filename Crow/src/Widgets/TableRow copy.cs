// Copyright (c) 2019-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;
using System.Linq;

using Glfw;

namespace Crow
{
	public class TableRow2 : Group, ISelectable {
		#region ISelectable implementation
		bool isSelected;
		public event EventHandler Selected;
		public event EventHandler Unselected;
		[DefaultValue (false)]
		public virtual bool IsSelected {
			get { return isSelected; }
			set {
				if (isSelected == value)
					return;
				isSelected = value;

				if (isSelected)
					Selected.Raise (this, null);
				else
					Unselected.Raise (this, null);

				NotifyValueChangedAuto (isSelected);
			}
		}
		#endregion

		public Table Table => Parent as Table;		

		public override void ChildrenLayoutingConstraints(ILayoutable layoutable, ref LayoutingType layoutType)
		{
			//trigger layouting for width only in the first row, the other will be set at the same horizontal position and width.
			if (Table == null) {
				base.ChildrenLayoutingConstraints (layoutable, ref layoutType);
				return;
			}
			if (this == Table.Children[0])
				layoutType &= (~LayoutingType.X);	
			else
				layoutType &= (~(LayoutingType.X|LayoutingType.Width));			
		}


		public override void OnLayoutChanges(LayoutingType  layoutType)
		{
			switch (layoutType) {
			case LayoutingType.Width:
				RegisterForLayouting (LayoutingType.X);
				break;
			case LayoutingType.Height:
				childrenRWLock.EnterReadLock ();
				foreach (Widget c in Children.Where (cc => cc.Height.IsRelativeToParent))
					c.RegisterForLayouting (LayoutingType.Height);				
				childrenRWLock.ExitReadLock ();
				break;
			}
			raiseLayoutChanged (layoutType);
		}

		//all the rows have equal size
		protected override void searchLargestChild(bool forceMeasure = false) {}




		/*public override void ComputeChildrenPositions () {
			if (Children.Count == 0)
				return;
			int spacing = Table.Spacing;
			ObservableList<Column> cols = Table.Columns;
						
			Widget first = Children[0];
			TableRow firstRow = Table.Children[0] as TableRow;

			if (firstRow == this) {
				base.ComputeChildrenPositions();
				return;
			}			
			childrenRWLock.EnterReadLock();			
			
			for (int i = 0; i < Children.Count && i < firstRow.Children.Count; i++)
			{
				Widget w = Children[i];

					w.Slot.X = firstRow.Children[i].Slot.X;
					setChildWidth (w, firstRow.Children[i].Slot.Width);				
				
			}

			childrenRWLock.ExitReadLock();
			IsDirty = true;
		}*/





		/*public override void ComputeChildrenPositions () {
			if (Children.Count == 0)
				return;
			int spacing = Table.Spacing;
			ObservableList<Column> cols = Table.Columns;
						
			Widget first = Children[0];
			TableRow firstRow = Table.Children[0] as TableRow;

			if (firstRow == this) {
				base.ComputeChildrenPositions();
				return;
			}			
			childrenRWLock.EnterReadLock();			
			
			for (int i = 0; i < Children.Count && i < firstRow.Children.Count; i++)
			{
				Widget w = Children[i];

					w.Slot.X = firstRow.Children[i].Slot.X;
					setChildWidth (w, firstRow.Children[i].Slot.Width);				
				
			}

			childrenRWLock.ExitReadLock();
			IsDirty = true;
		}*/





		/*public override void ComputeChildrenPositions () {
			if (Children.Count == 0)
				return;
			int spacing = Table.Spacing;
			ObservableList<Column> cols = Table.Columns;
						
			Widget first = Children[0];
			TableRow firstRow = Table.Children[0] as TableRow;

			if (firstRow == this) {
				base.ComputeChildrenPositions();
				return;
			}			
			childrenRWLock.EnterReadLock();			
			
			for (int i = 0; i < Children.Count && i < firstRow.Children.Count; i++)
			{
				Widget w = Children[i];

					w.Slot.X = firstRow.Children[i].Slot.X;
					setChildWidth (w, firstRow.Children[i].Slot.Width);				
				
			}

			childrenRWLock.ExitReadLock();
			IsDirty = true;
		}*/





		/*public override void ComputeChildrenPositions () {
			if (Children.Count == 0)
				return;
			int spacing = Table.Spacing;
			ObservableList<Column> cols = Table.Columns;
						
			Widget first = Children[0];
			TableRow firstRow = Table.Children[0] as TableRow;

			if (firstRow == this) {
				base.ComputeChildrenPositions();
				return;
			}			
			childrenRWLock.EnterReadLock();			
			
			for (int i = 0; i < Children.Count && i < firstRow.Children.Count; i++)
			{
				Widget w = Children[i];

					w.Slot.X = firstRow.Children[i].Slot.X;
					setChildWidth (w, firstRow.Children[i].Slot.Width);				
				
			}

			childrenRWLock.ExitReadLock();
			IsDirty = true;
		}*/
		
		/*public override bool UpdateLayout(LayoutingType layoutType)
		{
			RegisteredLayoutings &= (~layoutType);

			if (Table == null)
				return false;
			TableRow firstRow = Table.Children[0] as TableRow;
			if (layoutType == LayoutingType.Width) {
				if (firstRow.RegisteredLayoutings.HasFlag (LayoutingType.Width))
					return false;
				if (this != firstRow) {					
					Slot.Width = firstRow.Slot.Width;
					if (Slot.Width != LastSlots.Width) {
						IsDirty = true;
						OnLayoutChanges (layoutType);
						LastSlots.Width = Slot.Width;
					}
					if (RegisteredLayoutings == LayoutingType.None && IsDirty)
						IFace.EnqueueForRepaint (this);

					return true;
				}
			}
			
			return base.UpdateLayout(layoutType);
		}*/
		/*public override int measureRawSize (LayoutingType lt) {
			if (lt == LayoutingType.Width) {
				if (Table == null)
					return -1;
				TableRow firstRow = Table.Children[0] as TableRow;				
				if (this != firstRow) {
					if (firstRow.RegisteredLayoutings.HasFlag (LayoutingType.Width))
						return -1;
					contentSize = firstRow.contentSize;
					return firstRow.measureRawSize (lt);
				}

			}
			return base.measureRawSize (lt);
		}*/
		/*public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg) {
			if (arg.LayoutType == LayoutingType.Width) {
				Widget w = sender as Widget;
				TableRow firstRow = Table.Children[0] as TableRow;
				int c = Children.IndexOf(w);				
				if (c < Table.Columns.Count) {
					Column col = Table.Columns[c];
					if (col.Width.IsFit) {
						if (col.LargestWidget == null || w.Slot.Width > col.ComputedWidth) {
							col.ComputedWidth = w.Slot.Width;
							col.LargestWidget = w;
						} else if (w == col.LargestWidget)
							Console.WriteLine ("must search for largest widget");
					}else if (w == firstRow)
						col.ComputedWidth = w.Slot.Width;

				}
				Console.WriteLine ($"ROW:{Table.Children.IndexOf(this)} COL:{c} {w.LastSlots.Width} -> {w.Slot.Width} ");
			}
			base.OnChildLayoutChanges (sender, arg);
		}*/
		/*public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg) {
			Widget go = sender as Widget;
			TableRow row = go.Parent as TableRow;
			TableRow firstRow = Table.Children[0] as TableRow;

			if (arg.LayoutType == LayoutingType.Width) {
				if (row == firstRow) {
					base.OnChildLayoutChanges (sender, arg);
					int idx = Children.IndexOf (go);					
					foreach (TableRow r in Table.Children.Skip(1)) {
						if (idx < r.Children.Count)
							r.setChildWidth (r.Children[idx], go.Slot.Width);
						r.contentSize = firstRow.contentSize;
					}					
				} //else
					this.RegisterForLayouting (LayoutingType.ArrangeChildren);
				return;
			}

			base.OnChildLayoutChanges (sender, arg);
		}*/
		int splitIndex = -1;		
		const int minColumnSize = 10;
		int Spacing => Table.Spacing;
		public override void onMouseMove(object sender, MouseMoveEventArgs e)
		{			
			if (Spacing > 0 && Table != null && Table.Children.Count > 0) {
				Point m = ScreenPointToLocal (e.Position);
				if (IFace.IsDown (Glfw.MouseButton.Left) && splitIndex >= 0) {
					TableRow firstRow = Table.Children[0] as TableRow;
					Rectangle cb = ClientRectangle;
					int splitPos = (int)(0.5 * Spacing + m.X);					
					if (splitPos > firstRow.Children[splitIndex].Slot.Left + minColumnSize && splitPos < firstRow.Children[splitIndex+1].Slot.Right - minColumnSize) {
						Table.Columns[splitIndex+1].Width = firstRow.Children[splitIndex+1].Slot.Right - splitPos;
						splitPos -= Spacing;
						Table.Columns[splitIndex].Width = splitPos - firstRow.Children[splitIndex].Slot.Left;
						Table.RegisterForLayouting (LayoutingType.Width);
						e.Handled = true;
					}
					//Console.WriteLine ($"left:{firstRow.Children[splitIndex].Slot.Left} right:{firstRow.Children[splitIndex+1].Slot.Right} cb.X:{cb.X} splitPos:{splitPos} m:{m}");				
				} else {
					splitIndex = -1;					
					for (int i = 0; i < Children.Count - 1; i++)
					{
						Rectangle r = Children[i].Slot;
						if (m.X >= r.Right) {
							r = Children[i+1].Slot;
							if (m.X <= r.Left && Table.Columns.Count - 1 > i ) {
								IFace.MouseCursor = MouseCursor.sb_h_double_arrow;
								splitIndex = i;
								e.Handled = true;
								break;
							}
						}
					}
					if (splitIndex < 0 && IFace.MouseCursor == MouseCursor.sb_h_double_arrow)
						IFace.MouseCursor = MouseCursor.top_left_arrow;
				}
			}
			base.onMouseMove(sender, e);
		}
	}
}

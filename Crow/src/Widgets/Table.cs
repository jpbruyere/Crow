// Copyright (c) 2019-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;
using System.Linq;
using Crow.Cairo;

namespace Crow
{
	/// <summary>
	/// Table column definition
	/// </summary>
	public class Column : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
			=> ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		#endregion

		string caption;
		Measure width = Measure.Fit;
		public int ComputedWidth;
		public Widget LargestChild;

		public string Caption {
			get => caption;
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChanged ("Caption", caption);
			}
		}
		/// <summary>
		/// column width, special value 'Inherit' will be used to share table width equaly among columns
		/// </summary>
		/// <value>The column's width.</value>
		public Measure Width {
			get => width;
			set {
				if (width == value)
					return;
				width = value;
				NotifyValueChanged ("Width", width);
			}
		}

		public static Column Parse (string str) {
			if (string.IsNullOrEmpty (str))
				return null;				
			Column c = new Column();
			string[] tmp = str.Split (',');
			c.Caption = tmp[0];
			if (tmp.Length > 1)
				c.Width = Measure.Parse (tmp[1]);
			return c;
		}
	}


	public class Table : VerticalStack
	{
		#region CTOR
		public Table ()  {}
		public Table (Interface iface, string style = null) : base (iface, style) { }
		#endregion
		int columnSpacing, borderLineWidth, verticalLineWidth, horizontalLineWidth, rowsMargin;
		ObservableList<Column> columns;
		HorizontalStack HeaderRow;
		string headerCellTemplate;
		IML.Instantiator headerCellITor;

		[DefaultValue ("#Crow.DefaultTableHeaderCell.template")]
		public string HeaderCellTemplate {
			get => headerCellTemplate;
			set {
				if (headerCellTemplate == value)
					return;
				headerCellTemplate = value;
				NotifyValueChangedAuto (headerCellTemplate);

				headerCellITor = new IML.Instantiator (IFace, HeaderCellTemplate);
				createHeaderRow();
			}
		}
		public override void InsertChild (int idx, Widget g) {
			g.Width = Measure.Stretched;
			g.Margin = RowsMargin;
			base.InsertChild (idx, g);
			if (HeaderRow == null || idx == 0)
				return;
			TableRow row = g as TableRow;
			for (int i = 0; i < HeaderRow.Children.Count; i++) {
				if (row.Children.Count <= i)
					continue;
				setRowCellWidth (row.Children[i], HeaderRow.Children[i].Slot.Width);				
				row.Children[i].Slot.X = HeaderRow.Children[i].Slot.X;
			}
		}
		[DefaultValue (1)]
		public int ColumnSpacing {
			get => columnSpacing;
			set {
				if (columnSpacing == value)
					return;
				columnSpacing = value;
				NotifyValueChangedAuto (columnSpacing);
				if (HeaderRow != null)
					HeaderRow.Spacing = ColumnSpacing;
				//RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue (1)]
		public int BorderLineWidth {
			get => borderLineWidth;
			set {
				if (borderLineWidth == value)
					return;
				borderLineWidth = value;
				NotifyValueChangedAuto (borderLineWidth);
				RegisterForRedraw ();
			}
		}
		[DefaultValue (1)]
		public int HorizontalLineWidth {
			get => horizontalLineWidth;
			set {
				if (horizontalLineWidth == value)
					return;
				horizontalLineWidth = value;
				NotifyValueChangedAuto (horizontalLineWidth);
				RegisterForRedraw ();
			}
		}
		[DefaultValue (1)]
		public int VerticalLineWidth {
			get => verticalLineWidth;
			set {
				if (verticalLineWidth == value)
					return;
				verticalLineWidth = value;
				NotifyValueChangedAuto (verticalLineWidth);
				RegisterForRedraw ();
			}
		}
		[DefaultValue (1)]
		public int RowsMargin {
			get => rowsMargin;
			set {
				if (rowsMargin == value)
					return;
				rowsMargin = value;
				NotifyValueChangedAuto (rowsMargin);
				childrenRWLock.EnterReadLock ();
				foreach (Widget row in Children)
					row.Margin = rowsMargin;
				childrenRWLock.ExitReadLock ();
			}
		}		
		//int lineWidth;		
		public ObservableList<Column> Columns {
			get => columns;
			set {
				if (columns == value)
					return;
				if (columns != null) {
					deleteHeaderRow ();
					columns.ListAdd -= Ol_AddColumn;
					columns.ListAdd -= Ol_RemoveColumn;
				}

				columns = value;

				if (columns != null) {
					createHeaderRow ();					
					columns.ListAdd += Ol_AddColumn;
					columns.ListAdd += Ol_RemoveColumn;
				}
				NotifyValueChangedAuto(columns);
			}
		}
		void deleteHeaderRow () {
			if (HeaderRow == null)
				return;
			DeleteChild (HeaderRow);
			HeaderRow = null;
		}
		void createHeaderRow () {
			deleteHeaderRow ();
			if (Columns == null || headerCellITor == null)
				return;
			HeaderRow = new HorizontalStack(IFace, "TableHeaderRow") {Spacing = ColumnSpacing};
			InsertChild (0, HeaderRow);			
			foreach (Column c in Columns) {
				Widget cell = headerCellITor.CreateInstance();
				cell.LayoutChanged += onHeaderCell_LayoutChanges;
				HeaderRow.AddChild (cell);
				cell.DataSource = c;
			}
		}
		
		void Ol_AddColumn (object sender, ListChangedEventArg e) {
			HeaderRow.InsertChild (e.Index, headerCellITor.CreateInstance());
			HeaderRow.DataSource = e.Element;
		}
		void Ol_RemoveColumn (object sender, ListChangedEventArg e) {
			Widget w = HeaderRow.Children[e.Index];
			HeaderRow.RemoveChild (e.Index);
			w.Dispose ();
		}
		void onHeaderCell_LayoutChanges (object sender, LayoutingEventArgs e) {
			if (Columns == null)
				return;
			if (e.LayoutType == LayoutingType.Width) {
				Widget g = sender as Widget;
				int cIdx = HeaderRow.Children.IndexOf (g);
				if (cIdx < Columns.Count &&  Columns[cIdx].Width.IsFit)
					searchLargestChildInColumn (cIdx);				
				childrenRWLock.EnterReadLock ();
				for (int i = 1; i < Children.Count; i++) {
					TableRow row = Children[i] as TableRow;
					if (row.Children.Count <= cIdx)
						continue;
					setRowCellWidth (row.Children[cIdx], g.Slot.Width);
				}
				childrenRWLock.ExitReadLock ();

				RegisterForRedraw ();
			} else if (e.LayoutType == LayoutingType.X) {
				Widget g = sender as Widget;
				int cIdx = HeaderRow.Children.IndexOf (g);
				childrenRWLock.EnterReadLock ();
				for (int i = 1; i < Children.Count; i++) {
					TableRow row = Children[i] as TableRow;
					if (row.Children.Count <= cIdx)
						continue;
					row.Children[cIdx].Slot.X = g.Slot.X;
				}
				childrenRWLock.ExitReadLock ();
				RegisterForRedraw ();
			}
		}
		protected void setRowCellWidth (Widget w, int newW) {
			if (newW == w.Slot.Width)
				return;			
			w.Slot.Width = newW;
			w.IsDirty = true;
			w.OnLayoutChanges (LayoutingType.Width);
			w.LastSlots.Width = w.Slot.Width;
			w.RegisterForRedraw ();
		}		

		public override void ClearChildren()
		{
			base.ClearChildren();
			createHeaderRow ();
		}

		void searchLargestChildInColumn (int cIdx)
		{
			DbgLogger.StartEvent (DbgEvtType.GOSearchLargestChild, this);

			Column c = Columns[cIdx];

			childrenRWLock.EnterReadLock ();
			try {
				c.LargestChild = null;
				int largestWidth = 0;	
				for (int i = 1; i < Children.Count; i++) {
					TableRow row = Children[i] as TableRow;
					if (!row.IsVisible)
						continue;
					int cw = row.Children [cIdx]. measureRawSize (LayoutingType.Width);
					if (cw > largestWidth) {
						largestWidth = cw;
						c.LargestChild = row.Children [cIdx];
					}
				}
				if (HeaderRow.Children[cIdx].Slot.Width > largestWidth) {
					c.LargestChild = HeaderRow.Children[cIdx];
					return;
				}
				HeaderRow.Children[cIdx].Slot.Width = largestWidth;
			} finally {
				childrenRWLock.ExitReadLock ();
				DbgLogger.EndEvent (DbgEvtType.GOSearchLargestChild);
			}
			//HeaderRow.adjustStretchedGo (LayoutingType.Width);

		}
		int splitIndex = -1;		
		const int minColumnSize = 10;		
		public override void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			
			if (ColumnSpacing > 0 && Columns.Count > 0) {
				Point m = ScreenPointToLocal (e.Position);
				if (IFace.IsDown (Glfw.MouseButton.Left) && splitIndex >= 0) {					
					int splitPos = (int)(0.5 * ColumnSpacing + m.X);					
					if (splitPos > HeaderRow.Children[splitIndex].Slot.Left + minColumnSize && splitPos < HeaderRow.Children[splitIndex+1].Slot.Right - minColumnSize) {
						Columns[splitIndex+1].Width = HeaderRow.Children[splitIndex+1].Slot.Right - splitPos;
						splitPos -= ColumnSpacing;
						Columns[splitIndex].Width =  splitPos - HeaderRow.Children[splitIndex].Slot.Left;
						HeaderRow.RegisterForLayouting (LayoutingType.ArrangeChildren);
						e.Handled = true;
					}
					//Console.WriteLine ($"left:{HeaderRow.Children[splitIndex].Slot.Left} right:{HeaderRow.Children[splitIndex+1].Slot.Right} splitPos:{splitPos} m:{m}");				
				} else {
					splitIndex = -1;					
					for (int i = 0; i < Columns.Count - 1; i++)
					{
						Rectangle r = HeaderRow.Children[i].Slot;
						if (m.X >= r.Right) {
							r = HeaderRow.Children[i+1].Slot;
							if (m.X <= r.Left && Columns.Count - 1 > i ) {
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


		protected override void onDraw (Context gr) {
			DbgLogger.StartEvent (DbgEvtType.GODraw, this);

			base.onDraw (gr);

			if (Columns != null && columns.Count > 0 && HeaderRow != null) {

				Rectangle cb = ClientRectangle;
							
				Foreground.SetAsSource (IFace, gr, cb);
				if (BorderLineWidth > 0) {
					gr.LineWidth = BorderLineWidth;
					CairoHelpers.CairoRectangle (gr, cb, CornerRadius, borderLineWidth);
					gr.Stroke ();
				}
				double x = 0;
				if (VerticalLineWidth > 0) {
					gr.LineWidth = VerticalLineWidth;
					x = cb.Left + HeaderRow.Margin + 0.5 * ColumnSpacing + HeaderRow.Children[0].Slot.Width;// - 0.5 * VerticalLineWidth;				
					for (int i = 1; i < HeaderRow.Children.Count ; i++)
					{
						gr.MoveTo (x, cb.Y);
						gr.LineTo (x, cb.Bottom);
						x += columnSpacing + HeaderRow.Children[i].Slot.Width ;
					}
					gr.Stroke ();
				}

				if (HorizontalLineWidth > 0) {
					gr.LineWidth = HorizontalLineWidth;
					x = cb.Top + 0.5 * Spacing + Children[0].Slot.Height;// - 0.5 * HorizontalLineWidth;
					for (int i = 1; i < Children.Count; i++)
					{
						gr.MoveTo (cb.Left, x);
						gr.LineTo (cb.Right, x);
						x += Spacing + Children[i].Slot.Height ;
					}
					gr.Stroke ();
				}
				
			}

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}			

	}
}

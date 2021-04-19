// Copyright (c) 2019-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;
using System.Linq;
#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif

namespace Crow
{
	/// <summary>
	/// Table column definition
	/// </summary>
	public class Column2 : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
			=> ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		#endregion

		string caption;
		Measure width = Measure.Fit;
		public int ComputedWidth;
		public Widget LargestWidget;

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


	public class Table2 : VerticalStack
	{
		#region CTOR
		public Table2 ()  {}
		public Table2 (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		//int lineWidth;
		ObservableList<Column> columns = new ObservableList<Column>();

		public ObservableList<Column> Columns {
			get => columns;
			set {
				if (columns == value)
					return;
				if (columns != null) {
					columns.ListAdd -= Ol_AddColumn;
					columns.ListAdd -= Ol_RemoveColumn;

					foreach (Column c in columns) 
						c.ValueChanged += column_valueChanged;					
				}

				columns = value;

				if (columns != null) {
					columns.ListAdd += Ol_AddColumn;
					columns.ListAdd += Ol_RemoveColumn;

					foreach (Column c in columns) 
						c.ValueChanged += column_valueChanged;					

					foreach (TableRow row in Children) {
						for (int i = 0; i < columns.Count && i < row.Children.Count; i++)
							row.Children[i].Width = columns[i].Width;
					}
				}
				NotifyValueChangedAuto(columns);
			}
		}

		void column_valueChanged (object sender, ValueChangeEventArgs e) {
			switch (e.MemberName) {
			case "Width":
				int columnIdx = columns.IndexOf (sender as Column);
				foreach (TableRow row in Children.OfType<TableRow>().Where (r => r.Children.Count > columnIdx)) 
					row.Children[columnIdx].Width = (Measure)e.NewValue;
				break;							
			}
		}

		public override void AddChild (Widget child) {
			if (!(child is TableRow tr))			
				throw new Exception ("Table widget accept only TableRow as child.");
			base.AddChild (child);

			tr.Children.ListAdd += Ol_tableRow_ChildAdd;
			tr.Children.ListRemove += Ol_tableRow_ChildRemove;
			tr.Children.ListClear += Ol_tableRow_ChildClear;

			for (int i = 0; i < columns.Count && i < tr.Children.Count; i++)
				tr.Children[i].Width = columns[i].Width;
		}
		public override void RemoveChild(Widget child)
		{
			base.RemoveChild(child);

			TableRow tr = child as TableRow;
			tr.Children.ListAdd -= Ol_tableRow_ChildAdd;
			tr.Children.ListRemove -= Ol_tableRow_ChildRemove;
			tr.Children.ListClear -= Ol_tableRow_ChildClear;
		}
		void Ol_tableRow_ChildAdd (object sender, ListChangedEventArg e)
		{
			Widget w = e.Element as Widget;
			if (e.Index < Columns.Count)
				w.Width = Columns[e.Index].Width;
			w.LayoutChanged += onTableRow_ChildLayoutChanges;
		}
		void Ol_tableRow_ChildRemove (object sender, ListChangedEventArg e) {
			Widget w = e.Element as Widget;
			w.LayoutChanged -= onTableRow_ChildLayoutChanges;
		}
		void Ol_tableRow_ChildClear (object sender, ListClearEventArg e) {
			foreach (Widget w in e.Elements)				
				w.LayoutChanged -= onTableRow_ChildLayoutChanges;
		}
		void onTableRow_ChildLayoutChanges (object sender, LayoutingEventArgs arg) {
			if (Columns == null)
				return;
			Widget g = sender as Widget;
			TableRow tr = g.Parent as TableRow;
			int cIdx = tr.Children.IndexOf (g);
			//if (cIdx < Columns.Count && Columns.wi)
		}
		void Ol_AddColumn (object sender, ListChangedEventArg e) {
			(e.Element as Column).ValueChanged += column_valueChanged;
			foreach (TableRow row in Children) {
				for (int i = e.Index; i < columns.Count && i < row.Children.Count; i++)
					row.Children[i].Width = columns[i].Width;												
			}
			
		}
		void Ol_RemoveColumn (object sender, ListChangedEventArg e) {
			(e.Element as Column).ValueChanged -= column_valueChanged;
			foreach (TableRow row in Children) {
				for (int i = e.Index; i < columns.Count && i < row.Children.Count; i++)
					row.Children[i].Width = columns[i].Width;												
			}			
		}


		public override void ChildrenLayoutingConstraints(ILayoutable layoutable, ref LayoutingType layoutType)
		{
			//trigger layouting for width only in the first row, the other will be set at the same horizontal position and width.
			if (layoutable == Children[0])
				layoutType &= (~LayoutingType.X);	
			else
				layoutType &= (~(LayoutingType.X|LayoutingType.Width));			
		}

		//overriden to prevent search for largest child, all the rows have the same total width.
		public override void ComputeChildrenPositions () {
			int d = 0;
			childrenRWLock.EnterReadLock();
			foreach (TableRow c in Children) {
				if (!c.IsVisible)
					continue;
				c.Slot.Y = d;
				d += c.Slot.Height + Spacing;
			}
			childrenRWLock.ExitReadLock();			
			IsDirty = true;
		}
/*		public override bool UpdateLayout(LayoutingType layoutType)
		{
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.Width) {
				//propagate column.width to each row's children
			}
			return base.UpdateLayout(layoutType);
		}*/
	
		/*public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg) {			
			TableRow row = sender as TableRow;
			TableRow firstRow = Children[0] as TableRow;

			if (arg.LayoutType == LayoutingType.Width) {
				if (row == firstRow) {
					base.OnChildLayoutChanges (sender, arg);										
					foreach (TableRow r in Children.Skip(1)) {						
						r.contentSize = firstRow.contentSize;
						setChildWidth (r, firstRow.Slot.Width);
					}					
				}
			}

			base.OnChildLayoutChanges (sender, arg);
		}*/
		/*protected override void onDraw (Context gr) {
			DbgLogger.StartEvent (DbgEvtType.GODraw, this);

			base.onDraw (gr);

			if (Children.Count > 0) {

				Rectangle cb = ClientRectangle;
				TableRow fr = Children[0] as TableRow;

				
				gr.LineWidth = lineWidth;
				Foreground.SetAsSource (IFace, gr, cb);				
				CairoHelpers.CairoRectangle (gr, cb, CornerRadius, lineWidth);
				double x = 0.5 + cb.Left + fr.Margin + 0.5 * fr.Spacing + fr.Children[0].Slot.Width;				
				for (int i = 1; i < fr.Children.Count ; i++)
				{
					gr.MoveTo (x, cb.Y);
					gr.LineTo (x, cb.Bottom);
					x += fr.Spacing + fr.Children[i].Slot.Width ;
				}

				//horizontal lines
				x = 0.5 + cb.Top + 0.5 * Spacing + Children[0].Slot.Height;
				for (int i = 0; i < Children.Count - 1; i++)
				{
					gr.MoveTo (cb.Left, x);
					gr.LineTo (cb.Right, x);
					x += Spacing + Children[i].Slot.Height ;
				}
				gr.Stroke ();
			}

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}*/		
	}	
}

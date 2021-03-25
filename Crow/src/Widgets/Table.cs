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
		//public int ComputedWidth;
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


	public class Table : VerticalStack
	{
		#region CTOR
		public Table ()  {}
		public Table (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		//int lineWidth;
		ObservableList<Column> columns = new ObservableList<Column>();

		/*[DefaultValue (1)]
		public int LineWidth {
			get => lineWidth;
			set {
				if (lineWidth == value)
					return;
				lineWidth = value;
				NotifyValueChangedAuto (lineWidth);
				RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			}
		}*/

		public ObservableList<Column> Columns {
			get => columns;
			set {
				if (columns == value)
					return;
				columns = value;
				NotifyValueChangedAuto(columns);
			}
		}
		public override void AddChild (Widget child) {
			TableRow tr = child as TableRow;
			if (tr == null)
				throw new Exception ("Table widget accept only TableRow as child.");
			base.AddChild (child);
		}
		/*public override void ChildrenLayoutingConstraints(ILayoutable layoutable, ref LayoutingType layoutType)
		{
			//trigger layouting for width only in the first row, the other will be set at the same horizontal position and width.
			if (layoutable == Children[0])
				layoutType &= (~LayoutingType.X);	
			else
				layoutType &= (~(LayoutingType.X|LayoutingType.Width));			
		}*/

		public override void ComputeChildrenPositions () {
			int d = 0;
			childrenRWLock.EnterReadLock();
			foreach (Widget c in Children) {
				if (!c.Visible)
					continue;
				c.Slot.Y = d;
				d += c.Slot.Height + Spacing;
			}
			childrenRWLock.ExitReadLock();			
			IsDirty = true;
		}
		public override bool UpdateLayout(LayoutingType layoutType)
		{
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.Width) {
				foreach (TableRow row in Children) {
					for (int i = 0; i < Columns.Count && i < row.Children.Count; i++) 
						row.Children[i].Width = Columns[i].Width;
				}				
			}
			return base.UpdateLayout(layoutType);
		}
	
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

// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using Crow.Cairo;
using System.Linq;

namespace Crow
{
	public class TabItem : TemplatedContainer
	{
		#region CTOR
		protected TabItem() {}
		public TabItem (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		public event EventHandler QueryClose;

		internal TabView tview = null;

		#region Private fields
		Widget titleWidget;
		int tabOffset;
		bool isSelected;
		//Measure tabThickness;
		Fill selectedBackground = Colors.Transparent;
		#endregion

		#region TemplatedControl overrides
		public override Widget Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				if (Content != null) {
					Content.LogicalParent = null;
					_contentContainer.SetChild (null);
				}
				_contentContainer.SetChild(value);
				if (value != null)
					value.LogicalParent = this;
			}
		}
		protected override void loadTemplate(Widget template = null)
		{
			base.loadTemplate (template);

			titleWidget = this.child.FindByName ("TabTitle");
		}
		internal Widget TabTitle { get { return titleWidget; }}
		#endregion

		/// <summary>
		/// order of redrawing, items can't be reordered in TemplatedGroup due to data linked, so we need another index
		/// instead of children list order
		/// </summary>
		public int viewIndex = 0;
		public virtual int ViewIndex {
			get { return viewIndex; }
			set {
				if (viewIndex == value)
					return;
				viewIndex = value;
				NotifyValueChangedAuto (viewIndex);
			}
		}
			
		[DefaultValue(0)]
		public int TabOffset {
			get { return tabOffset; }
			set {
				if (tabOffset == value)
					return;
				tabOffset = value;
				NotifyValueChangedAuto (tabOffset);

				RegisterForLayouting (LayoutingType.X);
				RegisterForGraphicUpdate ();
			}
		}
		public Measure TabHeight {
			get { return tview == null ? Measure.Fit : tview.TabHeight; }
		}
		public Measure TabWidth {
			get { return tview == null ? Measure.Fit : tview.TabWidth; }
		}
		[DefaultValue(false)]
		public virtual bool IsSelected {
			get { return isSelected; }
			set {
				if (isSelected == value)
					return;

				if (tview != null)
					tview.SelectedTab = tview.Children.IndexOf(this);
				
				isSelected = value;
				NotifyValueChangedAuto (isSelected);
				RegisterForRedraw ();
			}
		}

		/// <summary>
		/// background fill of the control, maybe solid color, gradient, image, or svg
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue("DimGrey")]
		public virtual Fill SelectedBackground {
			get { return selectedBackground; }
			set {
				if (selectedBackground == value)
					return;				
				if (value == null)
					return;
				selectedBackground = value;
				NotifyValueChangedAuto (selectedBackground);
				RegisterForRedraw ();
			}
		}
		protected override void onDraw (Context gr)
		{
			gr.Save ();

			parentRWLock.EnterReadLock ();

			TabView tv = Parent as TabView;

			//TODO:this appens in designView
			if (tv == null) {
				parentRWLock.ExitReadLock ();
				return;
			}

			Rectangle r = TabTitle.Slot;
			r.Width = TabWidth;

			gr.MoveTo (0.5, r.Bottom-0.5);
			gr.LineTo (r.Left - tv.LeftSlope, r.Bottom-0.5);
			gr.CurveTo (
				r.Left - tv.LeftSlope / 2, r.Bottom-0.5,
				r.Left - tv.LeftSlope / 2, 0.5,
				r.Left, 0.5);
			gr.LineTo (r.Right, 0.5);
			gr.CurveTo (
				r.Right + tv.RightSlope / 2, 0.5,
				r.Right + tv.RightSlope / 2, r.Bottom-0.5,
				r.Right + tv.RightSlope, r.Bottom-0.5);
			gr.LineTo (Slot.Width-0.5, r.Bottom-0.5);

			parentRWLock.ExitReadLock ();

			gr.LineTo (Slot.Width-0.5, Slot.Height-0.5);
			gr.LineTo (0.5, Slot.Height-0.5);
			gr.ClosePath ();
			gr.LineWidth = 1;
			Foreground.SetAsSource (gr);
			gr.StrokePreserve ();
			gr.ClipPreserve ();

			if (IsSelected)
				SelectedBackground.SetAsSource (gr, ClientRectangle);
			else
				Background.SetAsSource (gr, ClientRectangle);

			gr.Fill ();

			base.onDraw (gr);

			gr.Restore ();
		}

		Point dragStartPoint;
		int dragThreshold = 16;
		int dis = 128;
		internal TabView savedParent = null;


		void makeFloating (TabView tv) {			
			lock (IFace.UpdateMutex) {				
				ImageSurface di = new ImageSurface (Format.Argb32, dis, dis);
				IFace.DragImageHeight = dis;
				IFace.DragImageWidth = dis;
				using (Context ctx = new Context (di)) {
					double div = Math.Max (LastPaintedSlot.Width, LastPaintedSlot.Height);
					double s = (double)dis / div;
					ctx.Scale (s, s);
					if (bmp == null)
						this.onDraw (ctx);
					else {
						if (LastPaintedSlot.Width>LastPaintedSlot.Height)
							ctx.SetSourceSurface (bmp, 0, (LastPaintedSlot.Width-LastPaintedSlot.Height)/2);
						else
							ctx.SetSourceSurface (bmp, (LastPaintedSlot.Height-LastPaintedSlot.Width)/2, 0);

						ctx.Paint ();
					}
				}
				IFace.DragImage = di;
			}
			tv.RemoveChild (this);
			savedParent = tv;
		}

		public override ILayoutable Parent {
			get {
				return base.Parent;
			}
			set {
				base.Parent = value;
				if (value != null) {
					dragStartPoint = IFace.MousePosition;
					savedParent = value as TabView;
				}
			}
		}
		protected override void onStartDrag (object sender, DragDropEventArgs e)
		{
			base.onStartDrag (sender, e);

			dragStartPoint = IFace.MousePosition;
		}
		protected override void onEndDrag (object sender, DragDropEventArgs e)
		{
			base.onEndDrag (sender, e);

			if (Parent != null)
				return;

			savedParent.AddChild (this);

			IFace.ClearDragImage ();
		}
		protected override void onDrop (object sender, DragDropEventArgs e)
		{
			base.onDrop (sender, e);
			if (Parent != null)
				return;
			TabView tv = e.DropTarget as TabView;
			if (tv == null)
				return;

			IFace.ClearDragImage ();

			tv.AddChild (this);
		}
		#region Mouse Handling
		public override bool PointIsIn (ref Point m)
		{
			if (!base.PointIsIn (ref m))
				return false;
			if (tview == null)//double check this, just added to prevent exception
				return false;
			if (m.Y < tview.TabHeight)
				return TabTitle.Slot.ContainsOrIsEqual (m);
			else
				return this.isSelected;
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			tview?.UpdateLayout (LayoutingType.ArrangeChildren);
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (Parent == null)
				return;
			
			if (!IsDragged)
				return;

			TabView tv = Parent as TabView;
			if (Math.Abs (e.Position.Y - dragStartPoint.Y) > dragThreshold ||
				Math.Abs (e.Position.X - dragStartPoint.X) > dragThreshold) {
				makeFloating (tv);
				return;
			}

			Rectangle cb = ClientRectangle;

			int tmp = TabOffset + e.XDelta;
			if (tmp < tview.LeftSlope) {				
				TabOffset = tview.LeftSlope;
			} else if (tmp > cb.Width - tv.RightSlope - tv.TabWidth) {
				TabOffset = cb.Width - tv.RightSlope - tv.TabWidth;
			}else{
				dragStartPoint.X = e.Position.X;
				TabItem[] tabItms = tv.Children.Cast<TabItem>().OrderBy (t=>t.ViewIndex).ToArray();
				if (ViewIndex > 0 && e.XDelta < 0) {
					TabItem previous = tabItms [ViewIndex - 1];
					if (tmp < previous.TabOffset + tview.TabWidth / 2) {
						previous.ViewIndex = ViewIndex;
						ViewIndex--;
						tv.UpdateLayout (LayoutingType.ArrangeChildren);
					}

				}else if (ViewIndex < tabItms.Length - 1 && e.XDelta > 0) {
					TabItem next = tabItms [ViewIndex + 1];
					if (tmp > next.TabOffset - tview.TabWidth / 2){
						next.ViewIndex = ViewIndex;
						ViewIndex++;
						tv.UpdateLayout (LayoutingType.ArrangeChildren);
					}
				}
				TabOffset = tmp;
			}
		}
		public void butCloseTabClick (object sender, MouseButtonEventArgs e){			
			QueryClose.Raise (this, null);
			//if tab is used as a templated item root in a templatedGroup, local datasource
			//is not null, in this case, removing the data entries will delete automatically the item
			if (localDataSourceIsNull)
				(Parent as TabView)?.DeleteChild (this);
		}
		#endregion

	}
}


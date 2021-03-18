// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using Crow.Cairo;
using System.Linq;

namespace Crow
{
	public class TabView : Group
	{
		#region CTOR
		public TabView () { }
		public TabView (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region Private fields
		int adjustedTab = -1;
		int leftSlope;
		int rightSlope;
		Measure tabHeight, tabWidth;
		Orientation _orientation;
		TabItem activeTab;
		bool activateNewTab;
		#endregion

		#region public properties
		[DefaultValue (Orientation.Horizontal)]
		public virtual Orientation Orientation {
			get { return _orientation; }
			set {
				if (_orientation == value)
					return;
				_orientation = value;
				NotifyValueChangedAuto (_orientation);
				if (_orientation == Orientation.Horizontal)
					NotifyValueChanged ("TabOrientation", Orientation.Vertical);
				else
					NotifyValueChanged ("TabOrientation", Orientation.Horizontal);
				this.RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue (16)]
		public int LeftSlope {
			get { return leftSlope; }
			set {
				if (leftSlope == value)
					return;
				leftSlope = value;
				NotifyValueChangedAuto (leftSlope);
				//tabSizeHasChanged = true;
				//RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		//bool tabSizeHasChanged = false;
		[DefaultValue (16)]
		public int RightSlope {
			get { return rightSlope; }
			set {
				if (rightSlope == value)
					return;
				rightSlope = value;
				NotifyValueChangedAuto (rightSlope);
				//tabSizeHasChanged = true;
				//RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue ("18")]
		public Measure TabHeight {
			get { return tabHeight; }
			set {
				if (tabHeight == value)
					return;
				tabHeight = value;
				NotifyValueChangedAuto (tabHeight);
				//				childrenRWLock.EnterReadLock ();
				//				foreach (GraphicObject ti in Children) {
				//					ti.NotifyValueChanged ("TabHeight", tabHeight);
				//				}
				//				childrenRWLock.ExitReadLock ();
				RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue ("120")]
		public Measure TabWidth {
			get { return adjustedTab > 0 ? (Measure)adjustedTab : tabWidth; }
			set {
				if (tabWidth == value)
					return;
				tabWidth = value;
				NotifyValueChangedAuto (TabWidth);
				//
				//				childrenRWLock.EnterReadLock ();
				//				foreach (GraphicObject ti in Children) { 
				//					ti.NotifyValueChanged ("TabWidth", tabWidth);
				//				}
				//				childrenRWLock.ExitReadLock ();
				RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		/// <summary>
		/// If true new tabs will be set as the active tab of this tabview.
		/// </summary>
		[DefaultValue (true)]
		public bool ActivateNewTab {
			get => activateNewTab;
			set {
				if (activateNewTab == value)
					return;
				activateNewTab = value;
				NotifyValueChangedAuto (activateNewTab);
            }
        }
		public virtual TabItem ActiveTab {
			get => activeTab;
			set {				
				if (activeTab == value)
					return;

				//Console.WriteLine ($"TabView.ActiveTab: {activeTab?.DataSource} -> {value?.DataSource}");

				if (value != null) {
					if (activeTab != null) {
						activeTab.IsSelected = false;
						ActiveTab.NotifyValueChanged ("IsActiveTab", false);
					}
					activeTab = value;
					ActiveTab.IsSelected = true;
					ActiveTab.NotifyValueChanged ("IsActiveTab", true);
				} else
					activeTab = value;

				NotifyValueChangedAuto (activeTab);
				RegisterForRedraw ();
			}
		}
		#endregion

		public override void AddChild (Widget child) {
			TabItem ti = child as TabItem;
			if (ti == null)
				throw new Exception ("TabView control accept only TabItem as child.");

			ti.MouseDown += Ti_MouseDown;
			ti.TabTitle.LayoutChanged += Ti_TabTitle_LayoutChanged;
			ti.tview = this;

			base.AddChild (child);

			ti.ViewIndex = Children.Count - 1;

			if (ActivateNewTab || ti.ViewIndex == 0)
				ti.IsSelected = true;

			this.RegisterForLayouting (LayoutingType.ArrangeChildren);
		}
		public override void RemoveChild (Widget child) {
			TabItem ti = child as TabItem;
			if (ti == null)
				throw new Exception ("TabView control accept only TabItem as child.");

			ti.MouseDown -= Ti_MouseDown;
			ti.TabTitle.LayoutChanged -= Ti_TabTitle_LayoutChanged;
			ti.tview = null;

			childrenRWLock.EnterReadLock ();

			TabItem[] tabItms = Children.Cast<TabItem> ().OrderBy (t => t.ViewIndex).ToArray ();
			if (ActiveTab == ti) {				
				if (tabItms.Length > 1) {
					if (ti.ViewIndex == tabItms.Length - 1)
						ActiveTab = tabItms[ti.ViewIndex - 1];
					else
						ActiveTab = tabItms[ti.ViewIndex + 1];
				} else
					ActiveTab = null;
			}
			for (int i = ti.viewIndex + 1; i < tabItms.Length; i++)
				tabItms[i].ViewIndex--;

			/*int selTabViewIdx = -1;

			if (SelectedTab < tabItms.Length && SelectedTab >= 0)
				selTabViewIdx = (Children [SelectedTab] as TabItem).ViewIndex;

			for (int i = selTabViewIdx+1; i < tabItms.Length; i++)
				tabItms [i].ViewIndex--;

			if (selTabViewIdx > tabItms.Length - 2)
				selTabViewIdx = tabItms.Length - 2;

			if (selTabViewIdx < 0)
				SelectedTab = -1;
			else
				SelectedTab = Children.IndexOf (tabItms [selTabViewIdx]);*/

			childrenRWLock.ExitReadLock ();

			base.RemoveChild (child);
		}

		public override bool ArrangeChildren { get { return true; } }
		public override bool UpdateLayout (LayoutingType layoutType) {
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren && Children.Count > 0) {
				Rectangle cb = ClientRectangle;

				int tabSpace = tabWidth + leftSlope;
				int tc = Children.Count (c => c.Visible == true);

				if (tc > 0)
					tabSpace = Math.Min (tabSpace, (cb.Width - rightSlope) / tc);

				if (tabSpace < tabWidth + leftSlope)
					adjustedTab = tabSpace - leftSlope;
				else
					adjustedTab = -1;

				//System.Diagnostics.Debug.WriteLine ("tabspace: {0} tw:{1}", tabSpace, tabWidth);

				childrenRWLock.EnterReadLock ();
				TabItem[] tabItms = Children.Cast<TabItem> ().OrderBy (t => t.ViewIndex).ToArray ();
				childrenRWLock.ExitReadLock ();
				int curOffset = leftSlope;

				for (int i = 0; i < tabItms.Length; i++) {
					if (!tabItms[i].Visible)
						continue;
					tabItms[i].NotifyValueChanged ("TabHeight", tabHeight);
					tabItms[i].NotifyValueChanged ("TabWidth", TabWidth);
					if (!tabItms[i].IsDragged) {
						tabItms[i].TabOffset = curOffset;
						//System.Diagnostics.Debug.WriteLine ("offset: {0}=>{1}", tabItms [i].Name, tabItms [i].TabOffset);
					}
					if (Orientation == Orientation.Horizontal) {
						curOffset += tabSpace;
					} else
						curOffset += tabSpace;
				}

				//if no layouting remains in queue for item, registre for redraw
				if (RegisteredLayoutings == LayoutingType.None && IsDirty)
					IFace.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout (layoutType);
		}
		public override void OnLayoutChanges (LayoutingType layoutType) {
			if (_orientation == Orientation.Horizontal) {
				if (layoutType == LayoutingType.Width)
					RegisterForLayouting (LayoutingType.ArrangeChildren);
			} else if (layoutType == LayoutingType.Height)
				RegisterForLayouting (LayoutingType.ArrangeChildren);

			base.OnLayoutChanges (layoutType);
		}

		internal TabItem[] VisibleTabsByViewIdx =>
			Children.Where (tt => tt.Visible).Cast<TabItem> ().
				OrderBy (t => t.ViewIndex).ToArray ();

		protected override void onDraw (Context gr) {
			Rectangle rBack = new Rectangle (Slot.Size);

			Background.SetAsSource (IFace, gr, rBack);
			CairoHelpers.CairoRectangle (gr, rBack, CornerRadius);
			gr.Fill ();

			gr.Save ();

			if (ClipToClientRect) {
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			childrenRWLock.EnterReadLock ();

			TabItem[] tabItms = VisibleTabsByViewIdx;

			TabItem sti = ActiveTab;
			int selTabViewIdx = sti == null ? tabItms.Length - 1 : sti.ViewIndex;

			int i = 0;
			while (i < selTabViewIdx) {
				tabItms[i].Paint (gr);
				i++;
			}
			i = tabItms.Length - 1;
			while (i > selTabViewIdx) {
				tabItms[i].Paint (gr);
				i--;
			}

			if (selTabViewIdx >= 0 && selTabViewIdx < tabItms.Length)
				tabItms[selTabViewIdx].Paint (gr);

			childrenRWLock.ExitReadLock ();

			gr.Restore ();
		}

		protected override void onDragEnter (object sender, DragDropEventArgs e) {
			base.onDragEnter (sender, e);

			TabItem ti = e.DragSource as TabItem;
			if (ti == null)
				return;
			if (ti.Parent != null || ti.savedParent == this)
				return;

			this.AddChild (ti);

			Point p = ScreenPointToLocal (IFace.MousePosition) - Margin;

			p.X = Math.Max (leftSlope, p.X);
			p.X = Math.Min (ClientRectangle.Width - rightSlope - TabWidth, p.X);
			ti.TabOffset = p.X;

			IFace.ClearDragImage ();

		}

		void Ti_TabTitle_LayoutChanged (object sender, LayoutingEventArgs e) {
			if (e.LayoutType == LayoutingType.X)
				RegisterForLayouting (LayoutingType.ArrangeChildren);
		}
		void Ti_MouseDown (object sender, MouseButtonEventArgs e) {
			ActiveTab = sender as TabItem;
		}
	}
}


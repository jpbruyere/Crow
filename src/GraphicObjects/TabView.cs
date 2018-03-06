//
// TabView.cs
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
using System.Xml.Serialization;
using System.ComponentModel;
using Cairo;
using System.Diagnostics;
using System.Linq;

namespace Crow
{	
	public class TabView : Group
	{
		#region CTOR
		public TabView() : base(){}
		public TabView (Interface iface) : base(iface){}
		#endregion

		#region Private fields
		int spacing;
		int leftSlope;
		int rightSlope;
		Measure tabHeight, tabWidth;
		Orientation _orientation;
		int selectedTab;
		#endregion

		#region public properties
		[DefaultValue(Orientation.Horizontal)]
		public virtual Orientation Orientation
		{
			get { return _orientation; }
			set {
				if (_orientation == value)
					return;
				_orientation = value;
				NotifyValueChanged ("Orientation", _orientation);
				if (_orientation == Orientation.Horizontal)
					NotifyValueChanged ("TabOrientation", Orientation.Vertical);
				else
					NotifyValueChanged ("TabOrientation", Orientation.Horizontal);				
				this.RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue(16)]
		public int LeftSlope
		{
			get { return leftSlope; }
			set {
				if (leftSlope == value)
					return;
				leftSlope = value;
				NotifyValueChanged ("leftSlope", leftSlope);
				tabSizeHasChanged = true;
				//RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		bool tabSizeHasChanged = false;
		[DefaultValue(16)]
		public int RightSlope
		{
			get { return rightSlope; }
			set {
				if (rightSlope == value)
					return;
				rightSlope = value;
				NotifyValueChanged ("RightSlope", rightSlope);
				tabSizeHasChanged = true;
				//RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue("18")]
		public Measure TabHeight {
			get { return tabHeight; }
			set {
				if (tabHeight == value)
					return;
				tabHeight = value;
				NotifyValueChanged ("TabHeight", tabHeight);
//				childrenRWLock.EnterReadLock ();
//				foreach (GraphicObject ti in Children) {
//					ti.NotifyValueChanged ("TabHeight", tabHeight);
//				}
//				childrenRWLock.ExitReadLock ();
				RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}
		[DefaultValue("120")]
		public Measure TabWidth {
			get { return tabWidth; }
			set {
				if (tabWidth == value)
					return;
				tabWidth = value;
				NotifyValueChanged ("TabWidth", tabWidth);
//
//				childrenRWLock.EnterReadLock ();
//				foreach (GraphicObject ti in Children) { 
//					ti.NotifyValueChanged ("TabWidth", tabWidth);
//				}
//				childrenRWLock.ExitReadLock ();
				RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}

		public virtual int SelectedTab {
			get { return selectedTab; }
			set {
				if (value == selectedTab)
					return;

				if (selectedTab < Children.Count && selectedTab >= 0)
					(Children [selectedTab] as TabItem).IsSelected = false;

				selectedTab = value;

				if (selectedTab < Children.Count && selectedTab >= 0)
					(Children [selectedTab] as TabItem).IsSelected = true;

				NotifyValueChanged ("SelectedTab", selectedTab);
				RegisterForRedraw ();
			}
		}
		#endregion

		public override void AddChild (GraphicObject child)
		{
			TabItem ti = child as TabItem;
			if (ti == null)
				throw new Exception ("TabView control accept only TabItem as child.");

			ti.MouseDown += Ti_MouseDown;
			ti.TabTitle.LayoutChanged += Ti_TabTitle_LayoutChanged;
			ti.tview = this;

			base.AddChild (child);

			SelectedTab = ti.ViewIndex = Children.Count - 1;
			this.RegisterForLayouting (LayoutingType.ArrangeChildren);
		}
		public override void RemoveChild (GraphicObject child)
		{
			TabItem ti = child as TabItem;
			if (ti == null)
				throw new Exception ("TabView control accept only TabItem as child.");

			ti.MouseDown -= Ti_MouseDown;
			ti.TabTitle.LayoutChanged -= Ti_TabTitle_LayoutChanged;
			ti.tview = null;

			childrenRWLock.EnterReadLock ();

			TabItem[] tabItms = Children.Cast<TabItem>().OrderBy (t=>t.ViewIndex).ToArray();
			int selTabViewIdx = -1;

			if (SelectedTab < tabItms.Length && SelectedTab >= 0)
				selTabViewIdx = (Children [SelectedTab] as TabItem).ViewIndex;

			for (int i = selTabViewIdx+1; i < tabItms.Length; i++)
				tabItms [i].ViewIndex--;

			if (selTabViewIdx > tabItms.Length - 2)
				selTabViewIdx = tabItms.Length - 2;

			if (selTabViewIdx < 0)
				SelectedTab = -1;
			else
				SelectedTab = Children.IndexOf (tabItms [selTabViewIdx]);

			childrenRWLock.ExitReadLock ();

			base.RemoveChild (child);
		}

		public override bool ArrangeChildren { get { return true; } }
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren && Children.Count > 0) {
				Rectangle cb = ClientRectangle;

				int tabSpace = tabWidth + leftSlope;
				int computedSpacing = Math.Min(tabSpace, (cb.Width - rightSlope - leftSlope) / (Children.Count (c => c.Visible == true)));

				TabItem[] tabItms = Children.Cast<TabItem>().OrderBy (t=>t.ViewIndex).ToArray();
				int curOffset = leftSlope;

				for (int i = 0; i < tabItms.Length; i++) {
					if (!tabItms [i].Visible)
						continue;
//					if (tabSizeHasChanged) {
						tabItms [i].NotifyValueChanged ("TabHeight", tabHeight);
						tabItms [i].NotifyValueChanged ("TabWidth", tabWidth);
//						tabSizeHasChanged = false;
//					}
					if (!tabItms [i].HoldCursor)
						tabItms [i].TabOffset = curOffset;
					if (Orientation == Orientation.Horizontal) {
						curOffset += computedSpacing;
					} else
						curOffset += computedSpacing;					
				}

				//if no layouting remains in queue for item, registre for redraw
				if (RegisteredLayoutings == LayoutingType.None && IsDirty)
					IFace.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout(layoutType);
		}
//		public override void OnLayoutChanges (LayoutingType layoutType)
//		{
//			if (_orientation == Orientation.Horizontal) {
//				if (layoutType == LayoutingType.Width) {
//					computedSpacingOk = false;
//					RegisterForLayouting (LayoutingType.ArrangeChildren);
//				}
//			} else if (layoutType == LayoutingType.Height) {
//				computedSpacingOk = false;
//				RegisterForLayouting (LayoutingType.ArrangeChildren);
//			}
//			
//			base.OnLayoutChanges (layoutType);
//		}

		protected override void onDraw (Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);

			Background.SetAsSource (gr, rBack);
			CairoHelpers.CairoRectangle(gr,rBack, CornerRadius);
			gr.Fill ();

			gr.Save ();

			if (ClipToClientRect) {
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			childrenRWLock.EnterReadLock ();

			TabItem[] tabItms = Children.Cast<TabItem> ().OrderBy (t => t.ViewIndex).ToArray ();

			int selTabViewIdx = -1;
			if (SelectedTab < tabItms.Length && SelectedTab >= 0)
				selTabViewIdx = (Children [SelectedTab] as TabItem).ViewIndex;

			childrenRWLock.ExitReadLock ();

			int i = 0;
			while (i < selTabViewIdx) {
				tabItms [i].Paint (ref gr);
				i++;
			}
			i = tabItms.Length - 1;
			while (i > selTabViewIdx) {
				tabItms [i].Paint (ref gr);
				i--;
			}

			if (selTabViewIdx >= 0)
				tabItms [selTabViewIdx].Paint (ref gr);
		
			gr.Restore ();
		}

		void Ti_TabTitle_LayoutChanged (object sender, LayoutingEventArgs e)
		{
			if (e.LayoutType == LayoutingType.X)				
				RegisterForLayouting (LayoutingType.ArrangeChildren);			
		}
		void Ti_MouseDown (object sender, MouseButtonEventArgs e)
		{
			SelectedTab = Children.IndexOf (sender as GraphicObject);
		}
	}
}


//
// TabItem.cs
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
using System.Diagnostics;
using Cairo;
using System.Linq;

namespace Crow
{
	public class TabItem : TemplatedContainer
	{
		#region CTOR
		protected TabItem() : base(){}
		public TabItem (Interface iface) : base(iface){}
		#endregion

		public event EventHandler QueryClose;

		#region Private fields
		GraphicObject titleWidget;
		int tabOffset;
		bool isSelected;
		Measure tabThickness;
		#endregion

		#region TemplatedControl overrides
		public override GraphicObject Content {
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
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			titleWidget = this.child.FindByName ("TabTitle");
		}
		internal GraphicObject TabTitle { get { return titleWidget; }}
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
				NotifyValueChanged ("ViewIndex", viewIndex);
			}
		}

		[XmlAttributeAttribute][DefaultValue("18")]
		public virtual Measure TabThickness {
			get { return tabThickness; }
			set {
				if (tabThickness == value)
					return;
				tabThickness = value;
				NotifyValueChanged ("TabThickness", tabThickness);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute][DefaultValue(0)]
		public virtual int TabOffset {
			get { return tabOffset; }
			set {
				if (tabOffset == value)
					return;
				tabOffset = value;
				NotifyValueChanged ("TabOffset", tabOffset);

				RegisterForLayouting (LayoutingType.X);
				RegisterForGraphicUpdate ();
			}
		}

		[XmlAttributeAttribute][DefaultValue(false)]
		public virtual bool IsSelected {
			get { return isSelected; }
			set {
				if (isSelected == value)
					return;
				isSelected = value;
				NotifyValueChanged ("IsSelected", isSelected);
			}
		}
		protected override void onDraw (Context gr)
		{
			gr.Save ();

			int spacing = (Parent as TabView).Spacing;

			gr.MoveTo (0.5, TabTitle.Slot.Bottom-0.5);
			gr.LineTo (TabTitle.Slot.Left - spacing, TabTitle.Slot.Bottom-0.5);
			gr.CurveTo (
				TabTitle.Slot.Left - spacing / 2, TabTitle.Slot.Bottom-0.5,
				TabTitle.Slot.Left - spacing / 2, 0.5,
				TabTitle.Slot.Left, 0.5);
			gr.LineTo (TabTitle.Slot.Right, 0.5);
			gr.CurveTo (
				TabTitle.Slot.Right + spacing / 2, 0.5,
				TabTitle.Slot.Right + spacing / 2, TabTitle.Slot.Bottom-0.5,
				TabTitle.Slot.Right + spacing, TabTitle.Slot.Bottom-0.5);
			gr.LineTo (Slot.Width-0.5, TabTitle.Slot.Bottom-0.5);


			gr.LineTo (Slot.Width-0.5, Slot.Height-0.5);
			gr.LineTo (0.5, Slot.Height-0.5);
			gr.ClosePath ();
			gr.LineWidth = 1;
			Foreground.SetAsSource (gr);
			gr.StrokePreserve ();

			gr.Clip ();
			base.onDraw (gr);
			gr.Restore ();
		}

		#region Mouse Handling
		public bool HoldCursor = false;
		public override bool PointIsIn (ref Point m)
		{
			if (!base.PointIsIn (ref m))
				return false;

			if (m.Y < tabThickness)
				return TabTitle.Slot.ContainsOrIsEqual (m);
			else
				return this.isSelected;
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
			HoldCursor = true;
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			HoldCursor = false;
			(Parent as TabView).UpdateLayout (LayoutingType.ArrangeChildren);
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!(HasFocus && HoldCursor))
				return;
			TabView tv = Parent as TabView;
			TabItem previous = null, next = null;
			int tmp = TabOffset + e.XDelta;
			if (tmp < tv.Spacing)
				TabOffset = tv.Spacing;
			else if (tmp > Parent.getSlot ().Width - TabTitle.Slot.Width - tv.Spacing)
				TabOffset = Parent.getSlot ().Width - TabTitle.Slot.Width - tv.Spacing;
			else{
				TabItem[] tabItms = tv.Children.Cast<TabItem>().OrderBy (t=>t.ViewIndex).ToArray();
				if (ViewIndex > 0 && e.XDelta < 0) {
					previous = tabItms [ViewIndex - 1];
					if (tmp < previous.TabOffset + previous.TabTitle.Slot.Width / 2) {
						previous.ViewIndex = ViewIndex;
						ViewIndex--;
						tv.UpdateLayout (LayoutingType.ArrangeChildren);
					}

				}else if (ViewIndex < tabItms.Length - 1 && e.XDelta > 0) {
					next = tabItms [ViewIndex + 1];
					if (tmp > next.TabOffset - next.TabTitle.Slot.Width / 2){
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
				(Parent as TabView).DeleteChild (this);
		}
		#endregion

	}
}


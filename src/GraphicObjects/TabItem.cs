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

namespace Crow
{
	public class TabItem : TemplatedContainer
	{
		#region CTOR
		public TabItem() : base(){}
		public TabItem (Interface iface) : base(iface){}
		#endregion

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

			_contentContainer = this.child.FindByName ("Content") as Container;
			titleWidget = this.child.FindByName ("TabTitle");
		}
		internal GraphicObject TabTitle { get { return titleWidget; }}
		#endregion

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
		protected override void onDraw (Cairo.Context gr)
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
			gr.LineWidth = 2;
			Foreground.SetAsSource (gr);
			gr.StrokePreserve ();

			gr.Clip ();
			base.onDraw (gr);
			gr.Restore ();
		}

		#region Mouse Handling
		public override bool MouseIsIn (Point m)
		{
			if (!(Visible & IsEnabled) || IsDragged)
				return false;

			bool mouseIsInTitle = TabTitle.ScreenCoordinates (TabTitle.Slot).ContainsOrIsEqual (m);
			if (!IsSelected)
				return mouseIsInTitle;

			return _contentContainer.ScreenCoordinates (_contentContainer.Slot).ContainsOrIsEqual (m)
				|| mouseIsInTitle;
		}
		bool holdCursor = false;
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
			holdCursor = true;
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			holdCursor = false;
			(Parent as TabView).UpdateLayout (LayoutingType.ArrangeChildren);
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!(HasFocus&&holdCursor))
				return;
			TabView tv = Parent as TabView;
			TabItem previous = null, next = null;
			int tmp = TabOffset + e.XDelta;
			if (tmp < tv.Spacing)
				TabOffset = tv.Spacing;
			else if (tmp > Parent.getSlot ().Width - TabTitle.Slot.Width - tv.Spacing)
				TabOffset = Parent.getSlot ().Width - TabTitle.Slot.Width - tv.Spacing;
			else{
				int idx = tv.Children.IndexOf (this);
				if (idx > 0 && e.XDelta < 0) {
					previous = tv.Children [idx - 1] as TabItem;

					if (tmp < previous.TabOffset + previous.TabTitle.Slot.Width / 2) {
						tv.Children.RemoveAt (idx);
						tv.Children.Insert (idx - 1, this);
						tv.SelectedTab = idx - 1;
						tv.UpdateLayout (LayoutingType.ArrangeChildren);
					}

				}else if (idx < tv.Children.Count - 1 && e.XDelta > 0) {
					next = tv.Children [idx + 1] as TabItem;
					if (tmp > next.TabOffset - next.TabTitle.Slot.Width / 2){
						tv.Children.RemoveAt (idx);
						tv.Children.Insert (idx + 1, this);
						tv.SelectedTab = idx + 1;
						tv.UpdateLayout (LayoutingType.ArrangeChildren);
					}
				}
				TabOffset = tmp;
			}
		}
		public void butCloseTabClick (object sender, MouseButtonEventArgs e){
			(Parent as TabView).RemoveChild(this);
		}
		#endregion

	}
}


//
//  TabItem.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.TabItem.crow")]
	public class TabItem : TemplatedContainer
	{
		#region Private fields
		string caption;
		Container _contentContainer;
		GraphicObject _tabTitle;
		int tabOffset;
		#endregion

		#region CTOR
		public TabItem () : base()
		{
		}
		#endregion

		#region TemplatedControl overrides
		public override GraphicObject Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				_contentContainer.SetChild(value);
			}
		}
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (_contentContainer != null)
				_contentContainer.ResolveBindings ();
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			_contentContainer = this.child.FindByName ("Content") as Container;
			_tabTitle = this.child.FindByName ("TabTitle");
		}
		internal GraphicObject TabTitle { get { return _tabTitle; }}
		#endregion

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
		[XmlAttributeAttribute][DefaultValue("TabItem")]
		public string Caption {
			get { return caption; }
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChanged ("Caption", caption);
			}
		}
		bool isSelected;
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
			int spacing = (Parent as TabView).Spacing;
			gr.MoveTo (0, TabTitle.Slot.Bottom);
			gr.LineTo (TabTitle.Slot.Left - spacing, TabTitle.Slot.Bottom);
			gr.CurveTo (
				TabTitle.Slot.Left - spacing / 2, TabTitle.Slot.Bottom,
				TabTitle.Slot.Left - spacing / 2, 1,
				TabTitle.Slot.Left, 1);
			gr.LineTo (TabTitle.Slot.Right, 1);
			gr.CurveTo (
				TabTitle.Slot.Right + spacing / 2, 1,
				TabTitle.Slot.Right + spacing / 2, TabTitle.Slot.Bottom,
				TabTitle.Slot.Right + spacing, TabTitle.Slot.Bottom);
			gr.LineTo (Slot.Width, TabTitle.Slot.Bottom);

			gr.LineWidth = 1;
			Foreground.SetAsSource (gr);
			gr.StrokePreserve ();

			gr.LineTo (Slot.Width, Slot.Height);
			gr.LineTo (0, Slot.Height);
			gr.ClosePath ();
			gr.Save ();
			gr.Clip ();
			base.onDraw (gr);
			gr.Restore ();
		}

		#region Mouse Handling
		public override bool MouseIsIn (Point m)
		{
			if (!Visible)
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


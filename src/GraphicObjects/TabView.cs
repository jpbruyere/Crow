//
//  TabView.cs
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
using Cairo;
using System.Diagnostics;

namespace Crow
{
	//TODO:use TemplatedGroup
	public class TabView : Group
	{
		#region Private fields
		int _spacing;
		Measure tabThickness;
		Orientation _orientation;
		int selectedTab = 0;
		#endregion

		public TabView () : base()
		{
		}

		#region public properties
		[XmlAttributeAttribute()][DefaultValue(Orientation.Horizontal)]
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
			}
		}
		[XmlAttributeAttribute()][DefaultValue(20)]
		public int Spacing
		{
			get { return _spacing; }
			set {
				if (_spacing == value)
					return;
				_spacing = value;
				NotifyValueChanged ("Spacing", Spacing);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int SelectedTab {
			get { return selectedTab; }
			set {
				if (selectedTab < Children.Count && SelectedTab >= 0)
					(Children [selectedTab] as TabItem).IsSelected = false;

				selectedTab = value;

				if (selectedTab < Children.Count && SelectedTab >= 0)
					(Children [selectedTab] as TabItem).IsSelected = true;

				NotifyValueChanged ("SelectedTab", selectedTab);
				RegisterForRedraw ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("18")]
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
		#endregion

		public override void AddChild (GraphicObject child)
		{
			TabItem ti = child as TabItem;
			if (ti == null)
				throw new Exception ("TabView control accept only TabItem as child.");

			ti.MouseDown += Ti_MouseDown;

			if (Children.Count == 0) {
				ti.IsSelected = true;
				SelectedTab = 0;
			}

			base.AddChild (child);
		}
		public override void RemoveChild (GraphicObject child)
		{
			base.RemoveChild (child);
			if (selectedTab > Children.Count - 1)
				SelectedTab--;
			else
				SelectedTab = selectedTab;
		}
		public override bool ArrangeChildren { get { return true; } }
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			RegisteredLayoutings &= (~layoutType);

			if (layoutType == LayoutingType.ArrangeChildren) {
				int curOffset = Spacing;
				for (int i = 0; i < Children.Count; i++) {
					if (!Children [i].Visible)
						continue;
					TabItem ti = Children [i] as TabItem;
					ti.TabOffset = curOffset;
					if (Orientation == Orientation.Horizontal) {
						if (ti.TabTitle.RegisteredLayoutings.HasFlag (LayoutingType.Width))
							return false;
						curOffset += ti.TabTitle.Slot.Width + Spacing;
					} else {
						if (ti.TabTitle.RegisteredLayoutings.HasFlag (LayoutingType.Height))
							return false;
						curOffset += ti.TabTitle.Slot.Height + Spacing;
					}
				}

				//if no layouting remains in queue for item, registre for redraw
				if (RegisteredLayoutings == LayoutingType.None && bmp == null)
					CurrentInterface.EnqueueForRepaint (this);

				return true;
			}

			return base.UpdateLayout(layoutType);
		}
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

			for (int i = 0; i < Children.Count; i++) {
				if (i == SelectedTab)
					continue;
				Children [i].Paint (ref gr);
			}

			if (SelectedTab < Children.Count && SelectedTab >= 0)
				Children [SelectedTab].Paint (ref gr);

			gr.Restore ();
		}

		#region Mouse handling
		public override void checkHoverWidget (MouseMoveEventArgs e)
		{
			if (CurrentInterface.HoverWidget != this) {
				CurrentInterface.HoverWidget = this;
				onMouseEnter (this, e);
			}

			if (SelectedTab > Children.Count - 1)
				return;

			if (((Children[SelectedTab] as TabItem).Content.Parent as GraphicObject).MouseIsIn(e.Position))
			{
				Children[SelectedTab].checkHoverWidget (e);
				return;
			}
			for (int i = Children.Count - 1; i >= 0; i--) {
				TabItem ti = Children [i] as TabItem;
				if (ti.TabTitle.MouseIsIn(e.Position))
				{
					Children[i].checkHoverWidget (e);
					return;
				}
			}
		}
		#endregion

		void Ti_MouseDown (object sender, MouseButtonEventArgs e)
		{
			SelectedTab = Children.IndexOf (sender as GraphicObject);
		}
	}
}


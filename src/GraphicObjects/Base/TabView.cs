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

namespace Crow
{	
	public class TabView : Group
	{
		#region CTOR
		public TabView () : base() {}
		#endregion

		#region Private fields
		int _spacing;
		Measure tabThickness;
		Orientation _orientation;
		int selectedTab = 0;
		#endregion


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
		[XmlAttributeAttribute()][DefaultValue(16)]
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
				if (RegisteredLayoutings == LayoutingType.None && IsDirty)
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


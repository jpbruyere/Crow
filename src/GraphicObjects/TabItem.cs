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

		#region GraphicObject overrides
		[XmlAttributeAttribute()][DefaultValue(false)]
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		#endregion

		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int TabOffset {
			get { return tabOffset; }
			set {
				if (tabOffset == value)
					return;
				tabOffset = value;
				NotifyValueChanged ("TabOffset", tabOffset);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("TabItem")]
		public string Caption {
			get { return caption; }
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChanged ("Caption", caption);
			}
		}
		public override bool MouseIsIn (Point m)
		{
			if (!Visible)
				return false;

			Debug.WriteLine ("Mouse Testing " + this.ToString ());

			if (TabTitle.MouseIsIn (m)){
				Debug.WriteLine ("Mouse is in title of " + this.ToString ());
				return true;
			}
			if (Content.MouseIsIn (m)){
				Debug.WriteLine ("Mouse is in content of " + this.ToString ());
				return true;
			}

			Debug.WriteLine ("Mouse is not in " + this.ToString ());
			return false;
		}
		protected override void onDraw (Cairo.Context gr)
		{
			int spacing = (Parent as TabView).Spacing;
			gr.MoveTo (0, TabTitle.Slot.Bottom);
			gr.LineTo (TabTitle.Slot.Left - spacing, TabTitle.Slot.Bottom);
			gr.CurveTo (
				TabTitle.Slot.Left - spacing / 2, TabTitle.Slot.Bottom,
				TabTitle.Slot.Left - spacing / 2, 0,
				TabTitle.Slot.Left, 0);
			gr.LineTo (TabTitle.Slot.Right, 0);
			gr.CurveTo (
				TabTitle.Slot.Right + spacing / 2, 0,
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
	}
}


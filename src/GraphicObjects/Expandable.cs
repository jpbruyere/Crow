//
//  Expandable.cs
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
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
    public class Expandable : TemplatedContainer
    {
		#region CTOR
		public Expandable() : base()
		{
		}
		#endregion

		#region Private fields
		bool _isExpanded;
		string caption;
		string image;
		Container _contentContainer;
		#endregion

		#region Event Handlers
		public event EventHandler Expand;
		public event EventHandler Collapse;
		#endregion

		#region GraphicObject overrides
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			if (this.HasFocus)
				IsExpanded = !IsExpanded;
			base.onMouseClick (sender, e);
		}
		#endregion

		public override GraphicObject Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				_contentContainer.SetChild(value);
				NotifyValueChanged ("HasContent", HasContent);
			}
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			_contentContainer = this.child.FindByName ("Content") as Container;
		}
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}

		#region Public properties
		[XmlAttributeAttribute()][DefaultValue("Expandable")]
		public string Caption {
			get { return caption; } 
			set {
				if (caption == value)
					return;
				caption = value; 
				NotifyValueChanged ("Caption", caption);
			}
		}        
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.Icons.expandable.svg")]
		public string Image {
			get { return image; } 
			set {
				if (image == value)
					return;
				image = value; 
				NotifyValueChanged ("Image", image);
			}
		}     
		[XmlAttributeAttribute()][DefaultValue(false)]
        public bool IsExpanded
        {
			get { return _isExpanded; }
            set
            {
				if (value == _isExpanded)
					return;

				_isExpanded = value;

				if (!HasContent)
					_isExpanded = false;

				NotifyValueChanged ("IsExpanded", _isExpanded);

				if (_isExpanded)
					onExpand (this, null);
				else
					onCollapse (this, null);
            }
        }
		[XmlIgnore]public bool HasContent {
			get { return _contentContainer == null ? false : _contentContainer.Child != null; }
		}

		#endregion

		public virtual void onExpand(object sender, EventArgs e)
		{
			if (_contentContainer != null)
				_contentContainer.Visible = true;
			
			Expand.Raise (this, e);
		}
		public virtual void onCollapse(object sender, EventArgs e)
		{
			if (_contentContainer != null)
				_contentContainer.Visible = false;
			
			Collapse.Raise (this, e);
		}
	}
}

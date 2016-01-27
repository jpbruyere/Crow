using System;


using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OpenTK.Graphics.OpenGL;

using Cairo;

using winColors = System.Drawing.Color;
using System.Diagnostics;
using System.Xml.Serialization;
using OpenTK.Input;
using System.ComponentModel;
using System.Xml;
using System.IO;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.Expandable.goml")]
    public class Expandable : TemplatedContainer
    {		
		bool _isExpanded;
		string title;
		string image;
		Container _contentContainer;

		public event EventHandler Expand;
		public event EventHandler Collapse;

		public override GraphicObject Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				_contentContainer.SetChild(value);
			}
		}

		public Expandable() : base()
		{
		}	

		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			_contentContainer = this.child.FindByName ("Content") as Container;
		}

		[XmlAttributeAttribute()][DefaultValue(true)]//overiden to get default to true
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}

		[XmlAttributeAttribute()][DefaultValue("Expandable")]
		public string Title {
			get { return title; } 
			set {
				if (title == value)
					return;
				title = value; 
				NotifyValueChanged ("Title", title);
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
				_isExpanded = value;

				if (_isExpanded) {
					onExpand (this, null);
					NotifyValueChanged ("SvgSub", "expanded");
					return;
				}

				onCollapse (this, null);
 				NotifyValueChanged ("SvgSub", "collapsed");
            }
        }

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
			
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsExpanded = !IsExpanded;
			base.onMouseClick (sender, e);
		}
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}
	}
}

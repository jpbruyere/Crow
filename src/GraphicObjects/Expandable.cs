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

namespace go
{
	[DefaultTemplate("#go.Templates.Expandable.goml")]
    public class Expandable : TemplatedContainer
    {		
		bool _isExpanded;
		Label _caption;
		Image _image;
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
			_caption = this.child.FindByName ("Caption") as Label;
			_image = this.child.FindByName ("Image") as Image;

			if (_image == null)
				return;
			_image.SvgSub = "collapsed";

//			this.Expand += (object sender, EventArgs e) => {};
//			this.Collapse += (object sender, EventArgs e) => {};

		}
		[XmlAttributeAttribute()][DefaultValue(true)]//overiden to get default to true
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}

		[XmlAttributeAttribute()][DefaultValue("Expandable")]
		public string Title {
			get { return _caption.Text; } 
			set {
				if (_caption == null)
					return;
				_caption.Text = value; 
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

				if (_isExpanded)
					onExpand (this, null);
				else
					onCollapse (this, null);

                //registerForGraphicUpdate();
            }
        }

		public virtual void onExpand(object sender, EventArgs e)
		{
			_contentContainer.Visible = true;
			_image.SvgSub = "expanded";
			Expand.Raise (this, e);
		}
		public virtual void onCollapse(object sender, EventArgs e)
		{
			_contentContainer.Visible = false;
			_image.SvgSub = "collapsed";
			Collapse.Raise (this, e);
		}
			
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsExpanded = !IsExpanded;
			base.onMouseClick (sender, e);
		}

	}
}

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

namespace go
{
	[DefaultTemplate("#go.Templates.RadioButton.goml")]
    public class RadioButton : TemplatedControl
    {		        
		Label _caption;
		Image _image;

		public RadioButton() : base()
		{
		}	

		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			_caption = this.child.FindByName ("Caption") as Label;//TODO:should use bindings instead
			_image = this.child.FindByName ("Image") as Image;
			_image.SvgSub = "unchecked";
		}

		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(true)]//overiden to get default to true
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}

		[XmlAttributeAttribute][DefaultValue("RadioButton")]
		public string Caption {
			get { return _caption.Text; } 
			set { 
				_caption.Text = value; 
			}
		}
        [XmlAttributeAttribute()][DefaultValue(false)]
        public bool IsChecked
        {
			get { return _image == null ? false :_image.SvgSub == "checked"; }
            set
            {
//                if (value == IsChecked)
//                    return;
//
				if (value)
					_image.SvgSub = "checked";
				else
					_image.SvgSub = "unchecked";
				                
                registerForGraphicUpdate();
            }
        }

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{						
			Group pg = Parent as Group;
			if (pg != null) {
				foreach (RadioButton c in pg.Children.OfType<RadioButton>())
					c.IsChecked = (c == this);
			}

			base.onMouseClick (sender, e);
		}
	}
}

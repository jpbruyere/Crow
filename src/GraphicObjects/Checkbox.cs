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
    public class Checkbox : TemplatedControl
    {		        
		Label _caption;
		Image _image;

		public Checkbox() : base()
		{
		}	

		protected override void loadTemplate()
		{
			this.setChild (Interface.Load ("#go.Templates.Checkbox.goml"));
			_caption = this.child.FindByName ("Caption") as Label;
			_image = this.child.FindByName ("Image") as Image;
			_image.SvgSub = "unchecked";
		}
			

		[XmlAttributeAttribute()][DefaultValue("Checkbox")]
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
                if (value == IsChecked)
                    return;

				if (value)
					_image.SvgSub = "checked";
				else
					_image.SvgSub = "unchecked";
				                
                registerForGraphicUpdate();
            }
        }

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsChecked = !IsChecked;
			base.onMouseClick (sender, e);
		}
	}
}

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
		string caption;
		string image;
		bool isChecked;

		#region CTOR
		public RadioButton() : base(){}	
		#endregion
			
		#region GraphicObject overrides
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
		#endregion

		[XmlAttributeAttribute()][DefaultValue("RadioButton")]
		public string Caption {
			get { return caption; } 
			set {
				if (caption == value)
					return;
				caption = value; 
				NotifyValueChanged ("Caption", caption);
			}
		}        
		[XmlAttributeAttribute()][DefaultValue("#go.Images.Icons.radiobutton.svg")]
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
        public bool IsChecked
        {
			get { return isChecked; }
            set
            {
				isChecked = value;

				NotifyValueChanged ("IsChecked", value);
				if (isChecked)
					NotifyValueChanged ("SvgSub", "checked");
				else
					NotifyValueChanged ("SvgSub", "unchecked");
            }
        }

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{						
			Group pg = Parent as Group;
			if (pg != null) {
				foreach (RadioButton c in pg.Children.OfType<RadioButton>())
					c.IsChecked = (c == this);
			} else
				IsChecked = !IsChecked;

			base.onMouseClick (sender, e);
		}
	}
}

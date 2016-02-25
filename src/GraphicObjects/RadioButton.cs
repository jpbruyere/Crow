using System;
using OpenTK.Input;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.RadioButton.style")]
	[DefaultTemplate("#Crow.Templates.RadioButton.goml")]
    public class RadioButton : TemplatedControl
    {		        
		string caption;
		string image;
		bool isChecked;

		#region CTOR
		public RadioButton() : base(){}	
		#endregion

		public event EventHandler Checked;
		public event EventHandler Unchecked;

		#region GraphicObject overrides
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{						
			Group pg = Parent as Group;
			if (pg != null) {
				for (int i = 0; i < pg.Children.Count; i++) {
					RadioButton c = pg.Children [i] as RadioButton;
					if (c == null)
						continue;
					c.IsChecked = (c == this);
				}
			} else
				IsChecked = !IsChecked;

			base.onMouseClick (sender, e);
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
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.Icons.radiobutton.svg")]
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
				if (isChecked == value)
					return;
				
				isChecked = value;

				NotifyValueChanged ("IsChecked", value);

				if (isChecked)
					Checked.Raise (this, null);
				else
					Unchecked.Raise (this, null);
            }
        }
	}
}

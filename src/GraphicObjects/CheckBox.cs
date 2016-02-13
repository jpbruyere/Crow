using System;
using OpenTK.Input;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.CheckBox.goml")]
	public class CheckBox : TemplatedControl
	{
		string caption;
		string image;
		bool isChecked;

		#region CTOR
		public CheckBox() : base()
		{			
		}							
		#endregion

		public event EventHandler Checked;
		public event EventHandler Unchecked;

		#region GraphicObject overrides
		[XmlAttributeAttribute()][DefaultValue(true)]
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		#endregion

		[XmlAttributeAttribute()][DefaultValue("Checkbox")]
		public string Caption {
			get { return caption; } 
			set {
				if (caption == value)
					return;
				caption = value; 
				NotifyValueChanged ("Caption", caption);
			}
		}        
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.Icons.checkbox.svg")]
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
				if (isChecked) {
					NotifyValueChanged ("SvgSub", "checked");
					Checked.Raise (this, null);
				} else {
					NotifyValueChanged ("SvgSub", "unchecked");
					Unchecked.Raise (this, null);
				}
			}
		}
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsChecked = !IsChecked;
			base.onMouseClick (sender, e);
		}
	}
}

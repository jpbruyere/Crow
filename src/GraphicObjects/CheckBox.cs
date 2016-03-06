using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.CheckBox.style")]
	[DefaultTemplate("#Crow.Templates.CheckBox.goml")]
	public class CheckBox : TemplatedControl
	{
		string caption;
		string image;
		bool isChecked;

		#region CTOR
		public CheckBox() : base()
		{}							
		#endregion

		public event EventHandler Checked;
		public event EventHandler Unchecked;

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

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsChecked = !IsChecked;
			base.onMouseClick (sender, e);
		}
	}
}

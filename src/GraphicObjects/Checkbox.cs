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
	[DefaultTemplate("#go.Templates.Checkbox.goml")]
    public class Checkbox : TemplatedControl
    {		        
		string caption;
		string image;
		bool isChecked;

		#region CTOR
		public Checkbox() : base()
		{			
		}							
		#endregion

		public event EventHandler Checked;
		public event EventHandler Unchecked;

		#region GraphicObject overrides
//		[XmlAttributeAttribute()][DefaultValue(-1)]
//		public override int Height {
//			get { return base.Height; }
//			set { base.Height = value; }
//		}
		[XmlAttributeAttribute()][DefaultValue(true)]//overiden to get default to true
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
		[XmlAttributeAttribute()][DefaultValue("#go.Images.Icons.checkbox.svg")]
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

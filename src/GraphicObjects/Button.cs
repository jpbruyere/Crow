using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OpenTK.Graphics.OpenGL;

using System.Diagnostics;

using System.Xml.Serialization;
using Cairo;
using System.ComponentModel;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.Button.style")]
	[DefaultTemplate("#Crow.Templates.Button.crow")]
    public class Button : TemplatedContainer
    {
		#region CTOR
        public Button() : base()
        {}
		#endregion

		string caption;
		string image;
		Container _contentContainer;

		public override GraphicObject Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				if (_contentContainer != null)					
					_contentContainer.SetChild(value);
			}
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			_contentContainer = this.child.FindByName ("Content") as Container;
		}

		#region GraphicObject Overrides
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
			NotifyValueChanged ("State", "pressed");
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			NotifyValueChanged ("State", "normal");
		}
		#endregion

		[XmlAttributeAttribute()][DefaultValue("Button")]
		public string Caption {
			get { return caption; } 
			set {
				if (caption == value)
					return;
				caption = value; 
				NotifyValueChanged ("Caption", caption);
			}
		}        
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.button.svg")]
		public string Image {
			get { return image; } 
			set {
				if (image == value)
					return;
				image = value; 
				NotifyValueChanged ("Image", image);
			}
		}     
	}
}

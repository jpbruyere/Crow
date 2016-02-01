using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OpenTK.Graphics.OpenGL;

using System.Diagnostics;

using System.Xml.Serialization;
using Cairo;
using OpenTK.Input;
using System.ComponentModel;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.Button.crow")]
    public class Button : TemplatedContainer
    {
		#region CTOR
        public Button() : base()
        {
			//MouseEnter += delegate { Background = Color.RedDevil;};
			//MouseLeave += delegate { Background = Color.Transparent;};
//			MouseButtonDown += delegate { BackImgSub = "pressed"; registerForGraphicUpdate();};
//			MouseButtonUp += delegate { BackImgSub = "normal";registerForGraphicUpdate();};
		}
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
//		[XmlAttributeAttribute()][DefaultValue(50)]
//		public override int Width {
//			get { return base.Width; }
//			set { base.Width = value; }
//		}
//		[XmlAttributeAttribute()][DefaultValue(20)]
//		public override int Height {
//			get { return base.Height; }
//			set { base.Height = value; }
//		}
		[XmlAttributeAttribute()][DefaultValue(true)]
        public override bool Focusable
        {
            get { return base.Focusable; }
            set { base.Focusable = value; }
		}
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

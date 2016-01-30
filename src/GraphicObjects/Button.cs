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
    public class Button : Container, IXmlSerializable
    {
		#region CTOR
        public Button() : base()
        {
			//MouseEnter += delegate { Background = Color.RedDevil;};
			//MouseLeave += delegate { Background = Color.Transparent;};
			MouseButtonDown += delegate { BackImgSub = "pressed"; registerForGraphicUpdate();};
			MouseButtonUp += delegate { BackImgSub = "normal";registerForGraphicUpdate();};
		}
		#endregion

		#region GraphicObject Overrides
		[XmlAttributeAttribute()][DefaultValue(50)]
		public override int Width {
			get { return base.Width; }
			set { base.Width = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(20)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("Transparent")]
		public override Color Background {
			get { return base.Background; }
			set { base.Background = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(true)]
        public override bool Focusable
        {
            get { return base.Focusable; }
            set { base.Focusable = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.button.svg")]
		public override Picture BackgroundImage {
			get {
				return base.BackgroundImage;
			}
			set {
				base.BackgroundImage = value;
				BackImgSub = "normal";
			}
		}
		#endregion

		[XmlAttributeAttribute][DefaultValue("Button")]
		public string Text
		{
			get {
				Label l = Child as Label;
				return l == null ? "" : l.Text; 
			}
			set
			{
				Label l = Child as Label;
				if (l == null)
					this.SetChild(new Label (value) 
						{ 
							TextAlignment = Alignment.Center,
							Foreground = Color.Black
						});
				else
					l.Text = value;
			}
		}       
	}
}

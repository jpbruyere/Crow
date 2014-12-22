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

namespace go
{
    public class Button : Container, IXmlSerializable
    {
		#region CTOR
        public Button() : base()
        {
			MouseEnter += delegate { Background = Color.Lion;};
			MouseLeave += delegate { Background = Color.Gray;};
			MouseButtonDown += delegate { Background = Color.Red;};
			MouseButtonUp += delegate { Background = Color.Gray;};
		}
		#endregion

		#region GraphicObject Overrides
		[XmlAttributeAttribute()][DefaultValue(60)]
		public override int Width {
			get { return base.Width; }
			set { base.Width = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(30)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("Gray")]
		public virtual Color Background {
			get { return base.Background; }
			set { base.Background = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(true)]
        public override bool Focusable
        {
            get { return base.Focusable; }
            set { base.Focusable = value; }
        }	

		#endregion

		[XmlAttributeAttribute][DefaultValue("Button")]
		public string Text
		{
			get {
				Label l = child as Label;
				return l == null ? "" : l.Text; 
			}
			set
			{
				Label l = child as Label;
				if (l == null)
					this.setChild(new Label (value) 
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

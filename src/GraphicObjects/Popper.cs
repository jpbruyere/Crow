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
using System.Xml;
using System.IO;

namespace go
{
	[DefaultTemplate("#go.Templates.Popper.goml")]
    public class Popper : TemplatedContainer
    {		
		bool _isPopped;
		Label _caption;
		Image _image;
		GraphicObject _content;

		public event EventHandler Pop;
		public event EventHandler Unpop;

		public override GraphicObject Content {
			get { return _content; }
			set { _content = value; }
		}
		public Popper() : base()
		{
		}	

		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			_caption = this.child.FindByName ("Caption") as Label;
			_image = this.child.FindByName ("Image") as Image;

			if (_image == null)
				return;
			_image.SvgSub = "collapsed";

			this.Pop += (object sender, EventArgs e) => {_image.SvgSub = "expanded";};
			this.Unpop += (object sender, EventArgs e) => {_image.SvgSub = "collapsed";};

		}
			

		[XmlAttributeAttribute()][DefaultValue("Popper")]
		public string Title {
			get { return _caption.Text; } 
			set {
				if (_caption == null)
					return;
				_caption.Text = value; 
			}
		}        
      
		[XmlAttributeAttribute()][DefaultValue(false)]
        public bool IsPopped
        {
			get { return _isPopped; }
            set
            {
                if (value == _isPopped)
                    return;

				_isPopped = value;

				if (_isPopped)
					onPop (this, null);
				else
					onUnpop (this, null);

                registerForGraphicUpdate();
            }
        }

		public virtual void onPop(object sender, EventArgs e)
		{
			if (Content != null) {
				Rectangle r = this.ScreenCoordinates (this.Slot);
				Content.Visible = true;
				Content.Left = r.Left;
				Content.Top = r.Bottom;
				TopContainer.AddWidget (Content);
			}
			Pop.Raise (this, e);
		}
		public virtual void onUnpop(object sender, EventArgs e)
		{
			TopContainer.DeleteWidget (Content);
			Unpop.Raise (this, e);
		}
			
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsPopped = !IsPopped;
			base.onMouseClick (sender, e);
		}

	}
}

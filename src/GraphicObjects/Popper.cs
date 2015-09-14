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
		string title;
		string image;
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

		[XmlAttributeAttribute()][DefaultValue("Popper")]
		public string Title {
			get { return title; } 
			set {
				if (title == value)
					return;
				title = value; 
				NotifyValueChanged ("Title", title);
			}
		}        
		[XmlAttributeAttribute()][DefaultValue("#go.Images.Icons.expandable.svg")]
		public string Image {
			get { return image; } 
			set {
				if (image == value)
					return;
				image = value; 
				NotifyValueChanged ("Image", image);
			}
		} 
		[XmlAttributeAttribute()][DefaultValue("collapsed")]
		public string SvgSub {
			get { return IsPopped ? "expanded" : "collapsed"; } 
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
				NotifyValueChanged ("SvgSub", SvgSub);

				if (_isPopped)
					onPop (this, null);
				else
					onUnpop (this, null);

                //registerForGraphicUpdate();
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

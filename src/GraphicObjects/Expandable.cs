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
    public class Expandable : TemplatedControl
    {		
		bool _isExpanded;
		Label _caption;
		Image _image;

		public Container Content;

		public event EventHandler Expand;
		public event EventHandler Collapse;

		public Expandable() : base()
		{
		}	

		protected override void loadTemplate(GraphicObject template = null)
		{
			if (template == null)
				this.SetChild (Interface.Load ("#go.Templates.Expandable.goml",this));
			else
				this.SetChild (template);

			_caption = this.child.FindByName ("Caption") as Label;
			Content = this.child.FindByName ("Content") as Container;
			_image = this.child.FindByName ("Image") as Image;

			if (_image == null)
				return;
			_image.SvgSub = "collapsed";
			this.Expand += (object sender, EventArgs e) => {_image.SvgSub = "expanded";};
			this.Collapse += (object sender, EventArgs e) => {_image.SvgSub = "collapsed";};

		}
			

		[XmlAttributeAttribute()][DefaultValue("Expandable")]
		public string Title {
			get { return _caption.Text; } 
			set {
				if (_caption == null)
					return;
				_caption.Text = value; 
			}
		}        
      
		[XmlAttributeAttribute()][DefaultValue(false)]
        public bool IsExpanded
        {
			get { return _isExpanded; }
            set
            {
                if (value == _isExpanded)
                    return;

				_isExpanded = value;

				if (_isExpanded)
					onExpand (this, null);
				else
					onCollapse (this, null);

                registerForGraphicUpdate();
            }
        }

		public virtual void onExpand(object sender, EventArgs e)
		{
			Content.Visible = true;
			Expand.Raise (this, e);
		}
		public virtual void onCollapse(object sender, EventArgs e)
		{
			Content.Visible = false;
			Collapse.Raise (this, e);
		}
			
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsExpanded = !IsExpanded;
			base.onMouseClick (sender, e);
		}

		public override void ReadXml(System.Xml.XmlReader reader)
		{
			using (System.Xml.XmlReader subTree = reader.ReadSubtree ()) {
				subTree.Read ();
				string tmp = subTree.ReadOuterXml ();

				//seek for template tag
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read ();
					base.ReadXml (xr);		
				}
				//process content
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read (); //skip current node

					while (!xr.EOF) {
						xr.Read (); //read first child

						if (!xr.IsStartElement ())
							continue;
						if (xr.Name == "Template")
							continue;

						Type t = Type.GetType ("go." + xr.Name);
						GraphicObject go = (GraphicObject)Activator.CreateInstance (t);                                

						(go as IXmlSerializable).ReadXml (xr);

						Content.SetChild (go);

						xr.Read (); //closing tag
					}
						
				}
			}
		}
		public override void WriteXml(System.Xml.XmlWriter writer)
		{
			base.WriteXml(writer);

			if (Content == null)
				return;
			if (Content.Child == null)
				return;
			//TODO: if template is not the default one, we have to save it
			writer.WriteStartElement(Content.Child.GetType().Name);
			(Content.Child as IXmlSerializable).WriteXml(writer);
			writer.WriteEndElement();
		}
	}
}

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
	[DefaultTemplate("#go.Templates.Groupbox.goml")]
    public class Groupbox : TemplatedContainer
    {		
		string title;
		Container _contentContainer;

		#region CTOR
		public Groupbox() : base(){}	
		#endregion

		#region Template overrides
		public override GraphicObject Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				_contentContainer.SetChild(value);
			}
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			_contentContainer = this.child.FindByName ("Content") as Container;
		}
		#endregion

		#region GraphicObject overrides
		[XmlAttributeAttribute()][DefaultValue(true)]//overiden to get default to true
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		#endregion

		[XmlAttributeAttribute()][DefaultValue("Groupbox")]
		public string Title {
			get { return title; } 
			set {
				if (title == value)
					return;
				title = value; 
				NotifyValueChanged ("Title", title);
			}
		}        
	}
}

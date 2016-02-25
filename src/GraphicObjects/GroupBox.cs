using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.GroupBox.style")]
	[DefaultTemplate("#Crow.Templates.GroupBox.goml")]
    public class GroupBox : TemplatedContainer
    {		
		string caption;
		Container _contentContainer;

		#region CTOR
		public GroupBox() : base(){}	
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

		[XmlAttributeAttribute()][DefaultValue("Groupbox")]
		public string Caption {
			get { return caption; } 
			set {
				if (caption == value)
					return;
				caption = value; 
				NotifyValueChanged ("Caption", caption);
			}
		}        
	}
}

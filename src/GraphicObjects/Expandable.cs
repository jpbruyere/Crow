using System;
using OpenTK.Input;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.Expandable.style")]
	[DefaultTemplate("#Crow.Templates.Expandable.goml")]
    public class Expandable : TemplatedContainer
    {
		#region CTOR
		public Expandable() : base()
		{
		}
		#endregion

		#region Private fields
		bool _isExpanded;
		string caption;
		string image;
		Container _contentContainer;
		#endregion

		#region Event Handlers
		public event EventHandler Expand;
		public event EventHandler Collapse;
		#endregion

		#region GraphicObject overrides
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsExpanded = !IsExpanded;
			base.onMouseClick (sender, e);
		}
		#endregion

		public override GraphicObject Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				_contentContainer.SetChild(value);
				NotifyValueChanged ("HasContent", HasContent);
			}
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			_contentContainer = this.child.FindByName ("Content") as Container;
		}
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}

		#region Public properties
		[XmlAttributeAttribute()][DefaultValue("Expandable")]
		public string Caption {
			get { return caption; } 
			set {
				if (caption == value)
					return;
				caption = value; 
				NotifyValueChanged ("Caption", caption);
			}
		}        
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.Icons.expandable.svg")]
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
        public bool IsExpanded
        {
			get { return _isExpanded; }
            set
            {
				if (value == _isExpanded)
					return;

				_isExpanded = value;

				if (!HasContent)
					_isExpanded = false;

				NotifyValueChanged ("IsExpanded", _isExpanded);

				if (_isExpanded)
					onExpand (this, null);
				else
					onCollapse (this, null);
            }
        }
		[XmlIgnore]public bool HasContent {
			get { return _contentContainer == null ? false : _contentContainer.Child != null; }
		}

		#endregion

		public virtual void onExpand(object sender, EventArgs e)
		{
			if (_contentContainer != null)
				_contentContainer.Visible = true;
			
			Expand.Raise (this, e);
		}
		public virtual void onCollapse(object sender, EventArgs e)
		{
			if (_contentContainer != null)
				_contentContainer.Visible = false;
			
			Collapse.Raise (this, e);
		}
	}
}

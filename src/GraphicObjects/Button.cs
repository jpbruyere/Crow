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
	[DefaultTemplate("#Crow.Templates.Button.crow")]
    public class Button : TemplatedContainer
    {
		string caption;
		string image;
		bool isPressed;
		Container _contentContainer;

		#region CTOR
        public Button() : base()
        {}
		#endregion

		public event EventHandler Pressed;
		public event EventHandler Released;
		public event EventHandler Clicked;

		#region TemplatedContainer overrides
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
		#endregion

		#region GraphicObject Overrides
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			IsPressed = true;

			base.onMouseDown (sender, e);

			//TODO:remove
			NotifyValueChanged ("State", "pressed");
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			IsPressed = false;

			base.onMouseUp (sender, e);

			//TODO:remove
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
		[XmlAttributeAttribute()][DefaultValue(false)]
		public bool IsPressed
		{
			get { return isPressed; }
			set
			{
				if (isPressed == value)
					return;

				isPressed = value;

				NotifyValueChanged ("IsPressed", isPressed);

				if (isPressed)
					Pressed.Raise (this, null);
				else
					Released.Raise (this, null);
			}
		}
	}
}

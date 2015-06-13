using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using OpenTK.Input;

namespace go
{
	[DefaultTemplate("#go.Templates.Window.goml")]
	public class Window : TemplatedContainer
	{
		Label _title;
		Image _icon;
		Container _contentContainer;

		public override GraphicObject Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				_contentContainer.SetChild(value);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Window")]
		public string Title {
			get { return _title.Text; } 
			set {
				if (_title == null)
					return;
				_title.Text = value; 
			}
		}   
		public Window () : base()
		{
		}

		void Window_MouseMove (object sender, OpenTK.Input.MouseMoveEventArgs e)
		{
			if (!e.Mouse.IsButtonDown (MouseButton.Left))
				return;
			this.TopContainer.redrawClip.AddRectangle (this.ScreenCoordinates(this.Slot));
			this.Left += e.XDelta;
			this.Top += e.YDelta;
			this.registerForGraphicUpdate ();			
		}

		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);
			_contentContainer = this.child.FindByName ("Content") as Container;
			_title = this.child.FindByName ("Title") as Label;
			_icon = this.child.FindByName ("Icon") as Image;
		}

		void butQuitPress (object sender, MouseButtonEventArgs e)
		{
			TopContainer.Quit ();
		}


	}
}


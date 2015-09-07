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


		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);


			OpenTKGameWindow otkgw = TopContainer as OpenTKGameWindow;

			if ((e.Position - this.Slot.TopLeft).Length < 5)
				otkgw.Cursor = XCursor.NW;
			else
				otkgw.Cursor = XCursor.Cross;


			if (TopContainer.activeWidget != this)
				return;
			
			this.TopContainer.redrawClip.AddRectangle (this.ScreenCoordinates(this.Slot));
			this.Left += e.XDelta;
			this.Top += e.YDelta;
			this.registerForGraphicUpdate ();			


		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			OpenTKGameWindow otkgw = TopContainer as OpenTKGameWindow;
			otkgw.Cursor = XCursor.Default;
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
			TopContainer.DeleteWidget (this);
		}


	}
}


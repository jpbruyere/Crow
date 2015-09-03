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
			
//			if ((e.Position - this.Slot.TopLeft).Length < 3)
//				System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNWSE;
//			else
//				System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
			
			if (!e.Mouse.IsButtonDown (MouseButton.Left))
				return;
			
			//

			System.Windows.Forms.Cursor.Show();// = System.Windows.Forms.Cursors.SizeAll;

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
			TopContainer.DeleteWidget (this);
		}


	}
}


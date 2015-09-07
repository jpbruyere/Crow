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
		enum Direction
		{
			None,
			N,
			S,
			E,
			W,
			NW,
			NE,
			SW,
			SE
		}
		Direction currentDirection = Direction.None;

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			OpenTKGameWindow otkgw = TopContainer as OpenTKGameWindow;

			int borderLim = 5;



			if (TopContainer.activeWidget == null) {
				Direction lastDir = currentDirection;

				if ((e.Position - this.Slot.TopLeft).Length < borderLim)
					currentDirection = Direction.NW;
				else if ((e.Position - this.Slot.TopRight).Length < borderLim)
					currentDirection = Direction.NE;
				else if ((e.Position - this.Slot.BottomLeft).Length < borderLim)
					currentDirection = Direction.SW;
				else if ((e.Position - this.Slot.BottomRight).Length < borderLim)
					currentDirection = Direction.SE;
				else
					currentDirection = Direction.None;


				if (currentDirection != lastDir) {
					switch (currentDirection) {
					case Direction.None:
						otkgw.Cursor = XCursor.Default;
						break;
					case Direction.N:
						otkgw.Cursor = XCursor.Cross;
						break;
					case Direction.S:
						otkgw.Cursor = XCursor.Cross;
						break;
					case Direction.E:
						otkgw.Cursor = XCursor.Cross;
						break;
					case Direction.W:
						otkgw.Cursor = XCursor.Cross;
						break;
					case Direction.NW:
						otkgw.Cursor = XCursor.NW;
						break;
					case Direction.NE:
						otkgw.Cursor = XCursor.NE;
						break;
					case Direction.SW:
						otkgw.Cursor = XCursor.SW;
						break;
					case Direction.SE:
						otkgw.Cursor = XCursor.SE;
						break;
					}
				}				
				return;
			}

			if (TopContainer.activeWidget != this)
				return;
				
			this.TopContainer.redrawClip.AddRectangle (this.ScreenCoordinates(this.Slot));

			switch (currentDirection) {
			case Direction.None:
				this.Left += e.XDelta;
				this.Top += e.YDelta;
				break;
			case Direction.N:
				break;
			case Direction.S:
				break;
			case Direction.E:
				break;
			case Direction.W:
				break;
			case Direction.NW:
				this.Left += e.XDelta;
				this.Top += e.YDelta;
				this.Width -= e.XDelta;
				this.Height -= e.YDelta;
				break;
			case Direction.NE:
				this.Width += e.XDelta;
				this.Top += e.YDelta;
				this.Height -= e.YDelta;
				break;
			case Direction.SW:
				this.Left += e.XDelta;
				this.Width -= e.XDelta;
				this.Height += e.YDelta;
				break;
			case Direction.SE:
				this.Width += e.XDelta;
				this.Height += e.YDelta;
				break;
			}		
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			currentDirection = Direction.None;
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


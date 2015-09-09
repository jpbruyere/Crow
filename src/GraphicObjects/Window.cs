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
			SE,
		}
		Direction currentDirection = Direction.None;

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			OpenTKGameWindow otkgw = TopContainer as OpenTKGameWindow;

			if (otkgw.activeWidget == null) {
				Direction lastDir = currentDirection;

				if (Math.Abs (e.Position.Y - this.Slot.Y) < Interface.BorderThreshold) {
					if (Math.Abs (e.Position.X - this.Slot.X) < Interface.BorderThreshold)
						currentDirection = Direction.NW;
					else if (Math.Abs (e.Position.X - this.Slot.Right) < Interface.BorderThreshold)
						currentDirection = Direction.NE;
					else
						currentDirection = Direction.N;
				} else if (Math.Abs (e.Position.Y - this.Slot.Bottom) < Interface.BorderThreshold) {
					if (Math.Abs (e.Position.X - this.Slot.X) < Interface.BorderThreshold)
						currentDirection = Direction.SW;
					else if (Math.Abs (e.Position.X - this.Slot.Right) < Interface.BorderThreshold)
						currentDirection = Direction.SE;
					else
						currentDirection = Direction.S;
				} else if (Math.Abs (e.Position.X - this.Slot.X) < Interface.BorderThreshold)
					currentDirection = Direction.W;
				else if (Math.Abs (e.Position.X - this.Slot.Right) < Interface.BorderThreshold)
					currentDirection = Direction.E;
				else
					currentDirection = Direction.None;

				if (currentDirection != lastDir) {
					switch (currentDirection) {
					case Direction.None:
						otkgw.Cursor = XCursor.Default;
						break;
					case Direction.N:
						otkgw.Cursor = XCursor.V;
						break;
					case Direction.S:
						otkgw.Cursor = XCursor.V;
						break;
					case Direction.E:
						otkgw.Cursor = XCursor.H;
						break;
					case Direction.W:
						otkgw.Cursor = XCursor.H;
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
				this.Top += e.YDelta;
				this.Height -= e.YDelta;
				break;
			case Direction.S:
				this.Height += e.YDelta;
				break;
			case Direction.W:
				this.Left += e.XDelta;
				this.Width -= e.XDelta;
				break;
			case Direction.E:
				this.Width += e.XDelta;
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


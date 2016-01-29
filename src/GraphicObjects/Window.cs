using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using OpenTK.Input;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.Window.goml")]
	public class Window : TemplatedContainer
	{
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

		string _title;
		string _icon;
		bool _resizable;
		Container _contentContainer;
		Direction currentDirection = Direction.None;

		#region CTOR
		public Window () : base() {
			
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
			get { return _title; } 
			set {
				_title = value;
				NotifyValueChanged ("Title", _title);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.Icons.tetra.png")]
		public string Icon {
			get { return _icon; } 
			set {
				_icon = value;
				NotifyValueChanged ("Icon", _icon);
			}
		} 
		[XmlAttributeAttribute()][DefaultValue(true)]
		public bool Resizable {
			get {
				return _resizable;
			}
			set {
				_resizable = value;
				NotifyValueChanged ("Resizable", _resizable);
			}
		}


		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			OpenTKGameWindow otkgw = TopContainer as OpenTKGameWindow;

			if (otkgw.activeWidget == null) {
				if (Resizable) {
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
				}
				return;
			}

			if (TopContainer.activeWidget != this)
				return;
				
			this.TopContainer.redrawClip.AddRectangle (this.ScreenCoordinates(this.Slot));

			int currentLeft = this.Left;
			int currentTop = this.Top;

			if (currentLeft == 0)
				currentLeft = this.Slot.Left;
			if (currentTop == 0)
				currentTop = this.Slot.Top;

			switch (currentDirection) {
			case Direction.None:
				this.Left = currentLeft + e.XDelta;				
				this.Top = currentTop + e.YDelta;
				break;
			case Direction.N:
				this.Top = currentTop + e.YDelta;
				this.Height -= e.YDelta;
				break;
			case Direction.S:
				this.Height += e.YDelta;
				break;
			case Direction.W:
				this.Left = currentLeft + e.XDelta;
				this.Width -= e.XDelta;
				break;
			case Direction.E:
				this.Width += e.XDelta;
				break;
			case Direction.NW:
				this.Left = currentLeft + e.XDelta;
				this.Top = currentTop + e.YDelta;
				this.Width -= e.XDelta;
				this.Height -= e.YDelta;
				break;
			case Direction.NE:
				this.Width += e.XDelta;
				this.Top = currentTop + e.YDelta;
				this.Height -= e.YDelta;
				break;
			case Direction.SW:
				this.Left = currentLeft + e.XDelta;
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
		}

		protected void butQuitPress (object sender, MouseButtonEventArgs e)
		{
			ILayoutable parent = (sender as GraphicObject).Parent;
			while(!(parent is Window))
				parent = parent.Parent;
			TopContainer.DeleteWidget (parent as GraphicObject);
		}

		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}
	}
}


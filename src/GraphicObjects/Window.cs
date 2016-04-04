using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.Window.style")]
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

		bool hoverBorder = false;

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			Interface otkgw = Interface.CurrentInterface;

			if (!hoverBorder) {
				currentDirection = Direction.None;
				Interface.CurrentInterface.MouseCursor = XCursor.Default;
				return;
			}
			
			if (e.Mouse.IsButtonDown (MouseButton.Left)) {				

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
				return;
			}
//			GraphicObject firstFocusableAncestor = otkgw.hoverWidget;
//			while (firstFocusableAncestor != this) {
//				if (firstFocusableAncestor == null)
//					return;
//				if (firstFocusableAncestor.Focusable)
//					return;
//				firstFocusableAncestor = firstFocusableAncestor.Parent as GraphicObject;
//			}
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
						otkgw.MouseCursor = XCursor.Default;
						break;
					case Direction.N:
						otkgw.MouseCursor = XCursor.V;
						break;
					case Direction.S:
						otkgw.MouseCursor = XCursor.V;
						break;
					case Direction.E:
						otkgw.MouseCursor = XCursor.H;
						break;
					case Direction.W:
						otkgw.MouseCursor = XCursor.H;
						break;
					case Direction.NW:
						otkgw.MouseCursor = XCursor.NW;
						break;
					case Direction.NE:
						otkgw.MouseCursor = XCursor.NE;
						break;
					case Direction.SW:
						otkgw.MouseCursor = XCursor.SW;
						break;
					case Direction.SE:
						otkgw.MouseCursor = XCursor.SE;
						break;
					}
				}				
			}				
		}
		public void onBorderMouseLeave (object sender, MouseMoveEventArgs e)
		{
			hoverBorder = false;
			currentDirection = Direction.None;
			Interface.CurrentInterface.MouseCursor = XCursor.Default;
		}
		public void onBorderMouseEnter (object sender, MouseMoveEventArgs e)
		{
			hoverBorder = true;
		}

		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);
			_contentContainer = this.child.FindByName ("Content") as Container;
		}

		protected void butQuitPress (object sender, MouseButtonEventArgs e)
		{
			Interface.CurrentInterface.MouseCursor = XCursor.Default;
			Interface.CurrentInterface.DeleteWidget (this);
		}

		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}
	}
}


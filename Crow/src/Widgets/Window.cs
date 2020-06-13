// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using Glfw;

namespace Crow
{
	public class Window : TemplatedContainer
	{
		public enum Direction
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

		string _icon;
		bool resizable;
		bool movable;
		bool modal;
		protected bool hoverBorder = false;
		bool alwaysOnTop = false;

		Rectangle savedBounds;
		bool _minimized = false;

		Direction currentDirection = Direction.None;

		#region Events
		public event EventHandler Closing;
		public event EventHandler Maximized;
		public event EventHandler Unmaximized;
		public event EventHandler Minimize;
		#endregion

		#region CTOR
		protected Window() {}
		public Window (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		Widget moveHandle, sizingHandle;


		#region TemplatedContainer overrides
		protected override void loadTemplate(Widget template = null)
		{
			base.loadTemplate (template);

			NotifyValueChanged ("ShowNormal", false);
			NotifyValueChanged ("ShowMinimize", true);
			NotifyValueChanged ("ShowMaximize", true);

			moveHandle = child?.FindByName ("MoveHandle");
			sizingHandle = child?.FindByName ("SizeHandle");

			if (sizingHandle == null)
				return;
			sizingHandle.MouseEnter += (arg1, arg2) => IFace.SetStickyMouse(5);
		}
		#endregion

		#region public properties
		[DefaultValue("#Crow.Icons.crow.svg")]
		public string Icon {
			get { return _icon; } 
			set {
				if (_icon == value)
					return;
				_icon = value;
				NotifyValueChangedAuto (_icon);
			}
		} 
		[DefaultValue(true)]
		public bool Resizable {
			get {
				return resizable;
			}
			set {
				if (resizable == value)
					return;
				resizable = value;
				NotifyValueChangedAuto (resizable);
			}
		}
		[DefaultValue(true)]
		public bool Movable {
			get {
				return movable;
			}
			set {
				if (movable == value)
					return;
				movable = value;
				NotifyValueChangedAuto (movable);
			}
		}
		[DefaultValue(false)]
		public bool Modal {
			get {
				return modal;
			}
			set {
				if (modal == value)
					return;
				modal = value;
				NotifyValueChangedAuto (modal);
			}
		}
		[DefaultValue(false)]
		public bool IsMinimized {
			get { return _minimized; }
			set{
				if (value == IsMinimized)
					return;

				_minimized = value;
				_contentContainer.Visible = !_minimized;

				NotifyValueChangedAuto (_minimized);
			}
		}
		[XmlIgnore]public bool IsMaximized {
			get { return Width == Measure.Stretched & Height == Measure.Stretched & !_minimized; }
		}
		[XmlIgnore]public bool IsNormal {
			get { return !(IsMaximized|_minimized); }
		}
		[DefaultValue(false)]
		public bool AlwaysOnTop {
			get {
				return modal ? true : alwaysOnTop;
			}
			set {
				if (alwaysOnTop == value)
					return;
				
				alwaysOnTop = value;

				if (AlwaysOnTop && Parent != null)
					IFace.PutOnTop (this);

				NotifyValueChangedAuto (AlwaysOnTop);
			}
		}
		#endregion

		/// <summary>
		/// Moves the and resize with the same function entry point, the direction give the kind of move or resize
		/// </summary>
		/// <param name="XDelta">mouse delta on the X axis</param>
		/// <param name="YDelta">mouse delta on the Y axis</param>
		/// <param name="currentDirection">Current Direction of the operation, none for moving, other value for resizing in the given direction</param>
		public void MoveAndResize (int XDelta, int YDelta, Direction currentDirection = (Direction)0) {
			int currentLeft = this.Left;
			int currentTop = this.Top;
			int currentWidth, currentHeight;

			if (currentLeft == 0) {
				currentLeft = this.Slot.Left;
				this.Left = currentLeft;
			}
			if (currentTop == 0) {
				currentTop = this.Slot.Top;
				this.Top = currentTop;
			}
			if (this.Width.IsFixed)
				currentWidth = this.Width;
			else
				currentWidth = this.Slot.Width;

			if (this.Height.IsFixed)
				currentHeight = this.Height;
			else
				currentHeight = this.Slot.Height;

			switch (currentDirection) {
			case Direction.None:
				this.Left = currentLeft + XDelta;				
				this.Top = currentTop + YDelta;
				break;
			case Direction.N:
				this.Height = currentHeight - YDelta;
				if (this.Height == currentHeight - YDelta)
					this.Top = currentTop + YDelta;
				break;
			case Direction.S:
				this.Height = currentHeight + YDelta;
				break;
			case Direction.W:
				this.Width = currentWidth - XDelta;
				if (this.Width == currentWidth - XDelta)
					this.Left = currentLeft + XDelta;
				break;
			case Direction.E:
				this.Width = currentWidth + XDelta;
				break;
			case Direction.NW:
				this.Height = currentHeight - YDelta;
				if (this.Height == currentHeight - YDelta)
					this.Top = currentTop + YDelta;
				this.Width = currentWidth - XDelta;
				if (this.Width == currentWidth - XDelta)
					this.Left = currentLeft + XDelta;
				break;
			case Direction.NE:
				this.Height = currentHeight - YDelta;
				if (this.Height == currentHeight - YDelta)
					this.Top = currentTop + YDelta;
				this.Width = currentWidth + XDelta;
				break;
			case Direction.SW:
				this.Width = currentWidth - XDelta;
				if (this.Width == currentWidth - XDelta)
					this.Left = currentLeft + XDelta;
				this.Height = currentHeight + YDelta;
				break;
			case Direction.SE:
				this.Height = currentHeight + YDelta;
				this.Width = currentWidth + XDelta;
				break;
			}			
		}

		bool maySize => sizingHandle == null ? false : resizable & sizingHandle.IsHover;
		bool mayMove => moveHandle == null ? false : movable & moveHandle.IsHover;

		#region GraphicObject Overrides
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			Interface otkgw = IFace;

			if (maySize || mayMove) {
				if (grabMouse) {
					MoveAndResize (e.XDelta, e.YDelta, currentDirection);
					return;
				}
			}else
				return;

			Point m = Parent is Widget ? (Parent as Widget).ScreenPointToLocal (e.Position) : e.Position;

			if (maySize) {
				if (Math.Abs (m.Y - this.Slot.Y) < Interface.BorderThreshold) {
					if (Math.Abs (m.X - this.Slot.X) < Interface.BorderThreshold)
						currentDirection = Direction.NW;
					else if (Math.Abs (m.X - this.Slot.Right) < Interface.BorderThreshold)
						currentDirection = Direction.NE;
					else
						currentDirection = Direction.N;
				} else if (Math.Abs (m.Y - this.Slot.Bottom) < Interface.BorderThreshold) {
					if (Math.Abs (m.X - this.Slot.X) < Interface.BorderThreshold)
						currentDirection = Direction.SW;
					else if (Math.Abs (m.X - this.Slot.Right) < Interface.BorderThreshold)
						currentDirection = Direction.SE;
					else
						currentDirection = Direction.S;
				} else if (Math.Abs (m.X - this.Slot.X) < Interface.BorderThreshold)
					currentDirection = Direction.W;
				else if (Math.Abs (m.X - this.Slot.Right) < Interface.BorderThreshold)
					currentDirection = Direction.E;
			} else if (mayMove)
				currentDirection = Direction.None;
			else
				return;

			switch (currentDirection) {
			case Direction.None:
				otkgw.MouseCursor = MouseCursor.move;
				break;
			case Direction.N:
				otkgw.MouseCursor = MouseCursor.top_side;
				break;
			case Direction.S:
				otkgw.MouseCursor = MouseCursor.bottom_side;
				break;
			case Direction.E:
				otkgw.MouseCursor = MouseCursor.right_side;
				break;
			case Direction.W:
				otkgw.MouseCursor = MouseCursor.left_side;
				break;
			case Direction.NW:
				otkgw.MouseCursor = MouseCursor.top_left_corner;
				break;
			case Direction.NE:
				otkgw.MouseCursor = MouseCursor.top_right_corner;
				break;
			case Direction.SW:
				otkgw.MouseCursor = MouseCursor.bottom_left_corner;
				break;
			case Direction.SE:
				otkgw.MouseCursor = MouseCursor.bottom_right_corner;
				break;
			}
							
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			currentDirection = Direction.None;
			IFace.MouseCursor = MouseCursor.top_left_arrow;
		}
		bool grabMouse;
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			grabMouse = true;
			e.Handled = true;
			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			grabMouse = false;
			e.Handled = true;
			base.onMouseUp (sender, e);
		}
		public override bool MouseIsIn (Point m)
		{
			return modal ? true : base.MouseIsIn (m);
		}
		#endregion

		protected void onMaximized (object sender, EventArgs e){
			lock (IFace.LayoutMutex) {
				if (!IsMinimized)
					savedBounds = this.LastPaintedSlot;
				Left = Top = 0;
				RegisterForLayouting (LayoutingType.Positioning);
				Width = Height = Measure.Stretched;
				IsMinimized = false;
				Resizable = false;
				NotifyValueChanged ("ShowNormal", true);
				NotifyValueChanged ("ShowMinimize", true);
				NotifyValueChanged ("ShowMaximize", false);
			}

			Maximized.Raise (sender, e);
		}
		protected void onUnmaximized (object sender, EventArgs e){
			lock (IFace.LayoutMutex) {
				Left = savedBounds.Left;
				Top = savedBounds.Top;
				Width = savedBounds.Width;
				Height = savedBounds.Height;
				IsMinimized = false;
				Resizable = true;
				NotifyValueChanged ("ShowNormal", false);
				NotifyValueChanged ("ShowMinimize", true);
				NotifyValueChanged ("ShowMaximize", true);
			}

			Unmaximized.Raise (sender, e);
		}
		protected void onMinimized (object sender, EventArgs e){
			lock (IFace.LayoutMutex) {
				if (IsNormal)
					savedBounds = this.LastPaintedSlot;
				Width = 200;
				Height = 20;
				Resizable = false;
				IsMinimized = true;
				NotifyValueChanged ("ShowNormal", true);
				NotifyValueChanged ("ShowMinimize", false);
				NotifyValueChanged ("ShowMaximize", true);
			}

			Minimize.Raise (sender, e);
		}

		protected void butQuitPress (object sender, MouseButtonEventArgs e)
		{
			IFace.MouseCursor = MouseCursor.top_left_arrow;
			close ();
		}

		protected virtual void close(){
			Closing.Raise (this, null);
			if (Parent is Interface)
				(Parent as Interface).DeleteWidget (this);
			else {
				Widget p = Parent as Widget;
				if (p is Group g) {
					lock (IFace.UpdateMutex) {
						RegisterClip (p.ScreenCoordinates (p.LastPaintedSlot));
						g.DeleteChild (this);
					}
					//(Parent as Group).RegisterForRedraw ();
				} else if (Parent is Container c)
					c.Child = null;
			}
		}

		public static Window Show (Interface iface, string imlPath, bool modal = false){
			lock (iface.UpdateMutex) {
				Window w = iface.Load (imlPath) as Window;
				w.Modal = modal;
				return w;
			}
		}

	}
}


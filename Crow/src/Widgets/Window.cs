// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;
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
		Fill titleBarBackground = Color.SteelBlue;
		Fill titleBarForeground = Color.White;

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
		protected Window() : base(){}
		public Window (Interface iface) : base(iface){}
		#endregion

		#region TemplatedContainer overrides
		protected override void loadTemplate(Widget template = null)
		{
			base.loadTemplate (template);

			NotifyValueChanged ("ShowNormal", false);
			NotifyValueChanged ("ShowMinimize", true);
			NotifyValueChanged ("ShowMaximize", true);
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
				NotifyValueChanged ("Icon", _icon);
			}
		} 
		/// <summary>
		/// Background of the title bar if any.
		/// </summary>
		[DefaultValue("vgradient|0:Onyx|1:SteelBlue")]
		public virtual Fill TitleBarBackground {
			get { return titleBarBackground; }
			set {
				if (titleBarBackground == value)
					return;
				titleBarBackground = value;
				NotifyValueChanged ("TitleBarBackground", titleBarBackground);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Foreground of the title bar, usualy used for the window caption color.
		/// </summary>
		[DefaultValue("White")]
		public virtual Fill TitleBarForeground {
			get { return titleBarForeground; }
			set {
				if (titleBarForeground == value)
					return;
				titleBarForeground = value;
				NotifyValueChanged ("TitleBarForeground", titleBarForeground);
				RegisterForRedraw ();
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
				NotifyValueChanged ("Resizable", resizable);
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
				NotifyValueChanged ("Movable", movable);
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
				NotifyValueChanged ("Modal", modal);
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

				NotifyValueChanged ("IsMinimized", _minimized);
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

				NotifyValueChanged ("AlwaysOnTop", AlwaysOnTop);
			}
		}
//		[DefaultValue(WindowState.Normal)]
//		public virtual WindowState State {
//			get { return _state; }
//			set {
//				if (_state == value)
//					return;
//				_state = value;
//				NotifyValueChanged ("State", _state);
//				NotifyValueChanged ("IsNormal", IsNormal);
//				NotifyValueChanged ("IsMaximized", IsMaximized);
//				NotifyValueChanged ("IsMinimized", IsMinimized);
//				NotifyValueChanged ("IsNotMinimized", IsNotMinimized);
//			}
//		} 
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

		#region GraphicObject Overrides
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			Interface otkgw = IFace;

			if (HasFocus) {
				if (movable) {
					if (IFace.IsDown (MouseButton.Left)) {
						MoveAndResize (e.XDelta, e.YDelta, currentDirection);
						return;
					}
				}
			} else {
				currentDirection = Direction.None;
				return;
			}


			Point m = Parent is Widget ? (Parent as Widget).ScreenPointToLocal (e.Position) : e.Position;

			if (Resizable) {
				Direction lastDir = currentDirection;

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
				else
					currentDirection = Direction.None;

				//if (currentDirection != lastDir) {
					switch (currentDirection) {
					case Direction.None:
						otkgw.MouseCursor = MouseCursor.Move;
						break;
					case Direction.N:
						otkgw.MouseCursor = MouseCursor.Top;
						break;
					case Direction.S:
						otkgw.MouseCursor = MouseCursor.Bottom;
						break;
					case Direction.E:
						otkgw.MouseCursor = MouseCursor.Right;
						break;
					case Direction.W:
						otkgw.MouseCursor = MouseCursor.Left;
						break;
					case Direction.NW:
						otkgw.MouseCursor = MouseCursor.TopLeft;
						break;
					case Direction.NE:
						otkgw.MouseCursor = MouseCursor.TopRight;
						break;
					case Direction.SW:
						otkgw.MouseCursor = MouseCursor.BottomLeft;
						break;
					case Direction.SE:
						otkgw.MouseCursor = MouseCursor.BottomRight;
						break;
					}
				//}				
			}				
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			currentDirection = Direction.None;
			IFace.MouseCursor = MouseCursor.Arrow;
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
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
				this.Left = this.Top = 0;
				this.RegisterForLayouting (LayoutingType.Positioning);
				this.Width = this.Height = Measure.Stretched;
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
				this.Left = savedBounds.Left;
				this.Top = savedBounds.Top;
				this.Width = savedBounds.Width;
				this.Height = savedBounds.Height;
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
			IFace.MouseCursor = MouseCursor.Arrow;
			close ();
		}

		protected virtual void close(){
			Closing.Raise (this, null);
			if (Parent is Interface)
				(Parent as Interface).DeleteWidget (this);
			else {
				Widget p = Parent as Widget;
				if (p is Group) {
					lock (IFace.UpdateMutex) {
						RegisterClip (p.ScreenCoordinates (p.LastPaintedSlot));
						(p as Group).DeleteChild (this);
					}
					//(Parent as Group).RegisterForRedraw ();
				} else if (Parent is PrivateContainer)
					(Parent as Container).Child = null;
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


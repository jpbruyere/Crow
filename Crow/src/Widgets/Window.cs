// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
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
		[Flags]
		public enum Status {
			None = 0x00,
			Normal = 0x01,
			Minimized = 0x02,
			Maximized= 0x04
		}
		string _icon;
		bool resizable;
		bool movable;
		bool modal;
		bool alwaysOnTop = false;

		Rectangle savedBounds;
		bool wasResizable;

		Status currentState, allowedStates;

		protected Direction currentDirection = Direction.None;


		#region Events
		public event EventHandler Closing;
		public event EventHandler Maximized;
		public event EventHandler Unmaximized;
		public event EventHandler Minimize;
		#endregion
		[DefaultValue("Normal|Minimized|Maximized")]
		public Status AllowedStates {
			get => allowedStates;
			set {
				if (allowedStates == value)
					return;
				allowedStates = value;
				NotifyValueChangedAuto (allowedStates);

				if (currentState == Status.None)
					return;

				initCommands();

				if (allowedStates.HasFlag (currentState)) {
					CMDNormalize.CanExecute = currentState != Status.Normal & allowedStates.HasFlag (Status.Normal);
					CMDMinimize.CanExecute = currentState != Status.Minimized & allowedStates.HasFlag (Status.Minimized);
					CMDMaximize.CanExecute = currentState != Status.Maximized & allowedStates.HasFlag (Status.Maximized);
				} else {
					if (allowedStates.HasFlag (Status.Normal))
						CurrentState = Status.Normal;
					else if (allowedStates.HasFlag (Status.Maximized))
						CurrentState = Status.Maximized;
					else
						CurrentState = Status.Minimized;
				}
			}
		}
		[DefaultValue("Normal")]
		public Status CurrentState {
			get => currentState;
			set {
				Status newState = value;
				if (!allowedStates.HasFlag (newState)) {
					if (allowedStates.HasFlag (Status.Normal))
						newState = Status.Normal;
					else if (allowedStates.HasFlag (Status.Maximized))
						newState = Status.Maximized;
					else
						newState = Status.Minimized;
				}

				if (currentState == newState)
					return;

				if (currentState == Status.Normal) {
					savedBounds = LastPaintedSlot;
					wasResizable = Resizable;
				}

				currentState = value;
				NotifyValueChangedAuto (currentState);

				initCommands();

				switch (currentState) {
				case Status.Maximized:
					Left = Top = 0;
					RegisterForLayouting (LayoutingType.Positioning);
					Width = Height = Measure.Stretched;
					Resizable = false;
					CMDNormalize.CanExecute = allowedStates.HasFlag (Status.Normal);
					CMDMinimize.CanExecute = allowedStates.HasFlag (Status.Minimized);
					CMDMaximize.CanExecute = false;
					break;
				case Status.Minimized:
					Width = 200;
					Height = 20;
					Resizable = false;
					CMDNormalize.CanExecute = allowedStates.HasFlag (Status.Normal);
					CMDMinimize.CanExecute = false;
					CMDMaximize.CanExecute = allowedStates.HasFlag (Status.Maximized);
					break;
				case Status.Normal:
					Left = savedBounds.Left;
					Top = savedBounds.Top;
					Width = savedBounds.Width;
					Height = savedBounds.Height;
					Resizable = wasResizable;
					CMDNormalize.CanExecute = false;
					CMDMinimize.CanExecute = allowedStates.HasFlag (Status.Minimized);
					CMDMaximize.CanExecute = allowedStates.HasFlag (Status.Maximized);
					break;
				}
			}
		}

		#region CTOR
		protected Window() {}
		public Window (Interface iface, string style = null) : base (iface, style) {}
		#endregion

		Widget moveHandle, sizingHandle;

		public ActionCommand CMDMinimize, CMDMaximize, CMDNormalize, CMDClose;
		CommandGroup commands;

		public CommandGroup Commands {
			get => commands;
			set {
				if (commands == value)
					return;
				commands = value;
				NotifyValueChangedAuto (commands);
			}
		}
		/*public Command CMDMinimize = new Command ("Minimize", (sender) =>
			{(sender as Window).CurrentState = Status.Minimized;}, "#Crow.Icons.minimize.svg", false);
		public Command CMDMaximize = new Command ("Maximize", (sender) =>
			{(sender as Window).CurrentState = Status.Maximized;}, "#Crow.Icons.maximize.svg", false);
		public Command CMDNormalize = new Command ("Normalize", (sender) =>
			{(sender as Window).CurrentState = Status.Normal;}, "#Crow.Icons.normalize.svg", false);
		public Command CMDClose = new Command ("Close", (sender) =>
			{(sender as Window).close ();}, "#Crow.Icons.exit2.svg", true);*/


		void initCommands () {
			if (CMDMinimize != null)
				return;
			CMDMinimize = new ActionCommand ("Minimize", () => CurrentState = Status.Minimized, "#Crow.Icons.minimize.svg", allowedStates.HasFlag (Status.Minimized));
			CMDMaximize = new ActionCommand ("Maximize", () => CurrentState = Status.Maximized, "#Crow.Icons.maximize.svg", allowedStates.HasFlag (Status.Maximized));
			CMDNormalize = new ActionCommand ("Normalize", () => CurrentState = Status.Normal, "#Crow.Icons.normalize.svg", false);
			CMDClose = new ActionCommand ("Close", close, "#Crow.Icons.exit2.svg", true);
			Commands = new CommandGroup(CMDMinimize, CMDNormalize, CMDMaximize, CMDClose);
		}



		#region TemplatedContainer overrides
		protected override void loadTemplate(Widget template = null)
		{
			initCommands ();

			base.loadTemplate (template);

			initHandles ();
		}

		protected void initHandles () {
			moveHandle = child?.FindByName ("MoveHandle");
			sizingHandle = child?.FindByName ("SizeHandle");

			if (sizingHandle != null) {
				sizingHandle.Unhover += SizingHandle_Unhover;
				sizingHandle.MouseDown += setResizeOn;
				sizingHandle.MouseUp += setResizeOff;
			}

			if (moveHandle != null) {
				moveHandle.MouseDown += setMoveOn;
				moveHandle.MouseUp += setMoveOff;
			}
		}
		protected void resetHandles () {
			if (sizingHandle != null) {
				sizingHandle.Unhover -= SizingHandle_Unhover;
				sizingHandle.MouseDown -= setResizeOn;
				sizingHandle.MouseUp -= setResizeOff;
			}

			if (moveHandle != null) {
				moveHandle.MouseDown -= setMoveOn;
				moveHandle.MouseUp -= setMoveOff;
			}
			moveHandle = null;
			sizingHandle = null;
		}
		void setResizeOn (object o, EventArgs e) => resize = true;
		void setResizeOff (object o, EventArgs e) => resize = false;
		void setMoveOn (object o, EventArgs e) => move = true;
		void setMoveOff (object o, EventArgs e) => move = false;


		bool move, resize;

		void SizingHandle_Unhover (object sender, EventArgs e) {
			currentDirection = Direction.None;
			NotifyValueChanged ("CurDir", currentDirection);
		}
		#endregion

		#region public properties
		[DefaultValue("#Crow.Icons.crow.svg")]
		public string Icon {
			get => _icon;
			set {
				if (_icon == value)
					return;
				_icon = value;
				NotifyValueChangedAuto (_icon);
			}
		}
		[DefaultValue(true)]
		public bool Resizable {
			get => resizable;
			set {
				if (resizable == value)
					return;
				resizable = value;
				NotifyValueChangedAuto (resizable);
			}
		}
		[DefaultValue(true)]
		public bool Movable {
			get => movable;
			set {
				if (movable == value)
					return;
				movable = value;
				NotifyValueChangedAuto (movable);
			}
		}
		[DefaultValue(false)]
		public bool Modal {
			get => modal;
			set {
				if (modal == value)
					return;
				modal = value;
				NotifyValueChangedAuto (modal);
			}
		}
		[DefaultValue(false)]
		public bool AlwaysOnTop {
			get => modal ? true : alwaysOnTop;
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
		protected void moveAndResize (int XDelta, int YDelta, Direction currentDirection = (Direction)0) {
			//lock (IFace.UpdateMutex) {
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

				if (move) {
					this.Left = currentLeft + XDelta;
					this.Top = currentTop + YDelta;
					return;
				}

				if (!resize)
					return;

				switch (currentDirection) {
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
			//}
		}

		bool maySize => sizingHandle != null && resizable && sizingHandle.IsHover;
		bool mayMove => moveHandle != null && movable;

		#region Widget Overrides
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!(mayMove || maySize))
				return;

			if (move || resize) {
				moveAndResize (e.XDelta, e.YDelta, currentDirection);
				return;
			}

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
			/*case Direction.None:
				IFace.MouseCursor = MouseCursor.move;
				break;*/
			case Direction.N:
				IFace.MouseCursor = MouseCursor.top_side;
				break;
			case Direction.S:
				IFace.MouseCursor = MouseCursor.bottom_side;
				break;
			case Direction.E:
				IFace.MouseCursor = MouseCursor.right_side;
				break;
			case Direction.W:
				IFace.MouseCursor = MouseCursor.left_side;
				break;
			case Direction.NW:
				IFace.MouseCursor = MouseCursor.top_left_corner;
				break;
			case Direction.NE:
				IFace.MouseCursor = MouseCursor.top_right_corner;
				break;
			case Direction.SW:
				IFace.MouseCursor = MouseCursor.bottom_left_corner;
				break;
			case Direction.SE:
				IFace.MouseCursor = MouseCursor.bottom_right_corner;
				break;
			}
			NotifyValueChanged ("CurDir", currentDirection);
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			currentDirection = Direction.None;
			IFace.MouseCursor = MouseCursor.top_left_arrow;
			NotifyValueChanged ("CurDir", currentDirection);
		}
		public override bool MouseIsIn (Point m)
		{
			return modal ? true : base.MouseIsIn (m);
		}
		#endregion

		/*protected void onMaximized (){
			lock (IFace.LayoutMutex) {
				NotifyValueChanged ("ShowNormal", true);
				NotifyValueChanged ("ShowMinimize", true);
				NotifyValueChanged ("ShowMaximize", false);
			}

			Maximized.Raise (this, null);
		}
		protected void onUnmaximized (){
			lock (IFace.LayoutMutex) {
				NotifyValueChanged ("ShowNormal", false);
				NotifyValueChanged ("ShowMinimize", true);
				NotifyValueChanged ("ShowMaximize", true);
			}

			Unmaximized.Raise (this, null);
		}
		protected void onMinimized (){
			lock (IFace.LayoutMutex) {
				if (IsNormal)

			}

			Minimize.Raise (this, null);
		}*/

		public void onQuitPress (object sender, MouseButtonEventArgs e)
		{
			IFace.MouseCursor = MouseCursor.top_left_arrow;
			close ();
		}

		protected virtual void close(){
			Closing.Raise (this, null);
			if (Parent is Interface)
				(Parent as Interface).DeleteWidget (this);
			else {
				lock (IFace.UpdateMutex) {
					Widget p = Parent as Widget;
					if (p is Group g) {
							RegisterClip (p.ScreenCoordinates (p.LastPaintedSlot));
							g.DeleteChild (this);
						//(Parent as Group).RegisterForRedraw ();
					} else if (Parent is Container c)
						c.Child = null;
				}
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


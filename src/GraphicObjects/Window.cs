//
//  Window.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;

namespace Crow
{
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
		bool _movable;
		bool hoverBorder = false;

		Rectangle savedBounds;
		bool _minimized = false;

		Container _contentContainer;
		Direction currentDirection = Direction.None;

		#region Events
		public event EventHandler Closing;
		public event EventHandler Maximized;
		public event EventHandler Unmaximized;
		public event EventHandler Minimize;
		#endregion

		#region CTOR
		public Window () : base() {
			
		}
		#endregion

		#region TemplatedContainer overrides
		public override GraphicObject Content {
			get { return _contentContainer == null ? null : _contentContainer.Child; }
			set { _contentContainer.SetChild(value); }
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);
			_contentContainer = this.child.FindByName ("Content") as Container;

			NotifyValueChanged ("ShowNormal", false);
			NotifyValueChanged ("ShowMinimize", true);
			NotifyValueChanged ("ShowMaximize", true);
		}
		#endregion

		#region public properties
		[XmlAttributeAttribute][DefaultValue("Window")]
		public string Title {
			get { return _title; } 
			set {
				if (_title == value)
					return;
				_title = value;
				NotifyValueChanged ("Title", _title);
			}
		}
		[XmlAttributeAttribute][DefaultValue("#Crow.Images.Icons.crow.png")]
		public string Icon {
			get { return _icon; } 
			set {
				if (_icon == value)
					return;
				_icon = value;
				NotifyValueChanged ("Icon", _icon);
			}
		} 
		[XmlAttributeAttribute][DefaultValue(true)]
		public bool Resizable {
			get {
				return _resizable;
			}
			set {
				if (_resizable == value)
					return;
				_resizable = value;
				NotifyValueChanged ("Resizable", _resizable);
			}
		}
		[XmlAttributeAttribute][DefaultValue(true)]
		public bool Movable {
			get {
				return _movable;
			}
			set {
				if (_movable == value)
					return;
				_movable = value;
				NotifyValueChanged ("Movable", _movable);
			}
		}
		[XmlAttributeAttribute][DefaultValue(false)]
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
//		[XmlAttributeAttribute()][DefaultValue(WindowState.Normal)]
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

		#region GraphicObject Overrides
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			Interface otkgw = CurrentInterface;

			if (!hoverBorder) {
				currentDirection = Direction.None;
				CurrentInterface.MouseCursor = XCursor.Default;
				return;
			}

			if (this.HasFocus && _movable) {
				if (e.Mouse.IsButtonDown (MouseButton.Left)) {
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
						this.Left = currentLeft + e.XDelta;				
						this.Top = currentTop + e.YDelta;
						break;
					case Direction.N:
						this.Height = currentHeight - e.YDelta;
						if (this.Height == currentHeight - e.YDelta)
							this.Top = currentTop + e.YDelta;
						break;
					case Direction.S:
						this.Height = currentHeight + e.YDelta;
						break;
					case Direction.W:
						this.Width = currentWidth - e.XDelta;
						if (this.Width == currentWidth - e.XDelta)
							this.Left = currentLeft + e.XDelta;
						break;
					case Direction.E:
						this.Width = currentWidth + e.XDelta;
						break;
					case Direction.NW:
						this.Height = currentHeight - e.YDelta;
						if (this.Height == currentHeight - e.YDelta)
							this.Top = currentTop + e.YDelta;
						this.Width = currentWidth - e.XDelta;
						if (this.Width == currentWidth - e.XDelta)
							this.Left = currentLeft + e.XDelta;
						break;
					case Direction.NE:
						this.Height = currentHeight - e.YDelta;
						if (this.Height == currentHeight - e.YDelta)
							this.Top = currentTop + e.YDelta;
						this.Width = currentWidth + e.XDelta;
						break;
					case Direction.SW:
						this.Width = currentWidth - e.XDelta;
						if (this.Width == currentWidth - e.XDelta)
							this.Left = currentLeft + e.XDelta;
						this.Height = currentHeight + e.YDelta;
						break;
					case Direction.SE:
						this.Height = currentHeight + e.YDelta;
						this.Width = currentWidth + e.XDelta;
						break;
					}
					return;
				}
			}
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
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
		}
		#endregion

		protected void onMaximized (object sender, EventArgs e){
			lock (CurrentInterface.LayoutMutex) {
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
			lock (CurrentInterface.LayoutMutex) {
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
			lock (CurrentInterface.LayoutMutex) {
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
		protected void onBorderMouseLeave (object sender, MouseMoveEventArgs e)
		{
			hoverBorder = false;
			currentDirection = Direction.None;
			CurrentInterface.MouseCursor = XCursor.Default;
		}
		protected void onBorderMouseEnter (object sender, MouseMoveEventArgs e)
		{
			hoverBorder = true;
		}


		protected void butQuitPress (object sender, MouseButtonEventArgs e)
		{
			CurrentInterface.MouseCursor = XCursor.Default;
			close ();
		}

		void close(){
			Closing.Raise (this, null);
			CurrentInterface.DeleteWidget (this);
		}
	}
}


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
		bool hoverBorder = false;

		Container _contentContainer;
		Direction currentDirection = Direction.None;

		public event EventHandler Closing;

		#region CTOR
		public Window () : base() {
			
		}
		#endregion

		#region TemplatedContainer overrides
		public override GraphicObject Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				_contentContainer.SetChild(value);
			}
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);
			_contentContainer = this.child.FindByName ("Content") as Container;
		}
		#endregion

		[XmlAttributeAttribute()][DefaultValue("Window")]
		public string Title {
			get { return _title; } 
			set {
				_title = value;
				NotifyValueChanged ("Title", _title);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.Icons.crow.png")]
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

		#region GraphicObject Overrides
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			Interface otkgw = Interface.CurrentInterface;

			if (!hoverBorder) {
				currentDirection = Direction.None;
				Interface.CurrentInterface.MouseCursor = XCursor.Default;
				return;
			}

			if (this.HasFocus) {
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
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
		}
		#endregion

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


		protected void butQuitPress (object sender, MouseButtonEventArgs e)
		{
			Interface.CurrentInterface.MouseCursor = XCursor.Default;
			close ();
		}

		void close(){
			Closing.Raise (this, null);
			Interface.CurrentInterface.DeleteWidget (this);
		}
	}
}


// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using Glfw;

namespace Crow
{
	/// <summary>
	/// generic class to build scrolling control in both directions
	/// </summary>
	[DesignIgnore]
	public class ScrollingObject : Widget
	{
		#region CTOR
		protected ScrollingObject () {}
		public ScrollingObject (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		int scrollX, scrollY, maxScrollX, maxScrollY, mouseWheelSpeed;

		/// <summary>
		/// if true, key stroke are handled in derrived class
		/// </summary>
		protected bool KeyEventsOverrides = false;

		/// <summary> Horizontal Scrolling Position </summary>
		[DefaultValue(0)]
		public virtual int ScrollX {
			get { return scrollX; }
			set {
				if (scrollX == value)
					return;

				int newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScrollX)
					newS = maxScrollX;

				if (newS == scrollX)
					return;

				scrollX = newS;

				NotifyValueChangedAuto (scrollX);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Vertical Scrolling Position </summary>
		[DefaultValue(0)]
		public virtual int ScrollY {
			get { return scrollY; }
			set {
				if (scrollY == value)
					return;

				int newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScrollY)
					newS = maxScrollY;

				if (newS == scrollY)
					return;

				scrollY = newS;

				NotifyValueChangedAuto (scrollY);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Horizontal Scrolling maximum value </summary>
		[DefaultValue(0)]
		public virtual int MaxScrollX {
			get { return maxScrollX; }
			set {
				if (maxScrollX == value)
					return;

				maxScrollX = Math.Max(0, value);

				if (scrollX > maxScrollX)
					ScrollX = maxScrollX;

				NotifyValueChangedAuto (maxScrollX);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Vertical Scrolling maximum value </summary>
		[DefaultValue(0)]
		public virtual int MaxScrollY {
			get { return maxScrollY; }
			set {
				if (maxScrollY == value)
					return;

				maxScrollY = Math.Max (0, value);

				if (scrollY > maxScrollY)
					ScrollY = maxScrollY;

				NotifyValueChangedAuto (maxScrollY);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Mouse Wheel Scrolling multiplier </summary>
		[DefaultValue(1)]
		public virtual int MouseWheelSpeed {
			get { return mouseWheelSpeed; }
			set {
				if (mouseWheelSpeed == value)
					return;
				
				mouseWheelSpeed = value;

				NotifyValueChangedAuto (mouseWheelSpeed);
			}
		}

		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{
			if (IFace.Shift)
				ScrollX += e.Delta * MouseWheelSpeed;
			else
				ScrollY -= e.Delta * MouseWheelSpeed;
			e.Handled = true;
			base.onMouseWheel (sender, e);
		}
		/// <summary> Process scrolling with arrow keys, home and end keys. </summary>
		public override void onKeyDown (object sender, KeyEventArgs e)
		{
			base.onKeyDown (sender, e);

			if (KeyEventsOverrides)
				return;

			switch (e.Key) {
			case Key.Up:
				ScrollY--;
				break;
			case Key.Down:
				ScrollY++;
				break;
			case Key.Left:
				ScrollX--;
				break;
			case Key.Right:
				ScrollX++;
				break;
			case Key.Home:
				ScrollX = 0;
				ScrollY = 0;
				break;
			case Key.End:
				ScrollX = MaxScrollX;
				ScrollY = MaxScrollY;
				break;
			}
		}
	}
}


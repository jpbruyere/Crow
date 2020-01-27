﻿// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using Crow.Cairo;

namespace Crow
{
	/// <summary>
	/// scrolling surface, to be contained in a smaller container in which it will be scrolled
	/// </summary>
	public class Scroller : Container
	{
		#region CTOR
		protected Scroller () : base(){}
		public Scroller (Interface iface) : base(iface){}
		#endregion

		//public event EventHandler<ScrollingEventArgs> Scrolled;

		int scrollX, scrollY, maxScrollX, maxScrollY, scrollSpeed;

		/// <summary>
		/// if true, key stroke are handled in derrived class
		/// </summary>
		protected bool KeyEventsOverrides = false;

		#region public properties
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

				NotifyValueChanged ("ScrollX", scrollX);
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

				NotifyValueChanged ("ScrollY", scrollY);
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

				if (value < 0)
					maxScrollX = 0;
				else
					maxScrollX = value;

				if (scrollX > maxScrollX)
					ScrollX = maxScrollX;

				NotifyValueChanged ("MaxScrollX", maxScrollX);
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

				if (value < 0)
					maxScrollY = 0;
				else
					maxScrollY = value;


				if (scrollY > maxScrollY)
					ScrollY = maxScrollY;

				NotifyValueChanged ("MaxScrollY", maxScrollY);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Mouse Wheel Scrolling multiplier </summary>
		[DefaultValue(50)]
		public virtual int ScrollSpeed {
			get { return scrollSpeed; }
			set {
				if (scrollSpeed == value)
					return;

				scrollSpeed = value;

				NotifyValueChanged ("ScrollSpeed", scrollSpeed);
			}
		}
		#endregion

		public override void SetChild (Widget _child)
		{
			Group g = child as Group;
			if (g != null)
				g.ChildrenCleared -= onChildListCleared;
			
			base.SetChild (_child);

			g = _child as Group;
			if (g != null)
				g.ChildrenCleared += onChildListCleared;			
		}
		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			base.OnChildLayoutChanges (sender, arg);
			updateMaxScroll (arg.LayoutType);
		}


		#region GraphicObject Overrides
		public override Rectangle ScreenCoordinates (Rectangle r)
		{
			return base.ScreenCoordinates (r) - new Point((int)ScrollX,(int)ScrollY);
		}
		public override bool PointIsIn (ref Point m)
		{
			if (!base.PointIsIn(ref m))
				return false;
			if (!Slot.ContainsOrIsEqual (m) || child==null)
				return false;
			m += new Point (ScrollX, ScrollY);
			return true;
		}
		public override void RegisterClip (Rectangle clip)
		{
			base.RegisterClip (clip - new Point(ScrollX,ScrollY));
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height)
				NotifyValueChanged ("PageHeight", Slot.Height);
			else if (layoutType == LayoutingType.Width)
				NotifyValueChanged ("PageWidth", Slot.Width);
			else
				return;
			updateMaxScroll(layoutType);
		}
		protected override void onDraw (Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);

			Background.SetAsSource (gr, rBack);
			CairoHelpers.CairoRectangle(gr,rBack, CornerRadius);
			gr.Fill ();

			gr.Save ();
			if (ClipToClientRect) {
				//clip to scrolled client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			gr.Translate (-ScrollX, -ScrollY);
			if (child != null)
				child.Paint (ref gr);
			gr.Restore ();
		}

		#region Mouse & Keyboard
		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{
			if (IFace.Shift)
				ScrollX += e.Delta * ScrollSpeed;
			else
				ScrollY -= e.Delta * ScrollSpeed;
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
		#endregion

		#endregion

		void updateMaxScroll (LayoutingType lt){
			if (Child == null) {
				MaxScrollX = 0;
				MaxScrollY = 0;
				return;
			}

			Rectangle cb = ClientRectangle;

			if (lt == LayoutingType.Height) {
				MaxScrollY = child.Slot.Height - cb.Height;
				if (child.Slot.Height > 0)
					NotifyValueChanged ("ChildHeightRatio", Slot.Height * Slot.Height / child.Slot.Height);			
			} else if (lt == LayoutingType.Width) {
				MaxScrollX = child.Slot.Width - cb.Width;
				if (child.Slot.Width > 0)
					NotifyValueChanged ("ChildWidthRatio", Slot.Width * Slot.Width / child.Slot.Width);
			}
		}
		void onChildListCleared(object sender, EventArgs e){
			ScrollY = 0;
			ScrollX = 0;
		}


    }
}
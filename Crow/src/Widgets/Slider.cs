﻿// Copyright (c) 2020-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


using System;
using System.ComponentModel;
using Drawing2D;

namespace Crow
{
	/// <summary>
	/// templated numeric control to select a value by slidding a cursor.
	/// </summary>
	public class Slider : NumericControl
    {
		#region CTOR
		protected Slider() {}
		public Slider(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region implemented abstract members of TemplatedControl
		Widget cursorParent;
		protected override void loadTemplate (Widget template = null)
		{
			if (cursorParent != null) {
				cursorParent.LayoutChanged -= HandleCursorContainerLayoutChanged;
				cursorParent = null;
			}
			base.loadTemplate (template);

			cursor = child.FindByName ("Cursor");
			if (cursor == null)
				return;
			cursorParent = cursor.Parent as Widget;
			cursorParent.LayoutChanged += HandleCursorContainerLayoutChanged;

			updateCursorWidgetProps ();
		}

		protected virtual void HandleCursorContainerLayoutChanged (object sender, LayoutingEventArgs e)
		{
			computeCursorPosition ();
		}
		#endregion

		protected override void registerUpdate ()
			=> RegisterForLayouting (LayoutingType.ArrangeChildren);

		#region private fields
		int cursorSize, minimumCursorSize;
		Orientation _orientation;
		bool holdCursor, inverted;
		protected Widget cursor;
		#endregion

		protected double unity;

		#region Public properties
		[DefaultValue (Orientation.Horizontal)]
		public virtual Orientation Orientation
		{
			get { return _orientation; }
			set {
				if (_orientation == value)
					return;
				_orientation = value;

				RegisterForLayouting (LayoutingType.All);
				NotifyValueChangedAuto (_orientation);
				updateCursorWidgetProps ();
			}
		}
		[DefaultValue (20)]
		public virtual int MinimuCursorSize {
			get => minimumCursorSize;
			set {
				if (minimumCursorSize == value)
					return;
				minimumCursorSize = value;
				CursorSize = cursorSize;//force recheck
				NotifyValueChangedAuto (minimumCursorSize);
			}
		}

		[DefaultValue (20)]
		public virtual int CursorSize {
			get => cursorSize;
			set {
				int newCursorSize = Math.Max (MinimuCursorSize, value);
				if (cursorSize == newCursorSize)
					return;
				cursorSize = newCursorSize;
				RegisterForGraphicUpdate ();
				NotifyValueChangedAuto (cursorSize);
				updateCursorWidgetProps ();
			}
		}
		/// <summary>
		/// if true, horizontal gauge will align drawing right, and vertical on bottom.
		/// </summary>
		public bool Inverted {
			get => inverted;
			set {
				if (inverted == value)
					return;
				inverted = value;
				NotifyValueChangedAuto (inverted);
				RegisterForLayouting (LayoutingType.ArrangeChildren);
			}
		}

		#endregion

		public override bool ArrangeChildren => true;
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			if (layoutType == LayoutingType.ArrangeChildren)
				computeCursorPosition ();

			return base.UpdateLayout (layoutType);
		}

		void updateCursorWidgetProps ()
		{
			if (cursor == null)
				return;
			if (Orientation == Orientation.Horizontal) {
				cursor.Width = CursorSize;
				cursor.Height = Measure.Stretched;
				cursor.HorizontalAlignment = HorizontalAlignment.Left;
			} else {
				cursor.Height = CursorSize;
				cursor.Width = Measure.Stretched;
				cursor.VerticalAlignment = VerticalAlignment.Top;
			}
		}
		void computeCursorPosition ()
        {
			if (cursor?.Parent == null)
				return;
			if (Maximum <= Minimum) {
				cursor.IsVisible = false;
				return;
			}
			cursor.IsVisible = true;
            Rectangle r = cursor.Parent.ClientRectangle;
			if (_orientation == Orientation.Horizontal) {
				unity = (r.Width - cursorSize) / (Maximum - Minimum);
				if (inverted)
					cursor.Left = r.Right - cursorSize - (int)((Value - Minimum) * unity);
				else
					cursor.Left = r.Left + (int)((Value - Minimum) * unity);
			} else {
				unity = (r.Height - cursorSize) / (Maximum - Minimum);
				if (inverted)
					cursor.Top = r.Bottom - cursorSize - (int)((Value - Minimum) * unity);
				else
					cursor.Top = r.Top + (int)((Value - Minimum) * unity);
			}
        }
		Point mouseDownInit;
		double mouseDownInitValue;

		#region mouse handling
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			mouseDownInit = ScreenPointToLocal (e.Position);
			mouseDownInitValue = Value;
			Rectangle cursInScreenCoord = cursor == null ? default : cursor.ScreenCoordinates (cursor.Slot);
			double multiplier = inverted ? -1 : 1;
			if (cursInScreenCoord.ContainsOrIsEqual (e.Position)){
				//Rectangle r = cursor.Parent.ClientRectangle;
				//if (r.Width - cursorSize > 0) {
				//	double unit = (Maximum - Minimum) / (double)(r.Width - cursorSize);
				//	mouseDownInit += new Point ((int)(Value / unit), (int)(Value / unit));
				//}
				holdCursor = true;
			}else if (_orientation == Orientation.Horizontal) {
				if (e.Position.X < cursInScreenCoord.Left)
					Value -= LargeIncrement * multiplier;
				else
					Value += LargeIncrement * multiplier;
			} else if (e.Position.Y < cursInScreenCoord.Top)
				Value -= LargeIncrement * multiplier;
			else
				Value += LargeIncrement * multiplier;

			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender,MouseButtonEventArgs e)
		{
			holdCursor = false;
			e.Handled = true;
			base.onMouseUp (sender, e);
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			if (holdCursor) {
				Point m = ScreenPointToLocal (e.Position) - mouseDownInit;
				Rectangle r = cursor.Parent.ClientRectangle;

				if (_orientation == Orientation.Horizontal) {
					if (r.Width - cursorSize == 0)
						return;
					double unit = (Maximum - Minimum) / (double)(r.Width - cursorSize);
					if (inverted)
						unit = -unit;
					double tmp = mouseDownInitValue + (double)m.X * unit;
					tmp -= tmp % SmallIncrement;
					Value = tmp;
				} else {
					if (r.Height - cursorSize == 0)
						return;
					double unit = (Maximum - Minimum) / (double)(r.Height - cursorSize);
					if (inverted)
						unit = -unit;
					double tmp = mouseDownInitValue + (double)m.Y * unit;
					tmp -= tmp % SmallIncrement;
					Value = tmp;
				}
			}
			e.Handled = true;

			base.onMouseMove (sender, e);
		}
		#endregion
		/// <summary>
		/// Handler to decrease current value by `SmallIncrement`
		/// </summary>
		/// <param name="sender">event sender</param>
		/// <param name="e">event argument</param>
		public void OnDecrease (object sender, EventArgs e)
		{
			Value -= SmallIncrement;
		}
		/// <summary>
		/// Handler to increase current value by `SmallIncrement`
		/// </summary>
		/// <param name="sender">event sender</param>
		/// <param name="e">event argument</param>
		public void OnIncrease (object sender, EventArgs e)
		{
			Value += SmallIncrement;
		}

		protected override void Dispose(bool disposing)
		{
			if (cursorParent != null) {
				cursorParent.LayoutChanged -= HandleCursorContainerLayoutChanged;
				cursorParent = null;
			}

			base.Dispose(disposing);
		}
	}
}

// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Cairo;
using System;
using System.ComponentModel;

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
		protected override void loadTemplate (Widget template = null)
		{
			base.loadTemplate (template);

			cursor = child.FindByName ("Cursor");
			if (cursor == null)
				return;
			(cursor.Parent as Widget).LayoutChanged += HandleCursorContainerLayoutChanged;
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
		bool holdCursor = false;
		protected Widget cursor;
		#endregion

		protected double unity;

		public override bool ArrangeChildren => true;

		public override bool UpdateLayout (LayoutingType layoutType)
		{
			if (layoutType == LayoutingType.ArrangeChildren) 
				computeCursorPosition ();
			
			return base.UpdateLayout (layoutType);
		}

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
		#endregion

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
			if (cursor == null)
				return;
			if (Maximum <= Minimum) {
				cursor.Visible = false;
				return;
			}
			cursor.Visible = true;
            Rectangle r = cursor.Parent.ClientRectangle;
			if (_orientation == Orientation.Horizontal) {
				unity = (r.Width - cursorSize) / (Maximum - Minimum);
				cursor.Left = r.Left + (int)((Value - Minimum) * unity);
			} else {
				unity = (r.Height - cursorSize) / (Maximum - Minimum);
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
			if (cursInScreenCoord.ContainsOrIsEqual (e.Position)){
				//Rectangle r = cursor.Parent.ClientRectangle;
				//if (r.Width - cursorSize > 0) {
				//	double unit = (Maximum - Minimum) / (double)(r.Width - cursorSize);
				//	mouseDownInit += new Point ((int)(Value / unit), (int)(Value / unit));
				//}
				holdCursor = true;
			}else if (_orientation == Orientation.Horizontal) {
				if (e.Position.X < cursInScreenCoord.Left)
					Value -= LargeIncrement;
				else
					Value += LargeIncrement;
			} else if (e.Position.Y < cursInScreenCoord.Top)
				Value -= LargeIncrement;
			else
				Value += LargeIncrement;

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
					double tmp = mouseDownInitValue + (double)m.X * unit;
					tmp -= tmp % SmallIncrement;
					Value = tmp;
				} else {
					if (r.Height - cursorSize == 0)
						return;					
					double unit = (Maximum - Minimum) / (double)(r.Height - cursorSize);
					double tmp = mouseDownInitValue + (double)m.Y * unit;
					tmp -= tmp % SmallIncrement;
					Value = tmp;
				}
				e.Handled = true;
			}
			
			base.onMouseMove (sender, e);
		}
		#endregion

		public void OnDecrease (object sender, MouseButtonEventArgs e)
		{
			Value -= SmallIncrement;
		}
		public void OnIncrease (object sender, MouseButtonEventArgs e)
		{
			Value += SmallIncrement;
		}
	}
}

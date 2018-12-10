﻿//
// Slider.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Cairo;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;

namespace Crow
{
	/// <summary>
	/// templated numeric control to select a value
	/// by slidding a cursor
	/// </summary>
	public class Slider : NumericControl
    {
		#region CTOR
		protected Slider() : base(){}
		public Slider(Interface iface) : base(iface)
		{}
//		public Slider(double minimum, double maximum, double step)
//			: base(minimum,maximum,step)
//		{
//		}
		#endregion

		#region implemented abstract members of TemplatedControl

		protected override void loadTemplate (GraphicObject template = null)
		{
			
		}

		#endregion

		#region private fields
        Rectangle cursor;
		int _cursorSize;
		Fill _cursorColor;
		Orientation _orientation;
		bool holdCursor = false;
		#endregion

		protected double unity;

		#region Public properties
		[DefaultValue("vgradient|0:White|0,1:LightGrey|0,9:LightGrey|1:DimGrey")]
		public virtual Fill CursorColor {
			get { return _cursorColor; }
			set {
				if (_cursorColor == value)
					return;
				_cursorColor = value;
				RegisterForRedraw ();
				NotifyValueChanged ("CursorColor", _cursorColor);
			}
		}
		[DefaultValue(20)]
		public virtual int CursorSize {
			get { return _cursorSize; }
			set {
				if (_cursorSize == value || value < 8)
					return;
				_cursorSize = value;
				RegisterForGraphicUpdate ();
				NotifyValueChanged ("CursorSize", _cursorSize);
			}
		}
		[DefaultValue(Orientation.Horizontal)]
		public virtual Orientation Orientation
		{
			get { return _orientation; }
			set { 
				if (_orientation == value)
					return;
				_orientation = value; 

				RegisterForLayouting (LayoutingType.All);
				NotifyValueChanged ("Orientation", _orientation);
			}
		}
		#endregion

		[DefaultValue(10.0)]
		public override double Maximum {
			get { return base.Maximum; }
			set {				
				if (value == base.Maximum)
					return;
				base.Maximum = value;
				LargeIncrement = base.Maximum / 10.0;
				SmallIncrement = LargeIncrement / 5.0;
			}
		}

		#region GraphicObject Overrides
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			if (Maximum <= 0)
				return;

			computeCursorPosition ();

			Rectangle r = ClientRectangle;
			PointD pStart;
			PointD pEnd;
			if (_orientation == Orientation.Horizontal) {
				pStart = r.TopLeft + new Point (_cursorSize / 2, r.Height / 2);
				pEnd = r.TopRight + new Point (-_cursorSize / 2, r.Height / 2);
				pStart.Y += 0.5;
				pEnd.Y += 0.5;
			} else {
				pStart = r.TopLeft + new Point (r.Width / 2, _cursorSize / 2);
				pEnd = r.BottomLeft + new Point (r.Width / 2,- _cursorSize / 2);
				pStart.X += 0.5;
				pEnd.X += 0.5;

			}

            Background.SetAsSource(gr, r);
            gr.Rectangle (r);
            gr.Fill ();

			DrawGraduations (gr, pStart,pEnd);

			DrawCursor (gr, cursor);
		}
		#endregion

		protected virtual void DrawGraduations(Context gr, PointD pStart, PointD pEnd)
		{
			Foreground.SetAsSource (gr);

			gr.LineWidth = 1;
			gr.MoveTo(pStart);
			gr.LineTo(pEnd);

			gr.Stroke();

		}
		protected virtual void DrawCursor(Context gr, Rectangle _cursor)
		{
			CairoHelpers.CairoRectangle (gr, _cursor, CornerRadius);
            Foreground.SetAsSource(gr, _cursor);
            gr.StrokePreserve();
            CursorColor.SetAsSource(gr, _cursor);
            gr.Fill();
		}

        void computeCursorPosition()
        {            
            Rectangle r = ClientRectangle;
			PointD p1; 

			if (_orientation == Orientation.Horizontal) {
				cursor = new Rectangle (new Size (_cursorSize, (int)(r.Height)));
				p1 = r.TopLeft + new Point (_cursorSize / 2, r.Height / 2);
				unity = (double)(r.Width - _cursorSize) / (Maximum - Minimum);
				cursor.TopLeft = new Point (r.Left + (int)((Value - Minimum) * unity),
					(int)(p1.Y - cursor.Height / 2));
			} else {
				cursor = new Rectangle (new Size ((int)(r.Width), _cursorSize));
				p1 = r.TopLeft + new Point (r.Width / 2, _cursorSize / 2);
				unity = (double)(r.Height - _cursorSize) / (Maximum - Minimum);
				cursor.TopLeft = new Point ((int)(p1.X - r.Width / 2),
					r.Top + (int)((Value - Minimum) * unity));				
			}
			//cursor.Inflate (-1);
        }
		Point mouseDownInit;
		double mouseDownInitValue;

		#region mouse handling
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
			mouseDownInit = ScreenPointToLocal (e.Position);
			mouseDownInitValue = Value;
			Rectangle cursInScreenCoord = ScreenCoordinates (cursor + Slot.Position);
			if (cursInScreenCoord.ContainsOrIsEqual (e.Position)){
//				Rectangle r = ClientRectangle;
//				if (r.Width - _cursorSize > 0) {
//					double unit = (Maximum - Minimum) / (double)(r.Width - _cursorSize);
//					mouseDownInit += new Point ((int)(Value / unit), (int)(Value / unit));
//				}
				holdCursor = true;
			}else if (_orientation == Orientation.Horizontal) {
				if (e.Position.X < cursInScreenCoord.Left)
					Value -= LargeIncrement;
				else
					Value += LargeIncrement;
			} else {
				if (e.Position.Y < cursInScreenCoord.Top)
					Value -= LargeIncrement;
				else
					Value += LargeIncrement;
			}
		}
		public override void onMouseUp (object sender,MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			holdCursor = false;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			if (holdCursor) {				
				Point m = ScreenPointToLocal (e.Position) - mouseDownInit;
				Rectangle r = ClientRectangle;

				if (_orientation == Orientation.Horizontal) {
					if (r.Width - _cursorSize == 0)
						return;					
					double unit = (Maximum - Minimum) / (double)(r.Width - _cursorSize);
					double tmp = mouseDownInitValue + (double)m.X * unit;
					tmp -= tmp % SmallIncrement;
					Value = tmp;
				} else {
					if (r.Height - _cursorSize == 0)
						return;					
					double unit = (Maximum - Minimum) / (double)(r.Height - _cursorSize);
					double tmp = mouseDownInitValue + (double)m.Y * unit;
					tmp -= tmp % SmallIncrement;
					Value = tmp;
				}
			}
			
			base.onMouseMove (sender, e);
		}
		#endregion
    }
}

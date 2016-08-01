//
//  Slider.cs
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
using Cairo;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;

namespace Crow
{
	public class Slider : NumericControl
    {
		#region CTOR
		public Slider() : base()
		{}
		public Slider(double minimum, double maximum, double step)
			: base(minimum,maximum,step)
		{
		}
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
		[XmlAttributeAttribute()][DefaultValue("vgradient|0:White|0,1:LightGray|0,9:LightGray|1:DimGray")]
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
		[XmlAttributeAttribute()][DefaultValue(20)]
		public virtual int CursorSize {
			get { return _cursorSize; }
			set {
				if (_cursorSize == value)
					return;
				_cursorSize = value;
				RegisterForGraphicUpdate ();
				NotifyValueChanged ("CursorSize", _cursorSize);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(Orientation.Horizontal)]
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

		[XmlAttributeAttribute()][DefaultValue(10.0)]
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
			} else {
				pStart = r.TopLeft + new Point (r.Width / 2, _cursorSize / 2);
				pEnd = r.BottomLeft + new Point (r.Width / 2,- _cursorSize / 2);
			}

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
			CursorColor.SetAsSource (gr, _cursor);
			CairoHelpers.CairoRectangle (gr, _cursor, CornerRadius);
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
				cursor.TopLeft = new Point (r.Left + (int)(Value * unity),
					(int)(p1.Y - cursor.Height / 2));
			} else {
				cursor = new Rectangle (new Size ((int)(r.Width), _cursorSize));
				p1 = r.TopLeft + new Point (r.Width / 2, _cursorSize / 2);
				unity = (double)(r.Height - _cursorSize) / (Maximum - Minimum);
				cursor.TopLeft = new Point ((int)(p1.X - r.Width / 2),
					r.Top + (int)(Value * unity));				
			}
			cursor.Inflate (-1);
        }
        
		#region mouse handling
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);

			Rectangle cursInScreenCoord = ScreenCoordinates (cursor + Slot.Position);
			if (cursInScreenCoord.ContainsOrIsEqual (e.Position))
				holdCursor = true;
			else if (_orientation == Orientation.Horizontal) {
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
				if (_orientation == Orientation.Horizontal)
					Value += (double)e.XDelta / unity;
				else
					Value += (double)e.YDelta / unity;
			}
			
			base.onMouseMove (sender, e);
		}
		#endregion
    }
}

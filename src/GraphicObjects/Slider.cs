using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.Xml.Serialization;

namespace go
{
	public class Slider : NumericControl
    {
        Rectangle cursor;
		int _cursorWidth;
		Color _cursorColor;
		//Point mouseDownPos;
		bool holdCursor = false;//may use maybe active go equality

		protected double unity;

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue("Gray")]
		public override Color Background {
			get { return base.Background; }
			set { base.Background = value; }
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(2)]
		public override int BorderWidth {
			get { return base.BorderWidth; }
			set { base.BorderWidth = value; }
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue("BlueGray")]
		public virtual Color CursorColor {
			get { return _cursorColor; }
			set {
				_cursorColor = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(20)]
		public virtual int CursorWidth {
			get { return _cursorWidth; }
			set {
				_cursorWidth = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(true)]
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}


        
		public Slider() : base()
		{}
		public Slider(double minimum, double maximum, double step)
			: base(minimum,maximum,step)
        {
        }

        void computeCursorPosition()
        {            
            Rectangle r = clientBounds;

			cursor = new Rectangle(new Size(_cursorWidth,(int) (r.Height)));

			PointD p1 = r.TopLeft + new Point(cursor.Width/2, r.Height / 2);

			unity = (double)(r.Width - cursor.Width) / (Maximum - Minimum);

			cursor.TopLeft = new Point(r.Left + (int)(Value * unity),
                                        (int)(p1.Y - cursor.Height / 2));             
        }
        

		#region mouse handling
		public override void onMouseButtonDown (object sender, OpenTK.Input.MouseButtonEventArgs e)
		{
			base.onMouseButtonDown (sender, e);

			Rectangle cursInScreenCoord = ScreenCoordinates(cursor+Slot.Position);
			if (cursInScreenCoord.ContainsOrIsEqual(e.Position))
				holdCursor = true;
			else if (e.Position.X < cursInScreenCoord.Left)
				Value -= LargeIncrement;
            else
				Value += LargeIncrement;
		}
		public override void onMouseButtonUp (object sender, OpenTK.Input.MouseButtonEventArgs e)
		{
			base.onMouseButtonUp (sender, e);

			holdCursor = false;
		}
		public override void onMouseMove (object sender, OpenTK.Input.MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!holdCursor)
				return;

			Value += (double)e.XDelta / unity;

		}
		#endregion

		internal override void UpdateGraphic ()
		{
			computeCursorPosition();
			base.UpdateGraphic ();
		}

		public override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Rectangle r = clientBounds;
			PointD pStart = r.TopLeft + new Point(cursor.Width/2, r.Height / 2);
			PointD pEnd = r.TopRight + new Point(-cursor.Width/2, r.Height / 2);

			DrawGraduations (gr, pStart,pEnd);

			DrawCursor (gr, cursor);
		}

		public virtual void DrawGraduations(Context gr, PointD pStart, PointD pEnd)
		{
			gr.Color = Foreground;

			gr.LineWidth = 1;
			gr.MoveTo(pStart);
			gr.LineTo(pEnd);

			gr.Stroke();

		}
		public virtual void DrawCursor(Context gr, Rectangle _cursor)
		{
			gr.Color = CursorColor;
			CairoHelpers.CairoRectangle (gr, _cursor, CornerRadius);
			gr.FillPreserve();
			gr.Color = BorderColor;
			gr.Stroke ();
		}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
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

		#region private fields
        Rectangle cursor;
		int _cursorWidth;
		Color _cursorColor;
		bool holdCursor = false;
		#endregion

		protected double unity;

		#region Public properties
		[XmlAttributeAttribute()][DefaultValue("BlueGray")]
		public virtual Color CursorColor {
			get { return _cursorColor; }
			set {
				_cursorColor = value;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(20)]
		public virtual int CursorWidth {
			get { return _cursorWidth; }
			set {
				_cursorWidth = value;
				registerForGraphicUpdate ();
			}
		}
		#endregion

		#region GraphicObject Overrides
		[XmlAttributeAttribute()][DefaultValue("Gray")]
		public override Color Background {
			get { return base.Background; }
			set { base.Background = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(true)]
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		protected override void UpdateGraphic ()
		{
			computeCursorPosition();
			base.UpdateGraphic ();
		}

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Rectangle r = ClientRectangle;
			PointD pStart = r.TopLeft + new Point(cursor.Width/2, r.Height / 2);
			PointD pEnd = r.TopRight + new Point(-cursor.Width/2, r.Height / 2);

			DrawGraduations (gr, pStart,pEnd);

			DrawCursor (gr, cursor);
		}
		#endregion

		protected virtual void DrawGraduations(Context gr, PointD pStart, PointD pEnd)
		{
			gr.Color = Foreground;

			gr.LineWidth = 1;
			gr.MoveTo(pStart);
			gr.LineTo(pEnd);

			gr.Stroke();

		}
		protected virtual void DrawCursor(Context gr, Rectangle _cursor)
		{
			gr.Color = CursorColor;
			CairoHelpers.CairoRectangle (gr, _cursor, CornerRadius);
			gr.FillPreserve();
		}

        void computeCursorPosition()
        {            
            Rectangle r = ClientRectangle;

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
    }
}

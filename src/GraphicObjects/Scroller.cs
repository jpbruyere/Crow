using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Cairo;
using System.Xml.Serialization;
using OpenTK.Input;

namespace go
{
	public class Scroller : Container
    {
		bool _verticalScrolling;
		bool _horizontalScrolling;

		#region public properties

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(false)]
		public bool VerticalScrolling {
			get { return _verticalScrolling; }
			set { _verticalScrolling = value; }
		}
        
		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(false)]
        public bool HorizontalScrolling {
			get { return _horizontalScrolling; }
			set { _horizontalScrolling = value; }
		}

        [XmlIgnore]         
        public int scrollX = 0;
        [XmlIgnore]
        public int scrollY = 0;

		public static int ScrollSpeed = 10;

		#endregion


        public Scroller()
            : base()
        {
        }

		#region GraphicObject Overrides

		#endregion

		#region Mouse handling
		public override bool MouseIsIn (Point m)
		{			
			return Visible ? base.ScreenCoordinates(Slot).ContainsOrIsEqual (m) : false; 
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			Point m = e.Position + new Point (scrollX, scrollY);
			base.onMouseMove (sender, new MouseMoveEventArgs(m.X,m.Y,e.XDelta,e.YDelta));
		}
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{
			//base.onMouseWheel (sender, e); base event buble to the top
//			if (MouseWheelChanged!=null)
//				MouseWheelChanged (this, e);

			if (child == null)
				return;
			
			if (VerticalScrolling )
            {
                //add redraw call with old bounds to errase old position
                registerForRedraw();

				scrollY += e.Delta * ScrollSpeed;

                if (scrollY > 0)
                    scrollY = 0;
				else if (scrollY < -child.Slot.Height + ClientRectangle.Height)
					scrollY = -child.Slot.Height + ClientRectangle.Height;

            }
            if (HorizontalScrolling )
            {
                //add redraw call with old bounds to errase old position
                registerForRedraw();

				scrollX += e.Delta * ScrollSpeed;

				if (scrollX > 0)
					scrollX = 0;
				else if (scrollX < -child.Slot.Width + ClientRectangle.Width)
					scrollX = -child.Slot.Width + ClientRectangle.Width;
            }


            //renderBounds.Y = -scrollY;
            registerForRedraw();
			//Parent.registerForGraphicUpdate ();
        }
		#endregion

		public override Rectangle ContextCoordinates (Rectangle r)
		{
			return base.ContextCoordinates (r) + new Point(scrollX,scrollY);
		}
		public override Rectangle ScreenCoordinates (Rectangle r)
		{
			return base.ScreenCoordinates (r) + new Point(scrollX,scrollY);
		}

		public override void registerClipRect ()
		{
			TopContainer.redrawClip.AddRectangle (base.ScreenCoordinates(Slot));
		}
		public override void UpdateLayout ()
		{
			base.UpdateLayout ();

			if (!LayoutIsValid)
				return;

			//override child positionning inside scroller in the 
			//scrolling direction
			if (child == null)
				return;
			if (VerticalScrolling)
				child.Slot.Y = 0;
			if (HorizontalScrolling)
				child.Slot.X = 0;
		}
		public override void Paint(ref Cairo.Context ctx, Rectangles clip = null)
		{
			if (!Visible)//check if necessary??
				return;

			ctx.Save();

			//			ctx.Rectangle(ContextCoordinates(Slot));
			//            ctx.Clip();
			//
//			if (clip != null)
//				clip.clip(ctx);
			//clip.Srcoll (this);

			base.Paint (ref ctx, clip);

			//clip to client zone
			ctx.Rectangle(Parent.ContextCoordinates(ClientRectangle + Slot.Position));
			ctx.Clip();

//			if (clip != null)
//				clip.Srcoll (this);
			//            if (clip != null)
			//                clip.Rebase(this);

			if (child != null)
				child.Paint(ref ctx, clip);

			ctx.Restore();            
		}
    }
}

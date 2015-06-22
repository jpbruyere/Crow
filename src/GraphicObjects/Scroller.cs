using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Cairo;
using System.Xml.Serialization;
using OpenTK.Input;
using System.ComponentModel;

namespace go
{
	public class Scroller : Container
    {
		bool _verticalScrolling;
		bool _horizontalScrolling;
		bool _scrollbarVisible;
		int _scrollX = 0;
		int _scrollY = 0;


		#region public properties

		[XmlAttributeAttribute][DefaultValue(false)]
		public bool VerticalScrolling {
			get { return _verticalScrolling; }
			set { _verticalScrolling = value; }
		}
        
		[XmlAttributeAttribute][DefaultValue(false)]
        public bool HorizontalScrolling {
			get { return _horizontalScrolling; }
			set { _horizontalScrolling = value; }
		}
		[XmlAttributeAttribute][DefaultValue(true)]
		public bool ScrollbarVisible {
			get { return _scrollbarVisible; }
			set { _scrollbarVisible = value; }
		}


		[XmlIgnore]         
		public int ScrollX {
			get {
				return _scrollX;
			}
			set {
				_scrollX = value;
			}
		}


		[XmlIgnore]
		public int ScrollY {
			get {
				return _scrollY;
			}
			set {
				_scrollY = value;
			}
		}

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
			Point m = e.Position - new Point (ScrollX, ScrollY);
			base.onMouseMove (sender, new MouseMoveEventArgs(m.X,m.Y,e.XDelta,e.YDelta));
		}
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{
			//base.onMouseWheel (sender, e); base event buble to the top
//			if (MouseWheelChanged!=null)
//				MouseWheelChanged (this, e);

			if (Child == null)
				return;
			
			if (VerticalScrolling )
            {
                //add redraw call with old bounds to errase old position
                RegisterForRedraw();

				ScrollY -= e.Delta * ScrollSpeed;

                if (ScrollY < 0)
                    ScrollY = 0;
				else if (ScrollY > Child.Slot.Height - ClientRectangle.Height)
					ScrollY = Child.Slot.Height - ClientRectangle.Height;

            }
            if (HorizontalScrolling )
            {
                //add redraw call with old bounds to errase old position
                RegisterForRedraw();

				ScrollX -= e.Delta * ScrollSpeed;

				if (ScrollX < 0)
					ScrollX = 0;
				else if (ScrollX > Child.Slot.Width - ClientRectangle.Width)
					ScrollX = Child.Slot.Width - ClientRectangle.Width;
            }


            //renderBounds.Y = -scrollY;
            RegisterForRedraw();
			//Parent.registerForGraphicUpdate ();
        }
		#endregion

		public override Rectangle ContextCoordinates (Rectangle r)
		{
			return base.ContextCoordinates (r) - new Point(ScrollX,ScrollY);
		}
		public override Rectangle ScreenCoordinates (Rectangle r)
		{
			return base.ScreenCoordinates (r) - new Point(ScrollX,ScrollY);
		}

		public override void registerClipRect ()
		{
			TopContainer.redrawClip.AddRectangle (base.ScreenCoordinates(Slot));
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

			if (Child != null)
				Child.Paint(ref ctx, clip);

			ctx.Restore();            
		}
    }
}

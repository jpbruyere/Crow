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

namespace Crow
{
	public class Scroller : Container, IValueChange
    {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		#endregion

		bool _verticalScrolling;
		bool _horizontalScrolling;
		bool _scrollbarVisible;
		double _scrollX = 0.0;
		double _scrollY = 0.0;
		int scrollSpeed;



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


		[XmlAttributeAttribute][DefaultValue(0.0)]
		public double ScrollX {
			get {
				return _scrollX;
			}
			set {
				if (_scrollX == value)
					return;
				if (value < 0.0)
					_scrollX = 0.0;
				else if (value > Child.Slot.Width - ClientRectangle.Width)
					_scrollX = Math.Max(0.0, Child.Slot.Width - ClientRectangle.Width);
				else
					_scrollX = value;
				ValueChanged.Raise(this, new ValueChangeEventArgs("ScrollX", _scrollX));
				RegisterForRedraw();
			}
		}


		[XmlAttributeAttribute][DefaultValue(0.0)]
		public double ScrollY {
			get {
				return _scrollY;
			}
			set {
				if (_scrollY == value)
					return;
				if (value < 0.0)
					_scrollY = 0.0;
				else if (value > Child.Slot.Height - ClientRectangle.Height)
					_scrollY = Math.Max(0.0,Child.Slot.Height - ClientRectangle.Height);
				else
					_scrollY = value;
				ValueChanged.Raise(this, new ValueChangeEventArgs("ScrollY", _scrollY));
				RegisterForRedraw();
			}
		}

		[XmlIgnore]
		public int MaximumScroll {
			get {
				return VerticalScrolling ? 
					child == null ? 0 : Child.Slot.Height - ClientRectangle.Height :
					Child.Slot.Width - ClientRectangle.Width;				
			}
		}

		[XmlAttributeAttribute][DefaultValue(30)]
		public int ScrollSpeed {
			get { return scrollSpeed; }
			set {
				scrollSpeed = value;
				ValueChanged.Raise(this, new ValueChangeEventArgs("ScrollSpeed", scrollSpeed));
			}
		}

		#endregion


        public Scroller()
            : base()
        {
        }

		#region GraphicObject Overrides
		void OnChildLayoutChanges (object sender, LayoutChangeEventArgs arg)
		{
			int maxScroll = MaximumScroll;
			if (_verticalScrolling) {
				if (arg.LayoutType == LayoutingType.Height) {
					if (maxScroll < ScrollY) {
						Debug.WriteLine ("scrolly={0} maxscroll={1}", ScrollY, maxScroll);
						ScrollY = 0;
					}
					ValueChanged.Raise (this, new ValueChangeEventArgs ("MaximumScroll", maxScroll));
				}
			} else if (arg.LayoutType == LayoutingType.Width) {
				if (maxScroll < ScrollX)
					ScrollX = 0;
				ValueChanged.Raise (this, new ValueChangeEventArgs ("MaximumScroll", maxScroll));
			}
		}
		void onChildListCleared(object sender, EventArgs e){
			ScrollY = 0;
			ScrollX = 0;
		}
		public override T SetChild<T> (T _child)
		{			
			GraphicObject c = child as GraphicObject;
			Group g = child as Group;
			if (c != null) {
				c.LayoutChanged -= OnChildLayoutChanges;
				if (g != null)
					g.ChildrenCleared -= onChildListCleared;
			}
			c = _child as GraphicObject;
			g = _child as Group;
			if (c != null) {
				c.LayoutChanged += OnChildLayoutChanges;
				if (g != null)
					g.ChildrenCleared += onChildListCleared;				
			}
			return base.SetChild (_child);
		}
		#endregion

		#region Mouse handling
		internal Point savedMousePos;
		public override bool MouseIsIn (Point m)
		{			
//			Debug.WriteLine ("Mouse in scroller: {0} scr coord:{1} mouse:{2}",
//				base.ScreenCoordinates (Slot).ContainsOrIsEqual (m),
//				base.ScreenCoordinates (Slot), m);

			return Visible ? base.ScreenCoordinates(Slot).ContainsOrIsEqual (m) : false; 
		}
		public override void checkHoverWidget (MouseMoveEventArgs e)
		{
			savedMousePos = e.Position;
			Point m = e.Position - new Point ((int)ScrollX, (int)ScrollY);
			base.checkHoverWidget (new MouseMoveEventArgs(m.X,m.Y,e.XDelta,e.YDelta));
		}
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{
			//base.onMouseWheel (sender, e); base event buble to the top
//			if (MouseWheelChanged!=null)
//				MouseWheelChanged (this, e);

			if (Child == null)
				return;
			
			if (VerticalScrolling )
				ScrollY -= e.Delta * ScrollSpeed;
            if (HorizontalScrolling )
				ScrollX -= e.Delta * ScrollSpeed;
        }
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			savedMousePos.X += e.XDelta;
			savedMousePos.Y += e.YDelta;
			base.onMouseMove (sender, new MouseMoveEventArgs(savedMousePos.X,savedMousePos.Y,e.XDelta,e.YDelta));
		}
		#endregion

		public override Rectangle ContextCoordinates (Rectangle r)
		{
			return base.ContextCoordinates (r) - new Point((int)ScrollX,(int)ScrollY);
		}
		public override Rectangle ScreenCoordinates (Rectangle r)
		{
			return base.ScreenCoordinates (r) - new Point((int)ScrollX,(int)ScrollY);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using Cairo;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
{
	public class Group : GraphicObject, IXmlSerializable
    {
		#region CTOR
		public Group()
			: base()
		{            
		}
		#endregion

        bool _multiSelect = false;

        public GraphicObject activeWidget;
        public List<GraphicObject> Children = new List<GraphicObject>();

		[XmlAttributeAttribute()][DefaultValue(false)]
        public bool MultiSelect
        {
            get { return _multiSelect; }
            set { _multiSelect = value; }
        }
			
			
        public virtual T addChild<T>(T child)
        {
			GraphicObject g = child as GraphicObject;
            Children.Add(g);
            g.Parent = this as GraphicObject;            
			g.RegisterForLayouting ((int)LayoutingType.Sizing);
            return (T)child;
        }
        public virtual void removeChild(GraphicObject child)        
		{
            Children.Remove(child);
            child.Parent = null;
			this.RegisterForLayouting ((int)LayoutingType.Sizing);
        }

		public void putWidgetOnTop(GraphicObject w)
		{
			if (Children.Contains(w))
			{
				Children.Remove(w);
				Children.Add(w);
			}
		}
		public void putWidgetOnBottom(GraphicObject w)
		{
			if (Children.Contains(w))
			{
				Children.Remove(w);
				Children.Insert(0, w);
			}
		}
			
		#region GraphicObject overrides
		[XmlIgnore]public override bool DrawingIsValid {
			get {
				if (!base.DrawingIsValid)
					return false;
				foreach (GraphicObject g in Children) {
					if (!g.DrawingIsValid)
						return false;
				}
				return true;
			}
		}

		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			foreach (GraphicObject w in Children) {
				GraphicObject r = w.FindByName (nameToFind);
				if (r != null)
					return r;
			}
			return null;
		}
		public override bool Contains (GraphicObject goToFind)
		{
			foreach (GraphicObject w in Children) {
				if (w == goToFind)
					return true;
				if (w.Contains (goToFind))
					return true;
			}
			return false;
		}
		protected override Size measureRawSize ()
		{
			Size tmp = new Size ();

			foreach (GraphicObject c in Children.Where(ch=>ch.Visible)) {
				tmp.Width = Math.Max (tmp.Width, c.Slot.Right);
				tmp.Height = Math.Max (tmp.Height, c.Slot.Bottom);
			}

			tmp.Width += 2*Margin;
			tmp.Height += 2*Margin;

			return tmp;
		}
			
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			//position smaller objects in group when group size is fit
			switch (layoutType) {
			case LayoutingType.Width:				
				if (Width < 0) {
					int crw = ClientRectangle.Width;
					foreach (GraphicObject c in Children.Where(ch => ch.Slot.Width != crw && ch.Visible))
						c.RegisterForLayouting ((int)LayoutingType.X);						
				} else {
					foreach (GraphicObject c in Children.Where(ch => ch.Width == 0 && ch.Visible))
						c.RegisterForLayouting ((int)LayoutingType.Width);											
				}
				break;
			case LayoutingType.Height:
				if (Height < 0) {
					int crh = ClientRectangle.Height;
					foreach (GraphicObject c in Children.Where(ch => ch.Slot.Height != crh && ch.Visible))
						c.RegisterForLayouting ((int)LayoutingType.Y);						
				} else {
					foreach (GraphicObject c in Children.Where(ch => ch.Height == 0 && ch.Visible))
						c.RegisterForLayouting ((int)LayoutingType.Height);											
				}
				break;
			}
		}

		public override Rectangle ContextCoordinates(Rectangle r){
			return r + ClientRectangle.Position;
		}	

		protected override void onDraw (Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);
			gr.Color = Background;
			CairoHelpers.CairoRectangle(gr,rBack,CornerRadius);
			gr.Fill ();

			foreach (GraphicObject g in Children.Where(ch=>ch.Visible)) {
				g.Paint (ref gr);
			}
		}

		public override void Paint(ref Context ctx, Rectangles clip = null)
		{
			if ( !(Visible) )
				return;

			//			ctx.Save ();

//			if (clip!=null)
//				clip.clearAndClip (ctx);//n'était pas actif

			if (bmp == null)
				UpdateGraphic ();
			else {

				Rectangle rb = Parent.ContextCoordinates (Slot);

				if (clip != null) {
					clip.Rebase (this);									
					//localClip = clip.intersectingRects (Slot.Size);
				} else {
					clip = new Rectangles ();
					//TODO:added lately slot to empty clip,
					//should rework this precise case causing expandable not
					//to show image changes
					clip.AddRectangle (ContextCoordinates (Slot.Size));
				}

				if (!DrawingIsValid || clip != null) {//false when 1 child has changed
					//child having their content changed has to be repainted
					//and those with slot intersecting clip rectangle have also to be repainted

					using (ImageSurface cache =
						      new ImageSurface (bmp, Format.Argb32, Slot.Width, Slot.Height, Slot.Width * 4)) {
						Context gr = new Context (cache);
						clip.clearAndClip (gr);

						Rectangle rBack = Slot.Size;
						gr.Color = Background;
						CairoHelpers.CairoRectangle(gr,rBack,CornerRadius);
						gr.Fill ();
						#if DEBUG_CLIP_RECTANGLE
						clip.stroke (gr, Color.Amaranth.AdjustAlpha (0.8));
						#endif
						foreach (GraphicObject c in Children.Where(ch=>ch.Visible)) {
							Rectangles childClip = clip.intersectingRects (ContextCoordinates(c.Slot));
							if (!c.DrawingIsValid || childClip.count > 0)
								c.Paint (ref gr,childClip);//, localClip);
						}

						gr.Dispose ();
					}
				}
			}
				
			base.Paint (ref ctx, clip);

			//			ctx.Restore(); 
		}
		#endregion

	
		#region Mouse handling
		internal override void checkHoverWidget (OpenTK.Input.MouseMoveEventArgs e)
		{
			if (TopContainer.hoverWidget != this) {
				TopContainer.hoverWidget = this;
				onMouseEnter (this, e);
			}
			foreach (GraphicObject g in Children)
			{
				if (g.MouseIsIn(e.Position))
				{
					g.checkHoverWidget (e);
					return;
				}
			}
			base.checkHoverWidget (e);
		}
		#endregion
			        
//        public override string ToString()
//        {
//            string tmp = base.ToString();
//            foreach (GraphicObject w in Children)
//            {
//                tmp += "\n" + w.ToString();
//            }
//            return tmp;
//        }

		#region IXmlSerializable

        public override System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
        public override void ReadXml(System.Xml.XmlReader reader)
        {
            base.ReadXml(reader);

            using (System.Xml.XmlReader subTree = reader.ReadSubtree())
            {
                subTree.Read();

                while (!subTree.EOF)
                {
                    subTree.Read();

                    if (!subTree.IsStartElement())
                        break;

                    Type t = Type.GetType("go." + subTree.Name);
                    GraphicObject go = (GraphicObject)Activator.CreateInstance(t);
                    (go as IXmlSerializable).ReadXml(subTree);                    
                    addChild(go);
                }
            }
        }
        public override void WriteXml(System.Xml.XmlWriter writer)
        {
            base.WriteXml(writer);

            foreach (GraphicObject go in Children)
            {
                writer.WriteStartElement(go.GetType().Name);
                (go as IXmlSerializable).WriteXml(writer);
                writer.WriteEndElement();
            }
        }
    
		#endregion
	}
}

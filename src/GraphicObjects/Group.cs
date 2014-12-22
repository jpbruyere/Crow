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
            LayoutIsValid = false;
            return (T)child;
        }
        public void removeChild(GraphicObject child)        
		{
            Children.Remove(child);
            child.Parent = null;
            LayoutIsValid = false;
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
		[XmlAttributeAttribute()][DefaultValue(true)]//overiden to get default to true
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
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
		[XmlIgnore]public override bool LayoutIsValid
		{
			get
			{
				if (!Visible)
					return true;

				if (!base.LayoutIsValid)
					return false;
				else//le layout n'est valide que si tous les enfents sont validés aussi
				{
					foreach (GraphicObject w in Children)
						if (!w.LayoutIsValid)
							return false;
				}

				return true;
			}
			set { base.LayoutIsValid = value; }
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
		public override void InvalidateLayout()
		{
			base.InvalidateLayout();
			foreach (GraphicObject w in Children)
				w.InvalidateLayout();
		}
		protected override Size measureRawSize ()
		{
			Size raw = Bounds.Size;
			Size tmp = new Size ();

			if (raw.Width >= 0 && raw.Height >= 0)
				return raw;

			foreach (GraphicObject c in Children) {
				if (raw.Width < 0) {
					if (c.XIsValid & c.WIsValid)
						tmp.Width = Math.Max (tmp.Width, c.Slot.Right);
					else
						return raw;
				}
				if (raw.Height < 0) {
					if (c.YIsValid & c.HIsValid)
						tmp.Height = Math.Max (tmp.Height, c.Slot.Bottom);
					else
						return raw;
				}
			}

			if (raw.Width < 0)
				tmp.Width += 2*Margin;
			if (raw.Height < 0)
				tmp.Height += 2*Margin;

			return tmp;
		}
		public override void UpdateLayout()
		{
			if (LayoutIsValid)
				return;

			bool atLeastOneChildHasWNotDependingOnParent = false;
			bool atLeastOneChildHasHNotDependingOnParent = false;

			foreach (GraphicObject c in Children) {
				if (c.LayoutIsValid)
					continue;

				if (Width < 0 && c.Width != 0)
					atLeastOneChildHasWNotDependingOnParent = true;
				if (Height < 0 && c.Height != 0)
					atLeastOneChildHasHNotDependingOnParent = true;

				c.UpdateLayout ();
			}

			if (Width < 0 && !atLeastOneChildHasWNotDependingOnParent)
				Debug.WriteLine ("ERROR: no child has fixed width and parent width is set to content!");
			if (Height < 0 && !atLeastOneChildHasHNotDependingOnParent)
				Debug.WriteLine ("ERROR: no child has fixed height and parent height is set to content!");

			base.UpdateLayout ();
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

			foreach (GraphicObject g in Children) {
				g.Paint (ref gr);
			}
		}

		public override void Paint(ref Context ctx, Rectangles clip = null)
		{
			if ( !(Visible&LayoutIsValid) )
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
				} else
					clip = new Rectangles ();

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
						foreach (GraphicObject c in Children) {
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
		public override void onMouseMove (object sender, OpenTK.Input.MouseMoveEventArgs e)
		{
			foreach (GraphicObject g in Children)
            {
				if (g.MouseIsIn(e.Position))
                {
					g.onMouseMove (sender, e);
                    return;
                }
            }
			base.onMouseMove (sender, e);
		}
		#endregion
			        
        public override string ToString()
        {
            string tmp = base.ToString();
            foreach (GraphicObject w in Children)
            {
                tmp += "\n" + w.ToString();
            }
            return tmp;
        }

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

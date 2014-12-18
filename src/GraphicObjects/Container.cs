using System;
using System.Xml.Serialization;
using System.Reflection;
using OpenTK.Input;

namespace go
{
    public class Container : GraphicObject, IXmlSerializable
    {
        public GraphicObject child;

        public Container()
            : base()
        {
        }
        public Container(Rectangle _bounds)
            : base(_bounds)
        {
        }

        public T setChild<T>(T _child)
        {

            if (child != null)
                child.Parent = null;

            child = _child as GraphicObject;

            if (child != null)
                child.Parent = this;

            return (T)_child;
        }
		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			return child == null ? null : child.FindByName (nameToFind);
		}
        public override void InvalidateLayout()
        {
            base.InvalidateLayout();

            if (child != null)
                child.InvalidateLayout();
        }
        public override bool LayoutIsValid
        {
            get
            {
                if (!Visible)
                    return true;

				return !base.LayoutIsValid || child == null ?
					base.LayoutIsValid :
					child.LayoutIsValid;
            }
			set { base.LayoutIsValid = value; }
        }
		public override Size measureRawSize ()
		{
			Size raw = Bounds.Size;

			if (child != null) {
				if (Bounds.Width < 0 && child.WIsValid)
					raw.Width = child.Width + 2 * (Margin + BorderWidth);
				if (Bounds.Height < 0 && child.HIsValid)
					raw.Height = child.Height + 2 * (Margin + BorderWidth);
			}

			return raw;
		}
        public override void UpdateLayout()
        {
			if (LayoutIsValid)
                return;

			if (Width < 0 && child.Width == 0)
				child.Width = -1;
			if (Height < 0 && child.Height == 0)
				child.Height = -1;

			if (!(base.LayoutIsValid))
				base.UpdateLayout();
				
            if (child != null)
            {
				if (!child.LayoutIsValid) {
					child.UpdateLayout ();

					if (!WIsValid) {
						if (Width < 0 && child.WIsValid) {
							Slot.Width = child.Slot.Width + 2 * Margin + 2 * BorderWidth;
							WIsValid = true;
						}
					}
					if (!HIsValid) {
						if (Height < 0 && child.HIsValid) {
							Slot.Height = child.Slot.Height + 2 * Margin + 2 * BorderWidth;
							HIsValid = true;
						}
					}
				}
            }

            if (LayoutIsValid)
                registerForRedraw();
        }
//		public override void onDraw (Cairo.Context gr)
//		{
//			base.onDraw (gr);
//
//			if (child == null)
//				return;
//			if (!child.Visible)
//				return;
//
//			child.Paint (ref gr);
//		}
		public override Rectangle ContextCoordinates (Rectangle r)
		{
			return
				Parent.ContextCoordinates(r) + getSlot().Position +  ClientRectangle.Position;

		}
		public override void Paint(ref Cairo.Context ctx, Rectangles clip = null)
        {
            if (!Visible)//check if necessary??
                return;

            ctx.Save();

//			ctx.Rectangle(ContextCoordinates(Slot));
//            ctx.Clip();
//
            if (clip != null)
				clip.clip(ctx);

            base.Paint(ref ctx, clip);

            //clip to client zone
			ctx.Rectangle(Parent.ContextCoordinates(ClientRectangle + Slot.Position));
			ctx.Clip();

//            if (clip != null)
//                clip.Rebase(this);

            if (child != null)
                child.Paint(ref ctx, clip);

            ctx.Restore();            
        }

		#region Mouse handling
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (child != null) 
				if (child.MouseIsIn (e.Position)) 
					child.onMouseMove (sender, e);
			
		}
		#endregion

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
                subTree.Read(); //move to first child

                if (!subTree.IsStartElement())
                    return;

                Type t = Type.GetType("go." + subTree.Name);
                GraphicObject go = (GraphicObject)Activator.CreateInstance(t);                                

                (go as IXmlSerializable).ReadXml(subTree);
                
                setChild(go);

                subTree.Read();

                if (!subTree.IsStartElement())
                    return;

            }
        }
        public override void WriteXml(System.Xml.XmlWriter writer)
        {
            base.WriteXml(writer);

            if (child == null)
                return;

            writer.WriteStartElement(child.GetType().Name);
            (child as IXmlSerializable).WriteXml(writer);
            writer.WriteEndElement();
        }
    
		#endregion
	}
}


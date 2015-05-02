using System;
using System.Xml.Serialization;
using System.Reflection;
using OpenTK.Input;
using System.ComponentModel;
using System.Linq;

namespace go
{
    public class Container : GraphicObject, IXmlSerializable
    {
		#region CTOR
		public Container()
			: base()
		{
		}
		public Container(Rectangle _bounds)
			: base(_bounds)
		{
		}
		#endregion

		public GraphicObject child;

        public T setChild<T>(T _child)
        {

			if (child != null) {
				this.RegisterForLayouting ((int)LayoutingType.Sizing);
				child.Parent = null;
			}

            child = _child as GraphicObject;

			if (child != null) {
				child.Parent = this;
				child.RegisterForLayouting ((int)LayoutingType.Sizing);
			}

            return (T)_child;
        }

		#region GraphicObject Overrides
		//check if not causing problems
		[XmlAttributeAttribute()][DefaultValue(true)]
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}

		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			return child == null ? null : child.FindByName (nameToFind);
		}
		public override bool Contains (GraphicObject goToFind)
		{
			return child == goToFind ? true : 
				child == null ? false : child.Contains(goToFind);
		}
		protected override Size measureRawSize ()
		{			
			return child == null ? Bounds.Size : new Size(child.Slot.Width + 2 * (Margin),child.Slot.Height + 2 * (Margin));
		}

		protected override void OnLayoutChanges (LayoutingType layoutType)
		{
			switch (layoutType) {
			case LayoutingType.Width:				
				base.OnLayoutChanges (layoutType);
				if (child != null) {
					if (child.getBounds ().Width == 0)
						child.RegisterForLayouting ((int)LayoutingType.Width);
					else
						child.RegisterForLayouting ((int)LayoutingType.X);
				}
				break;
			case LayoutingType.Height:
				base.OnLayoutChanges (layoutType);
				if (child != null) {
					if (child.getBounds ().Height == 0)
						child.RegisterForLayouting ((int)LayoutingType.Height);
					else
						child.RegisterForLayouting ((int)LayoutingType.Y);
				}
				break;
			}							
		}

//		public override void RegisterForLayouting(int layoutType)
//		{
//			Interface.LayoutingQueue.RemoveAll (lq => lq.GraphicObject == this && (layoutType & (int)lq.LayoutType) > 0);
//
//			if ((layoutType & (int)LayoutingType.Width) > 0) {
//				if (Bounds.Width == 0) //stretch in parent
//					Interface.LayoutingQueue.EnqueueAfterParentSizing (LayoutingType.Width, this);
//				else //fit ou fixed
//					Interface.LayoutingQueue.Enqueue (LayoutingType.Width, this);
//			}
//
//			if ((layoutType & (int)LayoutingType.Height) > 0) {
//				if (Bounds.Height == 0) //stretch in parent
//					Interface.LayoutingQueue.EnqueueAfterParentSizing (LayoutingType.Height, this);
//				else//fit ou fixed
//					Interface.LayoutingQueue.Enqueue (LayoutingType.Height, this);
//			}
//
//			if ((layoutType & (int)LayoutingType.X) > 0)
//				//for x positionning, sizing of parent and this have to be done
//				Interface.LayoutingQueue.EnqueueAfterThisAndParentSizing (LayoutingType.X, this);
//
//			if ((layoutType & (int)LayoutingType.Y) > 0)
//				//for x positionning, sizing of parent and this have to be done
//				Interface.LayoutingQueue.EnqueueAfterThisAndParentSizing (LayoutingType.Y, this);
//
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
		#endregion

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


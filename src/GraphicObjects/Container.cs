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

		public override void UpdateLayout (LayoutingType layoutType)
		{
			{			
				switch (layoutType) {
				case LayoutingType.X:
					if (Bounds.X == 0) {
						switch (HorizontalAlignment) {
						case HorizontalAlignment.Left:
							Slot.X = 0;
							break;
						case HorizontalAlignment.Right:
							Slot.X = Parent.ClientRectangle.Width - Slot.Width;
							break;
						case HorizontalAlignment.Center:
							Slot.X = Parent.ClientRectangle.Width / 2 - Slot.Width / 2;
							break;
						}
					} else
						Slot.X = Bounds.X;
					if (LastSlots.X == Slot.X)
						break;
					//register layouting here for objects depending on this.x
					LastSlots.X = Slot.X;
					break;
				case LayoutingType.Y:
					if (Bounds.Y == 0) {
						switch (VerticalAlignment) {
						case VerticalAlignment.Top:
							Slot.Y = 0;
							break;
						case VerticalAlignment.Bottom:
							Slot.Y = Parent.ClientRectangle.Height - Slot.Height;
							break;
						case VerticalAlignment.Center:
							Slot.Y = Parent.ClientRectangle.Height / 2 - Slot.Height / 2;
							break;
						}
					}else
						Slot.Y = Bounds.Y;
					if (LastSlots.Y == Slot.Y)
						break;
					//register layouting here for objects depending on this.x
					LastSlots.Y = Slot.Y;
					break;
				case LayoutingType.Width:				
					if (Width > 0)
						Slot.Width = Width;
					else if (Width < 0)
						Slot.Width = measureRawSize ().Width;
					else
						Slot.Width = Parent.ClientRectangle.Width;

					if (LastSlots.Width == Slot.Width)
						break;

					if (Parent.getBounds ().Width < 0)
						this.Parent.RegisterForLayouting ((int)LayoutingType.Width);
					else if (Width != 0) //update position in parent
						this.RegisterForLayouting ((int)LayoutingType.X);

					if (child != null) {
						if (child.getBounds ().Width == 0)
							child.RegisterForLayouting ((int)LayoutingType.Width);
						else
							child.RegisterForLayouting ((int)LayoutingType.X);
					}
					LastSlots.Width = Slot.Width;
					break;
				case LayoutingType.Height:
					if (Height > 0)
						Slot.Height = Height;
					else if (Height < 0)
						Slot.Height = measureRawSize ().Height;
					else
						Slot.Height = Parent.ClientRectangle.Height;

					if (LastSlots.Height == Slot.Height)
						break;

					if (Parent.getBounds().Height < 0)
						this.Parent.RegisterForLayouting((int)LayoutingType.Height);
					else if (Height != 0) //update position in parent
						this.RegisterForLayouting ((int)LayoutingType.Y);

					if (child != null) {
						if (child.getBounds ().Height == 0)
							child.RegisterForLayouting ((int)LayoutingType.Height);
						else
							child.RegisterForLayouting ((int)LayoutingType.Y);
					}

					LastSlots.Height = Slot.Height;
					break;
				}

				//if no layouting remains in queue for item, registre for redraw
				if (Interface.LayoutingQueue.Where (lq => lq.GraphicObject == this).Count () <= 0)
					this.RegisterForRedraw ();
			}		
		}

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


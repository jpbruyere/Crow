using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using Cairo;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	public class Group : GraphicObject, IXmlSerializable
    {
		#region CTOR
		public Group()
			: base(){}
		#endregion

		#region EVENT HANDLERS
		public event EventHandler<EventArgs> ChildrenCleared;
		#endregion

		internal int maxChildrenWidth = 0;
		internal int maxChildrenHeight = 0;
		internal GraphicObject largestChild = null;
		internal GraphicObject tallestChild = null;

        bool _multiSelect = false;
		List<GraphicObject> children = new List<GraphicObject>();

        public GraphicObject activeWidget;

        public virtual List<GraphicObject> Children {
			get { return children; }
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
        public bool MultiSelect
        {
            get { return _multiSelect; }
            set { _multiSelect = value; }
        }
			
			
        public virtual T AddChild<T>(T child)
        {
			GraphicObject g = child as GraphicObject;
            children.Add(g);
            g.Parent = this as GraphicObject;            
			g.RegisterForLayouting (LayoutingType.Sizing);
			g.LayoutChanged += OnChildLayoutChanges;
            return (T)child;
        }
        public virtual void RemoveChild(GraphicObject child)        
		{
			child.LayoutChanged -= OnChildLayoutChanges;
			child.ClearBinding ();
			child.Parent = null;
            children.Remove(child);
			this.RegisterForLayouting (LayoutingType.Sizing);
        }
		public virtual void ClearChildren()
		{
			while(children.Count > 0){
				GraphicObject g = children[children.Count-1];
				g.LayoutChanged -= OnChildLayoutChanges;
				g.ClearBinding ();
				g.Parent = null;
				children.RemoveAt(children.Count-1);
			}
			this.RegisterForLayouting (LayoutingType.Sizing);
			ChildrenCleared.Raise (this, new EventArgs ());
		}
		public void putWidgetOnTop(GraphicObject w)
		{
			if (children.Contains(w))
			{
				children.Remove(w);
				children.Add(w);
			}
		}
		public void putWidgetOnBottom(GraphicObject w)
		{
			if (children.Contains(w))
			{
				children.Remove(w);
				children.Insert(0, w);
			}
		}

		#region GraphicObject overrides
		[XmlIgnore]public override bool DrawingIsValid {
			get {
				if (!base.DrawingIsValid)
					return false;
				foreach (GraphicObject g in children) {
					if (!g.DrawingIsValid)
						return false;
				}
				return true;
			}
		}
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			foreach (GraphicObject w in children)
				w.ResolveBindings ();
		}
		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			foreach (GraphicObject w in children) {
				GraphicObject r = w.FindByName (nameToFind);
				if (r != null)
					return r;
			}
			return null;
		}
		public override bool Contains (GraphicObject goToFind)
		{
			foreach (GraphicObject w in children) {
				if (w == goToFind)
					return true;
				if (w.Contains (goToFind))
					return true;
			}
			return false;
		}
		protected override Size measureRawSize ()
		{
			return new Size(maxChildrenWidth + 2 * Margin, maxChildrenHeight + 2 * Margin);
		}
			
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			//position smaller objects in group when group size is fit
			switch (layoutType) {
			case LayoutingType.Width:
				foreach (GraphicObject c in children) {
					if (!c.Visible)
						continue;					
					c.RegisterForLayouting (LayoutingType.X | LayoutingType.Width);
				}
				break;
			case LayoutingType.Height:
				foreach (GraphicObject c in children) {
					if (!c.Visible)
						continue;
					c.RegisterForLayouting (LayoutingType.Y | LayoutingType.Height);				}
				break;
			}
		}
		public virtual void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			GraphicObject g = sender as GraphicObject;
			switch (arg.LayoutType) {
			case LayoutingType.X:
				break;
			case LayoutingType.Y:
				break;
			case LayoutingType.Width:
				if (g.Slot.Width > maxChildrenWidth) {
					maxChildrenWidth = g.Slot.Width;
					largestChild = g;
					if (this.Bounds.Width < 0)
						this.RegisterForLayouting (LayoutingType.Width);
				} else if (g == largestChild) {
					//search for the new largest child
					largestChild = null;
					maxChildrenWidth = 0;
					for (int i = 0; i < children.Count; i++) {
						if (children [i].Slot.Width > maxChildrenWidth) {
							maxChildrenWidth = children [i].Slot.Width;
							largestChild = children [i];
						}
					}
					if (this.Bounds.Width < 0)
						this.RegisterForLayouting (LayoutingType.Width);
				}
				break;
			case LayoutingType.Height:
				if (g.Slot.Height > maxChildrenHeight) {
					maxChildrenHeight = g.Slot.Height;
					tallestChild = g;
					if (this.Bounds.Height < 0)
						this.RegisterForLayouting (LayoutingType.Height);
				} else if (g == tallestChild) {
					//search for the new tallest child
					tallestChild = null;
					maxChildrenHeight = 0;
					for (int i = 0; i < children.Count; i++) {
						if (children [i].Slot.Height > maxChildrenHeight) {
							maxChildrenHeight = children [i].Slot.Height;
							tallestChild = children [i];
						}
					}
					if (this.Bounds.Height < 0)
						this.RegisterForLayouting (LayoutingType.Height);
				}
				break;
			}
		}

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			foreach (GraphicObject g in children) {
				g.Paint (ref gr);
			}
		}
		protected override void UpdateCache (Context ctx)
		{
			Rectangle rb = Slot + Parent.ClientRectangle.Position;

			using (ImageSurface cache = new ImageSurface (bmp, Format.Argb32, Slot.Width, Slot.Height, 4 * Slot.Width)) {
				Context gr = new Context (cache);

				//Clipping.clearAndClip (gr);
				base.onDraw (gr);

				foreach (GraphicObject c in children) {
					if (!c.Visible)
						continue;
					c.Paint (ref gr);						
				}

				#if DEBUG_CLIP_RECTANGLE
				Clipping.stroke (gr, Color.Amaranth.AdjustAlpha (0.8));
				#endif

				gr.Dispose ();

				ctx.SetSourceSurface (cache, rb.X, rb.Y);
				ctx.Paint ();
			}
		}
		#endregion

	
		#region Mouse handling
		public override void checkHoverWidget (OpenTK.Input.MouseMoveEventArgs e)
		{
			if (HostContainer.hoverWidget != this) {
				HostContainer.hoverWidget = this;
				onMouseEnter (this, e);
			}
			foreach (GraphicObject g in children)
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

                    Type t = Type.GetType("Crow." + subTree.Name);
					if (t == null)
						throw new Exception ("Crow." + subTree.Name + " type not found");
                    GraphicObject go = (GraphicObject)Activator.CreateInstance(t);
                    (go as IXmlSerializable).ReadXml(subTree);                    
                    AddChild(go);
                }
            }
        }
        public override void WriteXml(System.Xml.XmlWriter writer)
        {
            base.WriteXml(writer);

            foreach (GraphicObject go in children)
            {
                writer.WriteStartElement(go.GetType().Name);
                (go as IXmlSerializable).WriteXml(writer);
                writer.WriteEndElement();
            }
        }
    
		#endregion

		public override void ClearBinding(){
			foreach (GraphicObject c in children)
				c.ClearBinding ();
			base.ClearBinding ();
		}
	}
}

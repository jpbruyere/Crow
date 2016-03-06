using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Cairo;
using OpenTK.Input;
using System.Diagnostics;


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
            Children.Add(g);
            g.Parent = this as GraphicObject;            
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			g.LayoutChanged += OnChildLayoutChanges;
            return (T)child;
        }
        public virtual void RemoveChild(GraphicObject child)        
		{
			child.LayoutChanged -= OnChildLayoutChanges;
			child.ClearBinding ();
			//child.Parent = null;
            Children.Remove(child);

			if (child == largestChild)
				searchLargestChild ();
			if (child == tallestChild)
				searchTallestChild ();
			
			this.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
        }
		public virtual void ClearChildren()
		{
			while(Children.Count > 0){
				GraphicObject g = Children[Children.Count-1];
				g.LayoutChanged -= OnChildLayoutChanges;
				g.ClearBinding ();
				g.Parent = null;
				Children.RemoveAt(Children.Count-1);
			}

			resetChildrenMaxSize ();

			this.RegisterForLayouting (LayoutingType.Sizing);
			ChildrenCleared.Raise (this, new EventArgs ());
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
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			foreach (GraphicObject w in Children)
				w.ResolveBindings ();
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
		protected override int measureRawSize (LayoutingType lt)
		{
			if (Children.Count == 0)
				return base.measureRawSize (lt);
			
			if (lt == LayoutingType.Width) {
				if (largestChild == null)
					searchLargestChild ();
				if (largestChild == null){
					//if still null, not possible to determine a width
					//because all children are stretched, force first one to fit
					Children[0].Width = -1;
					return -1;//cancel actual sizing to let child computation take place
				}
				return maxChildrenWidth + 2 * Margin;
			}else{
				if (tallestChild == null)
					searchTallestChild ();
				if (tallestChild == null) {
					Children[0].Height = -1;
					return -1;
				}
				return maxChildrenHeight + 2 * Margin;
			}
		}
			
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			//position smaller objects in group when group size is fit
			switch (layoutType) {
			case LayoutingType.Width:
				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;					
					c.RegisterForLayouting (LayoutingType.X | LayoutingType.Width);
				}
				break;
			case LayoutingType.Height:
				foreach (GraphicObject c in Children) {
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
			case LayoutingType.Width:
				if (g.Slot.Width > maxChildrenWidth) {
					maxChildrenWidth = g.Slot.Width;
					largestChild = g;
					if (this.Bounds.Width < 0)
						this.RegisterForLayouting (LayoutingType.Width);
				} else if (g == largestChild) {

					largestChild = null;
					maxChildrenWidth = 0;

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

					tallestChild = null;
					maxChildrenHeight = 0;

					if (this.Bounds.Height < 0)
						this.RegisterForLayouting (LayoutingType.Height);
				}
				break;
			}
		}

		//TODO: x,y position should be taken in account for computation of width and height
		void resetChildrenMaxSize(){
			largestChild = null;
			tallestChild = null;
			maxChildrenWidth = 0;
			maxChildrenHeight = 0;
		}
		void searchLargestChild(){
			#if DEBUG_LAYOUTING
			Debug.WriteLine("\tSearch largest child");
			#endif
			largestChild = null;
			maxChildrenWidth = 0;
			for (int i = 0; i < Children.Count; i++) {
				if (!Children [i].Visible)
					continue;
				if (children [i].RegisteredLayoutings.HasFlag (LayoutingType.Width))
					continue;
				if (Children [i].Slot.Width > maxChildrenWidth) {
					maxChildrenWidth = Children [i].Slot.Width;
					largestChild = Children [i];
				}
			}
		}
		void searchTallestChild(){
			#if DEBUG_LAYOUTING
			Debug.WriteLine("\tSearch tallest child");
			#endif
			tallestChild = null;
			maxChildrenHeight = 0;
			for (int i = 0; i < Children.Count; i++) {
				if (!Children [i].Visible)
					continue;
				if (children [i].RegisteredLayoutings.HasFlag (LayoutingType.Height))
					continue;
				if (Children [i].Slot.Height > maxChildrenHeight) {
					maxChildrenHeight = Children [i].Slot.Height;
					tallestChild = Children [i];
				}
			}
		}

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.Save ();
			//clip to client zone
			CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
			gr.Clip ();

			foreach (GraphicObject g in Children) {
				g.Paint (ref gr);
			}
			gr.Restore ();
		}
		protected override void UpdateCache (Context ctx)
		{
			Rectangle rb = Slot + Parent.ClientRectangle.Position;

			using (ImageSurface cache = new ImageSurface (bmp, Format.Argb32, Slot.Width, Slot.Height, 4 * Slot.Width)) {
				Context gr = new Context (cache);

				if (Clipping.count > 0) {
					Clipping.clearAndClip (gr);
					base.onDraw (gr);

					//clip to client zone
					CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
					gr.Clip ();

					foreach (GraphicObject c in Children) {
						if (!c.Visible)
							continue;
						if (Clipping.intersect(c.Slot + ClientRectangle.Position))
							c.Paint (ref gr);
					}

					#if DEBUG_CLIP_RECTANGLE
					Clipping.stroke (gr, Color.Amaranth.AdjustAlpha (0.8));
					#endif
				}
				gr.Dispose ();

				ctx.SetSourceSurface (cache, rb.X, rb.Y);
				ctx.Paint ();
			}
			Clipping.Reset();
		}
		#endregion

	
		#region Mouse handling
		public override void checkHoverWidget (MouseMoveEventArgs e)
		{
			if (Interface.CurrentInterface.hoverWidget != this) {
				Interface.CurrentInterface.hoverWidget = this;
				onMouseEnter (this, e);
			}
			for (int i = Children.Count - 1; i >= 0; i--) {
				if (Children[i].MouseIsIn(e.Position))
				{
					Children[i].checkHoverWidget (e);
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

            foreach (GraphicObject go in Children)
            {
                writer.WriteStartElement(go.GetType().Name);
                (go as IXmlSerializable).WriteXml(writer);
                writer.WriteEndElement();
            }
        }
    
		#endregion

		public override void ClearBinding(){
			foreach (GraphicObject c in Children)
				c.ClearBinding ();
			base.ClearBinding ();
		}
	}
}

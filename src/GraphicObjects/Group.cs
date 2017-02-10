using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Cairo;
using OpenTK.Input;
using System.Diagnostics;
using System.Reflection;


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

		internal GraphicObject largestChild = null;
		internal GraphicObject tallestChild = null;

        bool _multiSelect = false;
		List<GraphicObject> children = new List<GraphicObject>();

        public virtual List<GraphicObject> Children {
			get { return children; }
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
        public bool MultiSelect
        {
            get { return _multiSelect; }
            set { _multiSelect = value; }
        }
		public virtual void AddChild(GraphicObject g){
			lock (children) {
				g.Parent = this;
				Children.Add (g);
			}
			g.RegisteredLayoutings = LayoutingType.None;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			g.LayoutChanged += OnChildLayoutChanges;
		}
        public virtual void RemoveChild(GraphicObject child)        
		{
			child.LayoutChanged -= OnChildLayoutChanges;
			//child.Parent = null;
			lock (children)
            	Children.Remove(child);

			if (child == largestChild && Width == Measure.Fit)
				searchLargestChild ();
			if (child == tallestChild && Height == Measure.Fit)
				searchTallestChild ();
			
			this.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
        }
		public virtual void ClearChildren()
		{
			while(Children.Count > 0){
				GraphicObject g = Children[Children.Count-1];
				g.LayoutChanged -= OnChildLayoutChanges;
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
		public override void OnDataSourceChanged (object sender, DataSourceChangeEventArgs e)
		{
			base.OnDataSourceChanged (this, e);
			foreach (GraphicObject g in children)
				if (g.localDataSourceIsNull & g.localLogicalParentIsNull)
					g.OnDataSourceChanged (sender, e);
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
			if (Children.Count > 0) {
				if (lt == LayoutingType.Width) {
					if (largestChild == null)
						searchLargestChild ();
					if (largestChild == null) {
						//if still null, not possible to determine a width
						//because all children are stretched, force first one to fit
						Children [0].Width = Measure.Fit;
						return -1;//cancel actual sizing to let child computation take place
					}
				} else {
					if (tallestChild == null)
						searchTallestChild ();
					if (tallestChild == null) {
						Children [0].Height = Measure.Fit;
						return -1;
					}
				}
			}
			return base.measureRawSize (lt);
		}
			
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			//position smaller objects in group when group size is fit
			switch (layoutType) {
			case LayoutingType.Width:
				foreach (GraphicObject c in Children){
					if (c.Width.Units == Unit.Percent)
						c.RegisterForLayouting (LayoutingType.Width);
					else
						c.RegisterForLayouting (LayoutingType.X);
				}
				break;
			case LayoutingType.Height:
				foreach (GraphicObject c in Children) {
					if (c.Height.Units == Unit.Percent)
						c.RegisterForLayouting (LayoutingType.Height);
					else
						c.RegisterForLayouting (LayoutingType.Y);
				}
				break;
			}
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.Save ();

			if (ClipToClientRect) {
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			lock (children) {
				foreach (GraphicObject g in Children) {
					g.Paint (ref gr);
				}
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

					lock (Children) {
						foreach (GraphicObject c in Children) {
							if (!c.Visible)
								continue;
							if (Clipping.intersect (c.Slot + ClientRectangle.Position))
								c.Paint (ref gr);
						}
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

		public virtual void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			GraphicObject g = sender as GraphicObject;

			switch (arg.LayoutType) {
			case LayoutingType.Width:
				if (Width != Measure.Fit)
					return;
				if (g.Slot.Width > contentSize.Width) {
					largestChild = g;
					contentSize.Width = g.Slot.Width;
				} else if (g == largestChild)
					searchLargestChild ();

				this.RegisterForLayouting (LayoutingType.Width);
				break;
			case LayoutingType.Height:
				if (Height != Measure.Fit)
					return;
				if (g.Slot.Height > contentSize.Height) {
					tallestChild = g;
					contentSize.Height = g.Slot.Height;
				} else if (g == tallestChild)
					searchTallestChild ();

				this.RegisterForLayouting (LayoutingType.Height);
				break;
			}
		}
		//TODO: x,y position should be taken in account for computation of width and height
		void resetChildrenMaxSize(){
			largestChild = null;
			tallestChild = null;
			contentSize = 0;
		}
		void searchLargestChild(){
			#if DEBUG_LAYOUTING
			Debug.WriteLine("\tSearch largest child");
			#endif
			largestChild = null;
			contentSize.Width = 0;
			for (int i = 0; i < Children.Count; i++) {
				if (!Children [i].Visible)
					continue;
				if (children [i].RegisteredLayoutings.HasFlag (LayoutingType.Width))
					continue;
				if (Children [i].Slot.Width > contentSize.Width) {
					contentSize.Width = Children [i].Slot.Width;
					largestChild = Children [i];
				}
			}
		}
		void searchTallestChild(){
			#if DEBUG_LAYOUTING
			Debug.WriteLine("\tSearch tallest child");
			#endif
			tallestChild = null;
			contentSize.Height = 0;
			for (int i = 0; i < Children.Count; i++) {
				if (!Children [i].Visible)
					continue;
				if (children [i].RegisteredLayoutings.HasFlag (LayoutingType.Height))
					continue;
				if (Children [i].Slot.Height > contentSize.Height) {
					contentSize.Height = Children [i].Slot.Height;
					tallestChild = Children [i];
				}
			}
		}

	
		#region Mouse handling
		public override void checkHoverWidget (MouseMoveEventArgs e)
		{
			if (CurrentInterface.HoverWidget != this) {
				CurrentInterface.HoverWidget = this;
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
					if (t == null) {
						Assembly a = Assembly.GetEntryAssembly ();
						foreach (Type expT in a.GetExportedTypes ()) {
							if (expT.Name == subTree.Name) {
								t = expT;
								break;
							}
						}
					}
					if (t == null)
						throw new Exception (subTree.Name + " type not found");
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

	}
}

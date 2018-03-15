//
// Group.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Cairo;
using System.Diagnostics;
using System.Reflection;
using System.Threading;


namespace Crow
{
	public class Group : GraphicObject
    {
		#if DESIGN_MODE
		public override bool FindByDesignID(string designID, out GraphicObject go){
			go = null;
			if (base.FindByDesignID (designID, out go))
				return true;
			childrenRWLock.EnterReadLock ();
			foreach (GraphicObject w in Children) {
				if (!w.FindByDesignID (designID, out go))
					continue;
				childrenRWLock.ExitReadLock ();
				return true;
			}
			childrenRWLock.ExitReadLock ();
			return false;
		}
		public override void getIML (System.Xml.XmlDocument doc, System.Xml.XmlNode parentElem)
		{
			if (this.design_isTGItem)
				return;
			base.getIML (doc, parentElem);
			foreach (GraphicObject g in Children) {
				g.getIML (doc, parentElem.LastChild);	
			}
		}
		#endif

		protected ReaderWriterLockSlim childrenRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		#region CTOR
		public Group () : base() {}
		public Group(Interface iface) : base(iface){}
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
			childrenRWLock.EnterWriteLock();

			g.Parent = this;
			Children.Add (g);

			childrenRWLock.ExitWriteLock();

			g.RegisteredLayoutings = LayoutingType.None;
			g.LayoutChanged += OnChildLayoutChanges;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
		}
        public virtual void RemoveChild(GraphicObject child)
		{
			child.LayoutChanged -= OnChildLayoutChanges;
			//check if HoverWidget is removed from Tree
			if (IFace.HoverWidget != null) {
				if (this.Contains (IFace.HoverWidget))
					IFace.HoverWidget = null;
			}

			childrenRWLock.EnterWriteLock ();

			Children.Remove(child);
			child.Parent = null;

			childrenRWLock.ExitWriteLock ();

			if (child == largestChild && Width == Measure.Fit)
				searchLargestChild ();
			if (child == tallestChild && Height == Measure.Fit)
				searchTallestChild ();

			this.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);

		}
        public virtual void DeleteChild(GraphicObject child)
		{
			RemoveChild (child);
			child.Dispose ();
        }
		public virtual void InsertChild (int idx, GraphicObject g) {
			childrenRWLock.EnterWriteLock ();
				
			g.Parent = this;
			Children.Insert (idx, g);

			childrenRWLock.ExitWriteLock ();

			g.RegisteredLayoutings = LayoutingType.None;
			g.LayoutChanged += OnChildLayoutChanges;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
		}
		public virtual void RemoveChild (int idx) {
			RemoveChild (children[idx]);
		}
		public virtual void DeleteChild (int idx) {
			DeleteChild (children[idx]);
		}
		public virtual void ClearChildren()
		{
			childrenRWLock.EnterWriteLock ();

			while (Children.Count > 0) {
				GraphicObject g = Children [Children.Count - 1];
				g.LayoutChanged -= OnChildLayoutChanges;
				Children.RemoveAt (Children.Count - 1);
				g.Dispose ();
			}

			childrenRWLock.ExitWriteLock ();

			resetChildrenMaxSize ();

			this.RegisterForLayouting (LayoutingType.Sizing);
			ChildrenCleared.Raise (this, new EventArgs ());
		}
		public override void OnDataSourceChanged (object sender, DataSourceChangeEventArgs e)
		{
			base.OnDataSourceChanged (this, e);

			childrenRWLock.EnterReadLock ();
			foreach (GraphicObject g in Children) {
				if (g.localDataSourceIsNull & g.localLogicalParentIsNull)
					g.OnDataSourceChanged (g, e);	
			}
			childrenRWLock.ExitReadLock ();
		}

		public void putWidgetOnTop(GraphicObject w)
		{
			if (Children.Contains(w))
			{
				childrenRWLock.EnterWriteLock ();

				Children.Remove (w);
				Children.Add (w);

				childrenRWLock.ExitWriteLock ();
			}
		}
		public void putWidgetOnBottom(GraphicObject w)
		{
			if (Children.Contains(w))
			{
				childrenRWLock.EnterWriteLock ();

				Children.Remove (w);
				Children.Insert (0, w);

				childrenRWLock.ExitWriteLock ();
			}
		}

		#region GraphicObject overrides

		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;
			GraphicObject tmp = null;

			childrenRWLock.EnterReadLock ();

			foreach (GraphicObject w in Children) {
				tmp = w.FindByName (nameToFind);
				if (tmp != null)
					break;
			}

			childrenRWLock.ExitReadLock ();

			return tmp;
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
						//Children [0].Width = Measure.Fit;
						return -1;//cancel actual sizing to let child computation take place
					}
				} else {
					if (tallestChild == null)
						searchTallestChild ();
					if (tallestChild == null) {
						//Children [0].Height = Measure.Fit;
						return -1;
					}
				}
			}
			return base.measureRawSize (lt);
		}

		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			childrenRWLock.EnterReadLock ();
			//position smaller objects in group when group size is fit
			switch (layoutType) {
			case LayoutingType.Width:
				foreach (GraphicObject c in Children) {
					if (c.Width.IsRelativeToParent)
						c.RegisterForLayouting (LayoutingType.Width);
					else
						c.RegisterForLayouting (LayoutingType.X);
				}
				break;
			case LayoutingType.Height:
				foreach (GraphicObject c in Children) {
					if (c.Height.IsRelativeToParent)
						c.RegisterForLayouting (LayoutingType.Height);
					else
						c.RegisterForLayouting (LayoutingType.Y);
				}
				break;
			}
			childrenRWLock.ExitReadLock ();
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

			childrenRWLock.EnterReadLock ();

			foreach (GraphicObject g in Children) {
				g.Paint (ref gr);
			}

			childrenRWLock.ExitReadLock ();
			gr.Restore ();
		}
		protected override void UpdateCache (Context ctx)
		{
			Rectangle rb = Slot + Parent.ClientRectangle.Position;


			Context gr = new Context (bmp);

			if (!Clipping.IsEmpty) {
				for (int i = 0; i < Clipping.NumRectangles; i++)
					gr.Rectangle(Clipping.GetRectangle(i));
				gr.ClipPreserve();
				gr.Operator = Operator.Clear;
				gr.Fill();
				gr.Operator = Operator.Over;

				base.onDraw (gr);

				if (ClipToClientRect) {
					CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
					gr.Clip ();
				}

				childrenRWLock.EnterReadLock ();

				foreach (GraphicObject c in Children) {
					if (!c.Visible)
						continue;
					if (Clipping.Contains (c.Slot + ClientRectangle.Position) == RegionOverlap.Out)
						continue;
					c.Paint (ref gr);
				}

				childrenRWLock.ExitReadLock ();

				#if DEBUG_CLIP_RECTANGLE
				Clipping.stroke (gr, Color.Amaranth.AdjustAlpha (0.8));
				#endif
			}
			gr.Dispose ();

			ctx.SetSourceSurface (bmp, rb.X, rb.Y);
			ctx.Paint ();

			Clipping.Dispose();
			Clipping = new Region ();
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
			childrenRWLock.EnterReadLock ();
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
			childrenRWLock.ExitReadLock ();
		}
		void searchTallestChild(){
			#if DEBUG_LAYOUTING
			Debug.WriteLine("\tSearch tallest child");
			#endif
			tallestChild = null;
			contentSize.Height = 0;
			childrenRWLock.EnterReadLock ();
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
			childrenRWLock.ExitReadLock ();
		}


		#region Mouse handling
		public override void checkHoverWidget (MouseMoveEventArgs e)
		{
			if (IFace.HoverWidget != this) {
				IFace.HoverWidget = this;
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
//		public override bool PointIsIn (ref Point m)
//		{
//			if (!base.PointIsIn (ref m))
//				return false;
//			if (CurrentInterface.HoverWidget == this)
//				return true;
//			lock (Children) {
//				for (int i = Children.Count - 1; i >= 0; i--) {
//					if (Children [i].Slot.ContainsOrIsEqual (m) && !(bool)CurrentInterface.HoverWidget?.IsOrIsInside(Children[i])) {						
//						return false;
//					}
//				}
//			}
//			return true;
//		}
		#endregion

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				foreach (GraphicObject c in children)
					c.Dispose ();
			}
			base.Dispose (disposing);
		}
	}
}

// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Crow.Cairo;
using System.Threading;

using static Crow.Logger;

namespace Crow
{
	public class Group : Widget
    {
		#if DESIGN_MODE
		public override bool FindByDesignID(string designID, out Widget go){
			go = null;
			if (base.FindByDesignID (designID, out go))
				return true;
			childrenRWLock.EnterReadLock ();
			foreach (Widget w in Children) {
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
			foreach (Widget g in Children) {
				g.getIML (doc, parentElem.LastChild);	
			}
		}
		#endif

		protected ReaderWriterLockSlim childrenRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		#region CTOR
		public Group () {}
		public Group(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region EVENT HANDLERS
		public event EventHandler<EventArgs> ChildrenCleared;
		#endregion

		internal Widget largestChild = null;
		internal Widget tallestChild = null;

        bool _multiSelect = false;
		List<Widget> children = new List<Widget>();

        public virtual List<Widget> Children {
			get { return children; }
		}
		[DefaultValue(false)]
        public bool MultiSelect
        {
            get { return _multiSelect; }
            set { _multiSelect = value; }
        }
		public virtual void AddChild(Widget g){
			if (disposed) {
				DbgLogger.AddEvent (DbgEvtType.AlreadyDisposed | DbgEvtType.GOAddChild);
				return;
			}

			childrenRWLock.EnterWriteLock();

			g.Parent = this;
			Children.Add (g);

			childrenRWLock.ExitWriteLock();

			//g.RegisteredLayoutings = LayoutingType.None;
			g.LayoutChanged += OnChildLayoutChanges;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
		}
        public virtual void RemoveChild(Widget child)
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
			child.LogicalParent = null;

			childrenRWLock.ExitWriteLock ();

			if (child == largestChild && Width == Measure.Fit)
				searchLargestChild ();
			if (child == tallestChild && Height == Measure.Fit)
				searchTallestChild ();

			this.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);

		}
        public virtual void DeleteChild(Widget child)
		{
			RemoveChild (child);
			child.Dispose ();
        }
		public virtual void InsertChild (int idx, Widget g) {
			if (disposed) {
				DbgLogger.AddEvent (DbgEvtType.AlreadyDisposed | DbgEvtType.GOAddChild);
				return;
			}
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
				Widget g = Children [Children.Count - 1];
				g.LayoutChanged -= OnChildLayoutChanges;
				Children.RemoveAt (Children.Count - 1);
				g.Dispose ();
			}

			childrenRWLock.ExitWriteLock ();

			resetChildrenMaxSize ();

			RegisterForLayouting (LayoutingType.Sizing);
			ChildrenCleared.Raise (this, new EventArgs ());
		}
		public override void OnDataSourceChanged (object sender, DataSourceChangeEventArgs e)
		{
			base.OnDataSourceChanged (this, e);

			childrenRWLock.EnterReadLock ();
			foreach (Widget g in Children) {
				if (g.localDataSourceIsNull & g.localLogicalParentIsNull)
					g.OnDataSourceChanged (g, e);	
			}
			childrenRWLock.ExitReadLock ();
		}

		public void putWidgetOnTop(Widget w)
		{
			if (Children.Contains(w))
			{
				childrenRWLock.EnterWriteLock ();

				Children.Remove (w);
				Children.Add (w);

				childrenRWLock.ExitWriteLock ();
			}
		}
		public void putWidgetOnBottom(Widget w)
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

		public override Widget FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;
			Widget tmp = null;

			childrenRWLock.EnterReadLock ();

			foreach (Widget w in Children) {
				tmp = w.FindByName (nameToFind);
				if (tmp != null)
					break;
			}

			childrenRWLock.ExitReadLock ();

			return tmp;
		}
		public override Widget FindByType<T> ()
		{
			if (this is T)
				return this;
			Widget tmp = null;

			childrenRWLock.EnterReadLock ();

			foreach (Widget w in Children) {
				tmp = w.FindByType<T> ();
				if (tmp != null)
					break;
			}

			childrenRWLock.ExitReadLock ();

			return tmp;
		}
		public override bool Contains (Widget goToFind)
		{
			foreach (Widget w in Children) {
				if (w == goToFind)
					return true;
				if (w.Contains (goToFind))
					return true;
			}
			return false;
		}
		public override int measureRawSize (LayoutingType lt)
		{
			if (Children.Count > 0) {
				if (lt == LayoutingType.Width) {
					if (largestChild == null)
						searchLargestChild ();
					if (largestChild == null)
						searchLargestChild (true);
				} else {
					if (tallestChild == null)
						searchTallestChild ();
					if (tallestChild == null)
						searchTallestChild (true);
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
				foreach (Widget c in Children) {
					if (c.Width.IsRelativeToParent)
						c.RegisterForLayouting (LayoutingType.Width);
					else
						c.RegisterForLayouting (LayoutingType.X);
				}
				break;
			case LayoutingType.Height:
				foreach (Widget c in Children) {
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

			for (int i = 0; i < Children.Count; i++) 
				Children[i].Paint (ref gr);			

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

				foreach (Widget c in Children) {
					if (!c.Visible)
						continue;
					if (Clipping.Contains (c.Slot + ClientRectangle.Position) == RegionOverlap.Out)
						continue;
					c.Paint (ref gr);
				}

				childrenRWLock.ExitReadLock ();

				#if DEBUG_CLIP_RECTANGLE
				/*gr.LineWidth = 1;
				gr.SetSourceColor(Color.DarkMagenta.AdjustAlpha (0.8));
				for (int i = 0; i < Clipping.NumRectangles; i++)
					gr.Rectangle(Clipping.GetRectangle(i));
				gr.Stroke ();*/
				#endif
			}
			gr.Dispose ();

			ctx.SetSource (bmp, rb.X, rb.Y);
			ctx.Paint ();

			Clipping.Dispose();
			Clipping = new Region ();
		}
		#endregion

		public virtual void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			Widget g = sender as Widget;

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
		void searchLargestChild (bool forceMeasure = false)
		{
			DbgLogger.StartEvent (DbgEvtType.GOSearchLargestChild, this);

			largestChild = null;
			contentSize.Width = 0;
			for (int i = 0; i < Children.Count; i++) {
				if (!Children [i].Visible)
					continue;
				int cw = 0;
				if (forceMeasure)
					cw = children [i].measureRawSize (LayoutingType.Width);
				else if (children [i].RegisteredLayoutings.HasFlag (LayoutingType.Width))
					continue;
				else
					cw = Children [i].Slot.Width;
				if (cw > contentSize.Width) {
					contentSize.Width = cw;
					largestChild = Children [i];
				}
			}

			DbgLogger.EndEvent (DbgEvtType.GOSearchLargestChild);
		}
		void searchTallestChild (bool forceMeasure = false)
		{
			DbgLogger.StartEvent (DbgEvtType.GOSearchTallestChild, this);

			tallestChild = null;
			contentSize.Height = 0;
			for (int i = 0; i < Children.Count; i++) {
				if (!Children [i].Visible)
					continue;
				int ch = 0;
				if (forceMeasure)
					ch = children [i].measureRawSize (LayoutingType.Height);
				else if (children [i].RegisteredLayoutings.HasFlag (LayoutingType.Height))
					continue;
				else
					ch = Children [i].Slot.Height;
				if (ch > contentSize.Height) {
					contentSize.Height = ch;
					tallestChild = Children [i];
				}
			}
			DbgLogger.EndEvent (DbgEvtType.GOSearchTallestChild);
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
				childrenRWLock.EnterReadLock ();
				foreach (Widget c in children)
					c.Dispose ();
				childrenRWLock.ExitReadLock ();
			}
			base.Dispose (disposing);
		}
	}
}

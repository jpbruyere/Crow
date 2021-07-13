// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.ComponentModel;
#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif
using System.Threading;

using static Crow.Logger;

namespace Crow
{
	public abstract class GroupBase : Widget
    {
		#if DESIGN_MODE
		public override bool FindByDesignID(string designID, out Widget go){
			go = null;
			if (base.FindByDesignID (designID, out go))
				return true;
			childrenRWLock.EnterReadLock ();
			try {
				foreach (Widget w in Children) {
					if (w.FindByDesignID (designID, out go))
						return true;
				}
				return false;
			} finally {
				childrenRWLock.ExitReadLock ();
			}
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
		public GroupBase () {}
		public GroupBase (Interface iface, string style = null) : base (iface, style) { }
		#endregion

        bool _multiSelect = false;
		IList<Widget> children = new ObservableList<Widget>();
        public virtual IList<Widget> Children => children;

		[DefaultValue(false)]
        public bool MultiSelect
        {
            get => _multiSelect;
            set {
				if (_multiSelect == value)
					return;
				_multiSelect = value;
				NotifyValueChangedAuto (_multiSelect);
			}
        }
		public virtual void AddChild(Widget g){
			InsertChild (children.Count, g);
		}
        public virtual void RemoveChild(Widget child)
		{
			//check if HoverWidget is removed from Tree
			if (IFace.HoverWidget != null) {
				if (this.Contains (IFace.HoverWidget))
					IFace.HoverWidget = null;
			}

			childrenRWLock.EnterWriteLock ();
			try {
				Children.Remove(child);
				child.Parent = null;
				child.LogicalParent = null;
			} finally {
				childrenRWLock.ExitWriteLock ();
			}
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
			try {	
				g.Parent = this;
				Children.Insert (idx, g);
			} finally {
				childrenRWLock.ExitWriteLock ();
			}
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

			try {
				while (Children.Count > 0) {
					Widget g = Children [Children.Count - 1];
					Children.RemoveAt (Children.Count - 1);
					g.Dispose ();
				}
			} finally {
				childrenRWLock.ExitWriteLock ();
			}
		}
		public override void OnDataSourceChanged (object sender, DataSourceChangeEventArgs e)
		{
			base.OnDataSourceChanged (this, e);

			childrenRWLock.EnterReadLock ();
			try {
				foreach (Widget g in Children) {
					if (g.localDataSourceIsNull & g.localLogicalParentIsNull)
						g.OnDataSourceChanged (g, e);	
				}
			} finally {
				childrenRWLock.ExitReadLock ();
			}
		}

		public void putWidgetOnTop(Widget w)
		{
			childrenRWLock.EnterWriteLock ();
			try {
				if (Children.Contains(w))
				{
					Children.Remove (w);
					Children.Add (w);
				}
			} finally {
				childrenRWLock.ExitWriteLock ();
			}
		}

		public void putWidgetOnBottom(Widget w)
		{
			childrenRWLock.EnterWriteLock ();
			try {
				if (Children.Contains(w))
				{
					Children.Remove (w);
					Children.Insert (0, w);
				}
			} finally {
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

			try {

				foreach (Widget w in Children) {
					tmp = w.FindByName (nameToFind);
					if (tmp != null)
						break;
				}
				return tmp;

			} finally {
				childrenRWLock.ExitReadLock ();
			}
		}
		public override T FindByType<T> () 
		{
			if (this is T t)
				return t;
			T tmp = default(T);

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
			childrenRWLock.EnterReadLock ();
			try {
				foreach (Widget w in Children) {
					if (w == goToFind)
						return true;
					if (w.Contains (goToFind))
						return true;
				}
				return false;
			} finally {
				childrenRWLock.ExitReadLock ();
			}
		}

		protected override void onDraw (Context gr)
		{
			DbgLogger.StartEvent (DbgEvtType.GODraw, this);

			base.onDraw (gr);			

			if (ClipToClientRect) {
				gr.Save ();
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			childrenRWLock.EnterReadLock ();
			try
			{
				for (int i = 0; i < Children.Count; i++) 
					Children[i].Paint (gr);
			} finally {
				childrenRWLock.ExitReadLock ();
			}

			if (ClipToClientRect)
				gr.Restore ();

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}
		protected override void UpdateCache (Context ctx)
		{
			DbgLogger.StartEvent(DbgEvtType.GOUpdateCache, this);
			if (!Clipping.IsEmpty) {
				using (Context gr = new Context (bmp)) {
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
					try {
						foreach (Widget c in Children) {
							if (!c.IsVisible)
								continue;
							if (Clipping.Contains (c.Slot + ClientRectangle.Position) == RegionOverlap.Out)
								continue;
							c.Paint (gr);
						}
					} finally {
						childrenRWLock.ExitReadLock ();
					}

					#if DEBUG_CLIP_RECTANGLE
					/*gr.LineWidth = 1;
					gr.SetSourceColor(Color.DarkMagenta.AdjustAlpha (0.8));
					for (int i = 0; i < Clipping.NumRectangles; i++)
						gr.Rectangle(Clipping.GetRectangle(i));
					gr.Stroke ();*/
					#endif
				}
			}/*else
				Console.WriteLine("GROUP REPAINT WITH EMPTY CLIPPING");*/
			//paintCache (ctx, Slot + Parent.ClientRectangle.Position);
			base.UpdateCache (ctx);
			DbgLogger.EndEvent(DbgEvtType.GOUpdateCache);				
		}
		#endregion

		#region Mouse handling
		public override void checkHoverWidget (MouseMoveEventArgs e) {
			base.checkHoverWidget (e);//TODO:check if not possible to put it at beginning of meth to avoid doubled check to DropTarget.
			if (!childrenRWLock.TryEnterReadLock (10))
				return;
			try {
				for (int i = Children.Count - 1; i >= 0; i--) {
					if (Children[i].MouseIsIn (e.Position)) {
						Children[i].checkHoverWidget (e);
						return;
					}
				}
			} finally {
				childrenRWLock.ExitReadLock ();
			}
		}
		#endregion

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				childrenRWLock.EnterReadLock ();
				try {
					foreach (Widget c in Children)
						c.Dispose ();
				} finally {
					childrenRWLock.ExitReadLock ();
				}
			}
			base.Dispose (disposing);
		}
	}
}

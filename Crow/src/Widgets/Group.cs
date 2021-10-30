// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Threading;

using static Crow.Logger;

namespace Crow
{
	public class Group : GroupBase
    {

		#region CTOR
		public Group () {}
		public Group(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		internal Widget largestChild = null;
		internal Widget tallestChild = null;
		#region EVENT HANDLERS
		public event EventHandler<EventArgs> ChildrenCleared;
		#endregion
        public override void RemoveChild(Widget child)
		{
			child.LayoutChanged -= OnChildLayoutChanges;
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

			if (child == largestChild && Width == Measure.Fit)
				searchLargestChild ();
			if (child == tallestChild && Height == Measure.Fit)
				searchTallestChild ();

			this.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			this.RegisterForGraphicUpdate();

		}
		public override void InsertChild (int idx, Widget g) {
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

			//largestChild = tallestChild = null;
			if (g.LastSlots.Width > contentSize.Width) {//TODO:Layout mutex?
				largestChild = g;
				contentSize.Width = g.LastSlots.Width;
			}
			if (g.LastSlots.Height > contentSize.Height) {
				tallestChild = g;
				contentSize.Height = g.LastSlots.Height;
			}

			g.LayoutChanged += OnChildLayoutChanges;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
		}
		public override void ClearChildren()
		{
			childrenRWLock.EnterWriteLock ();
			try {
				while (Children.Count > 0) {
					Widget g = Children [Children.Count - 1];
					g.LayoutChanged -= OnChildLayoutChanges;
					Children.RemoveAt (Children.Count - 1);
					g.Dispose ();
				}
			} finally {
				childrenRWLock.ExitWriteLock ();
			}

			resetChildrenMaxSize ();

			RegisterForLayouting (LayoutingType.Sizing);
			ChildrenCleared.Raise (this, new EventArgs ());
		}

		#region Widget overrides
		public override int measureRawSize (LayoutingType lt)
		{
			DbgLogger.StartEvent(DbgEvtType.GOMeasure, this, lt);
			try {
				if (Children.Count > 0) {
					if (lt == LayoutingType.Width) {
						//if (largestChild == null)
							searchLargestChild ();
					} else {
						//if (tallestChild == null)
							searchTallestChild ();
					}
				}
				return base.measureRawSize (lt);
			} finally {
				DbgLogger.EndEvent(DbgEvtType.GOMeasure);
			}
		}

		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			/*if (!IsVisible)
				return;*/
			base.OnLayoutChanges (layoutType);

			childrenRWLock.EnterReadLock ();
			try {
				//position smaller objects in group when group size is fit
				switch (layoutType) {
				case LayoutingType.Width:
					//childrenRWLock.EnterReadLock ();
					foreach (Widget c in Children) {
						if (c.Width.IsRelativeToParent)
							c.RegisterForLayouting (LayoutingType.Width);
						else
							c.RegisterForLayouting (LayoutingType.X);
					}
					//childrenRWLock.ExitReadLock ();
					break;
				case LayoutingType.Height:
					//childrenRWLock.EnterReadLock ();
					foreach (Widget c in Children) {
						if (c.Height.IsRelativeToParent)
							c.RegisterForLayouting (LayoutingType.Height);
						else
							c.RegisterForLayouting (LayoutingType.Y);
					}
					//childrenRWLock.ExitReadLock ();
					break;
				}
			} finally {
				childrenRWLock.ExitReadLock ();
			}
		}
		#endregion
		public virtual void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			DbgLogger.StartEvent(DbgEvtType.GOOnChildLayoutChange, this);

			Widget g = sender as Widget;

			switch (arg.LayoutType) {
			case LayoutingType.Width:
				if (Width == Measure.Fit) {
					if (g.Slot.Width > contentSize.Width) {
						largestChild = g;
						contentSize.Width = g.Slot.Width;
					} else if (g == largestChild)
						searchLargestChild ();
					/*else
						Console.WriteLine ($"else: {g} largest:{largestChild} {g.RequiredLayoutings} {this.RequiredLayoutings} {largestChild?.RequiredLayoutings}");*/
						/*break;*/
					this.RegisterForLayouting (LayoutingType.Width);
				}
				break;
			case LayoutingType.Height:
				if (Height == Measure.Fit) {
					if (g.Slot.Height > contentSize.Height) {
						tallestChild = g;
						contentSize.Height = g.Slot.Height;
					} else if (g == tallestChild)
						searchTallestChild ();
					/*else
						break;*/
					this.RegisterForLayouting (LayoutingType.Height);
				}
				break;
			}
			DbgLogger.EndEvent(DbgEvtType.GOOnChildLayoutChange);
		}
		//TODO: x,y position should be taken in account for computation of width and height + Layout mutex?
		void resetChildrenMaxSize(){
			largestChild = null;
			tallestChild = null;
			contentSize = 0;
		}
		protected virtual void searchLargestChild (bool forceMeasure = false)
		{
			DbgLogger.StartEvent (DbgEvtType.GOSearchLargestChild, this);
			childrenRWLock.EnterReadLock ();

			try {
				DbgLogger.SetMsg (DbgEvtType.GOSearchLargestChild, $"forced={forceMeasure}");

				largestChild = null;
				contentSize.Width = 0;
				for (int i = 0; i < Children.Count; i++) {
					if (!Children [i].IsVisible)
						continue;
					int cw = 0;
					if (forceMeasure)
						cw = Children [i].measureRawSize (LayoutingType.Width);
					else if (Children[i].Width.IsRelativeToParent || Children [i].RequiredLayoutings.HasFlag (LayoutingType.Width))
						continue;
					else
						cw = Children [i].Slot.Width;
					if (cw > contentSize.Width) {
						contentSize.Width = cw;
						largestChild = Children [i];
					}
				}
				if (largestChild == null && !forceMeasure)
					searchLargestChild (true);
			} finally {
				childrenRWLock.ExitReadLock ();
				DbgLogger.EndEvent (DbgEvtType.GOSearchLargestChild);
			}
		}
		protected virtual void searchTallestChild (bool forceMeasure = false)
		{
			DbgLogger.StartEvent (DbgEvtType.GOSearchTallestChild, this);
			childrenRWLock.EnterReadLock ();

			try {
				DbgLogger.SetMsg (DbgEvtType.GOSearchTallestChild, $"forced={forceMeasure}");

				tallestChild = null;
				contentSize.Height = 0;
				for (int i = 0; i < Children.Count; i++) {
					if (!Children [i].IsVisible)
						continue;
					int ch = 0;
					if (forceMeasure)
						ch = Children [i].measureRawSize (LayoutingType.Height);
					else if (Children[i].Height.IsRelativeToParent || Children [i].RequiredLayoutings.HasFlag (LayoutingType.Height))
						continue;
					else
						ch = Children [i].Slot.Height;
					if (ch > contentSize.Height) {
						contentSize.Height = ch;
						tallestChild = Children [i];
					}
				}
				if (tallestChild == null && !forceMeasure)
					searchTallestChild (true);
			} finally {
				childrenRWLock.ExitReadLock ();
				DbgLogger.EndEvent (DbgEvtType.GOSearchTallestChild);
			}
		}

#if DEBUG_STATS
		public override long ChildCount {
			get {
				childrenRWLock.EnterReadLock ();
				try {
					long total=0;
					foreach (Widget child in Children)
						total += 1 + child.ChildCount;
					return total;
				} finally {
					childrenRWLock.ExitReadLock ();
				}
			}
		}
#endif
		protected override string LogName => "grp";

	}
}

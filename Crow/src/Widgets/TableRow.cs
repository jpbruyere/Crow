// Copyright (c) 2019-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;
using System.Linq;

using Glfw;

namespace Crow
{
	public class TableRow : GroupBase, ISelectable {
		#region ISelectable implementation
		bool isSelected;
		public event EventHandler Selected;
		public event EventHandler Unselected;
		[DefaultValue (false)]
		public virtual bool IsSelected {
			get { return isSelected; }
			set {
				if (isSelected == value)
					return;
				isSelected = value;

				if (isSelected)
					Selected.Raise (this, null);
				else
					Unselected.Raise (this, null);

				NotifyValueChangedAuto (isSelected);
			}
		}
		#endregion
		#region EVENT HANDLERS
		public event EventHandler<EventArgs> ChildrenCleared;
		#endregion
		public override void ChildrenLayoutingConstraints(ILayoutable layoutable, ref LayoutingType layoutType)
			=> layoutType &= (~(LayoutingType.X|LayoutingType.Width));

		public Table Table => Parent as Table;
		internal Widget tallestChild = null;
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

			if (g.LastSlots.Height > contentSize.Height) {
				tallestChild = g;
				contentSize.Height = g.LastSlots.Height;
			}


			g.LayoutChanged += OnChildLayoutChanges;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
		}
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

			if (child == tallestChild && Height == Measure.Fit)
				searchTallestChild ();

			this.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);

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
		public override int measureRawSize (LayoutingType lt)
		{
			DbgLogger.StartEvent(DbgEvtType.GOMeasure, this);
			try {
				if (lt == LayoutingType.Height && Children.Count > 0 && tallestChild == null)
					searchTallestChild ();
				return base.measureRawSize (lt);
			} finally {
				DbgLogger.EndEvent(DbgEvtType.GOMeasure);
			}
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			//position smaller objects in group when group size is fit
			if (layoutType == LayoutingType.Height) {
				childrenRWLock.EnterReadLock ();
				try {
					foreach (Widget c in Children) {
						if (c.Height.IsRelativeToParent)
							c.RegisterForLayouting (LayoutingType.Height);
						else
							c.RegisterForLayouting (LayoutingType.Y);
					}
				} finally {
					childrenRWLock.ExitReadLock ();
				}
			}
		}
		public virtual void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			DbgLogger.StartEvent(DbgEvtType.GOOnChildLayoutChange, this);

			Widget g = sender as Widget;

			switch (arg.LayoutType) {
			case LayoutingType.Height:
				if (Height == Measure.Fit) {
					if (g.Slot.Height > contentSize.Height) {
						tallestChild = g;
						contentSize.Height = g.Slot.Height;
					} else if (g == tallestChild)
						searchTallestChild ();
					else
						break;
					this.RegisterForLayouting (LayoutingType.Height);
				}
				break;
			}
			DbgLogger.EndEvent(DbgEvtType.GOOnChildLayoutChange);
		}
		protected virtual void searchTallestChild (bool forceMeasure = false)
		{
			DbgLogger.StartEvent (DbgEvtType.GOSearchTallestChild, this);
			childrenRWLock.EnterReadLock ();

			try {
				tallestChild = null;
				contentSize.Height = 0;
				for (int i = 0; i < Children.Count; i++) {
					if (!Children [i].IsVisible)
						continue;
					int ch = 0;
					if (forceMeasure)
						ch = Children [i].measureRawSize (LayoutingType.Height);
					else if (Children [i].RequiredLayoutings.HasFlag (LayoutingType.Height))
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
		void resetChildrenMaxSize(){
			tallestChild = null;
			contentSize = 0;
		}
	}
}

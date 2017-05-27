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


namespace Crow
{
	public class Group : GraphicObject
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

			//check if HoverWidget is removed from Tree
			if (CurrentInterface.HoverWidget != null) {
				if (this.Contains (CurrentInterface.HoverWidget))
					CurrentInterface.HoverWidget = null;
			}

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
			lock (children) {
				while (Children.Count > 0) {
					GraphicObject g = Children [Children.Count - 1];
					g.LayoutChanged -= OnChildLayoutChanges;
					g.Parent = null;
					Children.RemoveAt (Children.Count - 1);
				}
			}

			resetChildrenMaxSize ();

			this.RegisterForLayouting (LayoutingType.Sizing);
			ChildrenCleared.Raise (this, new EventArgs ());
		}

//		public void putWidgetOnTop(GraphicObject w)
//		{
//			if (Children.Contains(w))
//			{
//				lock (children) {
//					Children.Remove (w);
//					Children.Add (w);
//				}
//			}
//		}
//		public void putWidgetOnBottom(GraphicObject w)
//		{
//			if (Children.Contains(w))
//			{
//				lock (children) {
//					Children.Remove (w);
//					Children.Insert (0, w);
//				}
//			}
//		}

		#region GraphicObject overrides
		public override void OnDataSourceChanged (object sender, DataSourceChangeEventArgs e)
		{
			base.OnDataSourceChanged (this, e);
			lock (children) {
				foreach (GraphicObject g in children)
					if (g.localDataSourceIsNull & g.localLogicalParentIsNull)
						g.OnDataSourceChanged (sender, e);
			}
		}
		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;
			GraphicObject tmp = null;
			lock (children) {
				foreach (GraphicObject w in Children) {
					tmp = w.FindByName (nameToFind);
					if (tmp != null)
						break;
				}
			}
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
		unsafe protected override void UpdateCache (Context ctx)
		{
			Rectangle rb = nativeHnd->Slot + Parent.ClientRectangle.Position;

			using (ImageSurface cache = new ImageSurface (bmp, Format.Argb32, nativeHnd->Slot.Width, nativeHnd->Slot.Height, 4 * nativeHnd->Slot.Width)) {
				Context gr = new Context (cache);

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

					lock (Children) {
						foreach (GraphicObject c in Children) {
							if (!c.Visible)
								continue;
							if (Clipping.Contains (c.nativeHnd->Slot + ClientRectangle.Position) == RegionOverlap.Out)
								continue;
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
			Clipping.Dispose();
			Clipping = new Region ();
		}
		#endregion

		unsafe public virtual void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{
			GraphicObject g = sender as GraphicObject;

			switch (arg.LayoutType) {
			case LayoutingType.Width:
				if (Width != Measure.Fit)
					return;
				if (g.nativeHnd->Slot.Width > contentSize.Width) {
					largestChild = g;
					contentSize.Width = g.nativeHnd->Slot.Width;
				} else if (g == largestChild)
					searchLargestChild ();

				this.RegisterForLayouting (LayoutingType.Width);
				break;
			case LayoutingType.Height:
				if (Height != Measure.Fit)
					return;
				if (g.nativeHnd->Slot.Height > contentSize.Height) {
					tallestChild = g;
					contentSize.Height = g.nativeHnd->Slot.Height;
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
		unsafe void searchLargestChild(){
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
				if (Children [i].nativeHnd->Slot.Width > contentSize.Width) {
					contentSize.Width = Children [i].nativeHnd->Slot.Width;
					largestChild = Children [i];
				}
			}
		}
		unsafe void searchTallestChild(){
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
				if (Children [i].nativeHnd->Slot.Height > contentSize.Height) {
					contentSize.Height = Children [i].nativeHnd->Slot.Height;
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
	}
}

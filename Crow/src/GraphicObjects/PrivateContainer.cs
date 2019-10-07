//
// PrivateContainer.cs
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
using System.Xml.Serialization;
using System.ComponentModel;
using Crow.Cairo;

namespace Crow
{
	/// <summary>
	/// Implement drawing and layouting for a single child, but
	/// does not expose child to allow reuse of container
	/// behaviour for widgets that have other xml hierarchy: example
	/// TemplatedControl may have 3 children (template,templateItem,content) but
	/// behave exactely as a container for layouting and drawing
	/// </summary>
	[DesignIgnore]
	public class PrivateContainer : Widget
	{
		#region CTOR
		protected PrivateContainer () : base(){}
		public PrivateContainer (Interface iface) : base(iface){}
		#endregion

		#if DESIGN_MODE
		public override bool FindByDesignID(string designID, out Widget go){
			go = null;
			if (base.FindByDesignID (designID, out go))
				return true;
			if (child == null)
				return false;
			return child.FindByDesignID (designID, out go);					
		}
		#endif
		protected Widget child;
		#if DEBUG_LOG
		internal GraphicObject getTemplateRoot {
			get { return child; }
		}
		#endif

		protected virtual void SetChild(Widget _child)
		{

			if (child != null) {
				//check if HoverWidget is removed from Tree
				if (IFace.HoverWidget != null) {
					if (this.Contains (IFace.HoverWidget))
						IFace.HoverWidget = null;
				}
				contentSize = default(Size);
				child.LayoutChanged -= OnChildLayoutChanges;
				this.RegisterForGraphicUpdate ();
				child.Dispose ();
			}

			child = _child as Widget;

			if (child != null) {
				child.Parent = this;
				child.LayoutChanged += OnChildLayoutChanges;
				contentSize = child.Slot.Size;
				child.RegisteredLayoutings = LayoutingType.None;
				child.RegisterForLayouting (LayoutingType.Sizing);
			}
		}
		//dispose child if not null
		protected virtual void deleteChild () {
			Widget g = child;
			SetChild (null);
			if (g != null)
				g.Dispose ();
		}

		#region GraphicObject Overrides

		public override Widget FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			return child == null ? null : child.FindByName (nameToFind);
		}
		public override bool Contains (Widget goToFind)
		{
			return child == goToFind ? true : 
				child == null ? false : child.Contains(goToFind);
		}
		public override void OnDataSourceChanged (object sender, DataSourceChangeEventArgs e)
		{
			base.OnDataSourceChanged (this, e);
			if (child != null)
			if (child.localDataSourceIsNull & child.localLogicalParentIsNull)
				child.OnDataSourceChanged (child, e);
		}
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			if (child != null) {
				//force sizing to fit if sizing on children and child has stretched size
				switch (layoutType) {
				case LayoutingType.Width:
					if (Width == Measure.Fit && child.Width.IsRelativeToParent)
						child.Width = Measure.Fit;
					break;
				case LayoutingType.Height:
					if (Height == Measure.Fit && child.Height.IsRelativeToParent)
						child.Height = Measure.Fit;
					break;
				}
			}
			return base.UpdateLayout (layoutType);
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (child == null)
				return;
			
			LayoutingType ltChild = LayoutingType.None;

			if (layoutType == LayoutingType.Width) {
				if (child.Width.IsRelativeToParent) {
					ltChild |= LayoutingType.Width;
					if (child.Width.Value < 100 && child.Left == 0)
						ltChild |= LayoutingType.X;
				} else if (child.Left == 0)
					ltChild |= LayoutingType.X;
			} else if (layoutType == LayoutingType.Height) {
				if (child.Height.IsRelativeToParent) {
					ltChild |= LayoutingType.Height;
					if (child.Height.Value < 100 && child.Top == 0)
						ltChild |= LayoutingType.Y;
				} else if (child.Top == 0)
						ltChild |= LayoutingType.Y;
			}
			if (ltChild == LayoutingType.None)
				return;
			child.RegisterForLayouting (ltChild);
		}
		public virtual void OnChildLayoutChanges (object sender, LayoutingEventArgs arg)
		{			
			Widget g = sender as Widget;

			if (arg.LayoutType == LayoutingType.Width) {
				if (Width != Measure.Fit)
					return;
				contentSize.Width = g.Slot.Width;
				this.RegisterForLayouting (LayoutingType.Width);
			}else if (arg.LayoutType == LayoutingType.Height){
				if (Height != Measure.Fit)
					return;
				contentSize.Height = g.Slot.Height;
				this.RegisterForLayouting (LayoutingType.Height);
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

			if (child != null) {
				if (child.Visible)
					child.Paint (ref gr);
			}
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

				onDraw (gr);
			}
				
			gr.Dispose ();

			ctx.SetSourceSurface (bmp, rb.X, rb.Y);
			ctx.Paint ();
			Clipping.Dispose();
			Clipping = new Region ();
		}
		#endregion

		#region Mouse handling
		public override void checkHoverWidget (MouseMoveEventArgs e)
		{
			base.checkHoverWidget (e);

			if (child != null) 
				if (child.MouseIsIn (e.Position)) 
					child.checkHoverWidget (e);
		}
		#endregion

		protected override void Dispose (bool disposing)
		{
			if (disposing && child != null)
				child.Dispose ();
			base.Dispose (disposing);
		}
	}
}


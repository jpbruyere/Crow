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
using Cairo;

namespace Crow
{
	/// <summary>
	/// Implement drawing and layouting for a single child, but
	/// does not implement IXmlSerialisation to allow reuse of container
	/// behaviour for widgets that have other xml hierarchy: example
	/// TemplatedControl may have 3 children (template,templateItem,content) but
	/// behave exactely as a container for layouting and drawing
	/// </summary>
	public class PrivateContainer : GraphicObject
	{
		#region CTOR
		public PrivateContainer()
			: base()
		{
		}
		#endregion

		protected GraphicObject child;

		internal virtual void SetChild(GraphicObject _child)
		{

			if (child != null) {
				//check if HoverWidget is removed from Tree
				if (CurrentInterface.HoverWidget != null) {
					if (this.Contains (CurrentInterface.HoverWidget))
						CurrentInterface.HoverWidget = null;
				}
				contentSize = new Size (0, 0);
				child.LayoutChanged -= OnChildLayoutChanges;
				child.Parent = null;
				this.RegisterForGraphicUpdate ();
			}

			child = _child as GraphicObject;

			if (child != null) {
				child.Parent = this;
				child.LayoutChanged += OnChildLayoutChanges;
				contentSize = child.Slot.Size;
				child.RegisteredLayoutings = LayoutingType.None;
				child.RegisterForLayouting (LayoutingType.Sizing);
			}
		}

		#region GraphicObject Overrides

		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			return child == null ? null : child.FindByName (nameToFind);
		}
		public override bool Contains (GraphicObject goToFind)
		{
			return child == goToFind ? true : 
				child == null ? false : child.Contains(goToFind);
		}
		public override void OnDataSourceChanged (object sender, DataSourceChangeEventArgs e)
		{
			base.OnDataSourceChanged (this, e);
			if (child != null)
			if (child.localDataSourceIsNull & child.localLogicalParentIsNull)
					child.OnDataSourceChanged (sender, e);
		}
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			if (child != null) {
				//force sizing to fit if sizing on children and child has stretched size
				switch (layoutType) {
				case LayoutingType.Width:
					if (Width == Measure.Fit && child.Width.Units == Unit.Percent)
						child.Width = Measure.Fit;
					break;
				case LayoutingType.Height:
					if (Height == Measure.Fit && child.Height.Units == Unit.Percent)
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
				if (child.Width.Units == Unit.Percent) {
					ltChild |= LayoutingType.Width;
					if (child.Width.Value < 100 && child.Left == 0)
						ltChild |= LayoutingType.X;
				} else if (child.Left == 0)
					ltChild |= LayoutingType.X;
			} else if (layoutType == LayoutingType.Height) {
				if (child.Height.Units == Unit.Percent) {
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
			GraphicObject g = sender as GraphicObject;

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

			using (ImageSurface cache = new ImageSurface (bmp, Format.Argb32, Slot.Width, Slot.Height, 4 * Slot.Width)) {
				Context gr = new Context (cache);

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

				ctx.SetSourceSurface (cache, rb.X, rb.Y);
				ctx.Paint ();
			}
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

	}
}


//
//  PrivateContainer.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
		public PrivateContainer(Rectangle _bounds)
			: base(_bounds)
		{
		}
		#endregion

		protected GraphicObject child;

		protected virtual T SetChild<T>(T _child)
		{

			if (child != null) {
				contentSize = new Size (0, 0);
				child.LayoutChanged -= OnChildLayoutChanges;
				this.RegisterForLayouting (LayoutingType.Sizing);
				child.Parent = null;
			}

			child = _child as GraphicObject;

			if (child != null) {
				child.Parent = this;
				child.LayoutChanged += OnChildLayoutChanges;
				contentSize = child.Slot.Size;
				child.RegisterForLayouting (LayoutingType.Sizing);
			}

			return (T)_child;
		}

		#region GraphicObject Overrides
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (child != null)
				child.ResolveBindings ();
		}
		protected void ResolveBindingsWithNoRecurse ()
		{
			base.ResolveBindings ();
		}
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
		public override void ChildrenLayoutingConstraints (ref LayoutingType layoutType)
		{			
			if (Width == Measure.Fit)
				layoutType &= (~LayoutingType.X);
			if (Height == Measure.Fit)
				layoutType &= (~LayoutingType.Y);
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
				if (Width == Measure.Fit)
					return;
				if (child.Width.Units == Unit.Percent) {
					ltChild |= LayoutingType.Width;
					if (child.Width.Value < 100 && child.Left == 0)
						ltChild |= LayoutingType.X;
				}
			} else if (layoutType == LayoutingType.Height) {
				if (Height == Measure.Fit)
					return;
				if (child.Height.Units == Unit.Percent) {
					ltChild |= LayoutingType.Height;
					if (child.Height.Value < 100 && child.Top == 0)
						ltChild |= LayoutingType.Y;
				}
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
			//clip to client zone
			CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
			gr.Clip ();

			if (child != null)
				child.Paint (ref gr);
			gr.Restore ();
		}
		protected override void UpdateCache (Context ctx)
		{
			//ctx.Save ();

			Rectangle rb = Slot + Parent.ClientRectangle.Position;

			using (ImageSurface cache = new ImageSurface (bmp, Format.Argb32, Slot.Width, Slot.Height, 4 * Slot.Width)) {
				Context gr = new Context (cache);

				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();

				if (Clipping.count > 0) {

					Clipping.clearAndClip (gr);

					if (child != null) {
						
						base.onDraw (gr);


						child.Paint (ref gr);
					}
				}
					
				gr.Dispose ();

				ctx.SetSourceSurface (cache, rb.X, rb.Y);
				ctx.Paint ();
			}
			Clipping.Reset();

			//ctx.Restore ();
		}
//		public override Rectangle ContextCoordinates (Rectangle r)
//		{
//			return
//				Parent.ContextCoordinates(r) + Slot.Position + ClientRectangle.Position;
//		}
//		public override void Paint(ref Cairo.Context ctx)
//		{
//			if (!Visible)//check if necessary??
//				return;
//
//			ctx.Save();
//
//			base.Paint(ref ctx);
//
//			//clip to client zone
//			CairoHelpers.CairoRectangle (ctx, Parent.ContextCoordinates(ClientRectangle + Slot.Position), CornerRadius);
//			ctx.Clip();
//
//			if (child != null)
//				child.Paint(ref ctx);
//
//			ctx.Restore();            
//		}

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

		public override void ClearBinding ()
		{
			if (child != null)
				child.ClearBinding ();
			base.ClearBinding ();
		}
	}
}


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
using OpenTK.Input;
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
				this.RegisterForLayouting ((int)LayoutingType.Sizing);
				child.Parent = null;
			}

			child = _child as GraphicObject;

			if (child != null) {
				child.Parent = this;
				child.RegisterForLayouting ((int)LayoutingType.Sizing);
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
		protected override Size measureRawSize ()
		{			
			return child == null ? Bounds.Size : child.Slot.Size + 2 * Margin;
		}

		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			switch (layoutType) {
			case LayoutingType.Width:								
				if (child != null) {
					if (child.getBounds ().Width == 0)
						child.RegisterForLayouting ((int)LayoutingType.Width);
					else
						child.RegisterForLayouting ((int)LayoutingType.X);
				}
				break;
			case LayoutingType.Height:
				if (child != null) {
					if (child.getBounds ().Height == 0)
						child.RegisterForLayouting ((int)LayoutingType.Height);
					else
						child.RegisterForLayouting ((int)LayoutingType.Y);
				}
				break;
			}							
		}

		public override Rectangle ContextCoordinates (Rectangle r)
		{
			return
				Parent.ContextCoordinates(r) + Slot.Position + ClientRectangle.Position;
		}
		public override void Paint(ref Cairo.Context ctx, Rectangles clip = null)
		{
			if (!Visible)//check if necessary??
				return;

			ctx.Save();

			if (clip != null)
				clip.clip(ctx);

			base.Paint(ref ctx, clip);

			//clip to client zone
			CairoHelpers.CairoRectangle (ctx, Parent.ContextCoordinates(ClientRectangle + Slot.Position), CornerRadius);
			ctx.Clip();

			if (child != null)
				child.Paint(ref ctx, clip);

			ctx.Restore();            
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

		public override void ClearBinding ()
		{
			if (child != null)
				child.ClearBinding ();
			base.ClearBinding ();
		}
	}
}


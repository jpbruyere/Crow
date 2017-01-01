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
		#endregion

		protected GraphicObject child;

		internal virtual void SetChild(GraphicObject _child)
		{

			if (child != null) {
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

				if (Clipping.count > 0) {
					Clipping.clearAndClip (gr);

					onDraw (gr);
				}
					
				gr.Dispose ();

				ctx.SetSourceSurface (cache, rb.X, rb.Y);
				ctx.Paint ();
			}
			Clipping.Reset();
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


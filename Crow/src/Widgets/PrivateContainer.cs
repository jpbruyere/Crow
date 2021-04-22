// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Cairo;
using static Crow.Logger;
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
		protected PrivateContainer () {}
		public PrivateContainer (Interface iface, string style = null) : base (iface, style) { }
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
		internal Widget getTemplateRoot {
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
		public override T FindByType<T> ()
		{
			if (this is T t)
				return t;

			return child == null ? default(T) : child.FindByType<T> ();
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

		public override int measureRawSize (LayoutingType lt)
		{
			if (child != null) {
				//force measure of child if sizing on children and child has stretched size
				switch (lt) {
				case LayoutingType.Width:
					if (child.Width.IsRelativeToParent)
						contentSize.Width = child.measureRawSize (LayoutingType.Width);
					break;
				case LayoutingType.Height:
					if (child.Height.IsRelativeToParent)
						contentSize.Height = child.measureRawSize (LayoutingType.Height);
					break;
				}
			}
			return base.measureRawSize (lt);
		}
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			if (child != null) {
				//force measure of child if sizing on children and child has stretched size
				switch (layoutType) {
				case LayoutingType.Width:
					if (Width == Measure.Fit && child.Width.IsRelativeToParent)
						//child.Width = Measure.Fit;
						contentSize.Width = child.measureRawSize (LayoutingType.Width);
					break;
				case LayoutingType.Height:
					if (Height == Measure.Fit && child.Height.IsRelativeToParent)
						//child.Height = Measure.Fit;
						contentSize.Height = child.measureRawSize (LayoutingType.Height);
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
				contentSize.Width = g.Slot.Width;
				if (Width != Measure.Fit)
					return;
				RegisterForLayouting (LayoutingType.Width);
			} else if (arg.LayoutType == LayoutingType.Height){
				contentSize.Height = g.Slot.Height;
				if (Height != Measure.Fit)
					return;
				RegisterForLayouting (LayoutingType.Height);
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

			if (child != null) {
				if (child.IsVisible)
					child.Paint (gr);
			}

			if (ClipToClientRect)
				gr.Restore ();

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}
		protected override void UpdateCache (Context ctx)
		{
			DbgLogger.StartEvent(DbgEvtType.GOUpdateCache, this);

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

			ctx.SetSource (bmp, rb.X, rb.Y);
			ctx.Paint ();
			DbgLogger.AddEvent (DbgEvtType.GOResetClip, this);
			Clipping.Reset ();
			DbgLogger.EndEvent(DbgEvtType.GOUpdateCache);
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


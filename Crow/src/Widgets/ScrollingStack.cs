// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;


namespace Crow {
	public class ScrollingStack : GenericStack {
		int scroll, maxScroll, itemSize = -1, visibleItems;

		[DefaultValue (0)]public virtual int Scroll {
			get { return scroll; }
			set {
                //cancelAdjustScroll = true;

				if (scroll == value)
					return;

				int newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScroll)
					newS = maxScroll;

				if (newS == scroll)
					return;

				scroll = newS;

				NotifyValueChangedAuto (scroll);
				RegisterForGraphicUpdate ();
			}
		}
		[DefaultValue (0)]public virtual int MaxScroll {
			get { return maxScroll; }
			set {
				if (maxScroll == value)
					return;

				maxScroll = Math.Max (0, value);

				if (scroll > maxScroll)
					Scroll = maxScroll;

				NotifyValueChangedAuto (maxScroll);
				//RegisterForGraphicUpdate ();
			}
		}



		public override void RemoveChild(Widget child){
			base.RemoveChild (child);
			updateMaxScroll ();
		}
		public override void InsertChild (int idx, Widget g) {
			base.InsertChild (idx, g);
			updateMaxScroll ();
		}
		public override void ClearChildren() {
			MaxScroll = 0;
			itemSize = -1;
			visibleItems = 0;
			base.ClearChildren ();
		}

		public override void OnLayoutChanges(LayoutingType layoutType)
		{
			if (Orientation == Orientation.Horizontal) {
				if (layoutType == LayoutingType.Width && itemSize > 0) {
					visibleItems = (int)Math.Ceiling ((double)ClientRectangle.Width / itemSize);
					updateMaxScroll ();
				}
			} else if (layoutType == LayoutingType.Height && itemSize > 0) {
				visibleItems = (int)Math.Ceiling ((double)ClientRectangle.Height / itemSize);
				updateMaxScroll ();
			}
			base.OnLayoutChanges(layoutType);
		}
		public override void OnChildLayoutChanges (object sender, LayoutingEventArgs arg) {
			DbgLogger.StartEvent(DbgEvtType.GOOnChildLayoutChange, this);
			Widget go = sender as Widget;

			if (Orientation == Orientation.Horizontal) {
				if (arg.LayoutType == LayoutingType.Width && itemSize < go.Slot.Width)
					itemSize = go.Slot.Width;
			} else if (arg.LayoutType == LayoutingType.Height && itemSize < go.Slot.Height)
				itemSize = go.Slot.Height;

			base.OnChildLayoutChanges (sender, arg);
			DbgLogger.EndEvent(DbgEvtType.GOOnChildLayoutChange);
		}

		void updateMaxScroll () {
			if (visibleItems <= 0)
				return;
			MaxScroll = Children.Count - visibleItems;
			NotifyValueChanged ("PageSize", visibleItems);
			NotifyValueChanged ("ChildRatio", Math.Min (1.0, (double)Children.Count / visibleItems));
		}

		protected override void onDraw (IContext gr)
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
			try	{
				for (int i = Scroll; i < Children.Count && i < Scroll + visibleItems; i++) {
					if (!Children[i].IsVisible)
						continue;
					/*if (Clipping.Contains (c.Slot + ClientRectangle.Position) == RegionOverlap.Out)
						continue;*/
					Children[i].Paint (gr);
				}
			} finally {
				childrenRWLock.ExitReadLock ();
			}

			if (ClipToClientRect)
				gr.Restore ();

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}
		protected override void UpdateCache (IContext ctx)
		{
			DbgLogger.StartEvent(DbgEvtType.GOUpdateCache, this);
			if (!Clipping.IsEmpty) {
				using (IContext gr = new Context (bmp)) {
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
						for (int i = Scroll; i < Children.Count && i < Scroll + visibleItems; i++) {
							if (!Children[i].IsVisible)
								continue;
							/*if (Clipping.Contains (c.Slot + ClientRectangle.Position) == RegionOverlap.Out)
								continue;*/
							Children[i].Paint (gr);
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
				DbgLogger.AddEvent (DbgEvtType.GOResetClip, this);
				Clipping.Reset ();
			}/*else
				Console.WriteLine("GROUP REPAINT WITH EMPTY CLIPPING");*/
			paintCache (ctx, Slot + Parent.ClientRectangle.Position);
			DbgLogger.EndEvent(DbgEvtType.GOUpdateCache);
		}


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
		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e) {
			e.Handled = true;
			Scroll += e.Delta * itemSize;
			base.onMouseWheel (sender, e);
		}
	}
}
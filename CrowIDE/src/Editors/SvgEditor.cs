// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using Crow.Cairo;

namespace Crow.Coding
{
	public class SvgEditor : Editor
	{
		SvgPicture _pic = new SvgPicture();

		int zoom;

		[DefaultValue(100)]
		public int Zoom {
			get { return zoom; }
			set {
				if (zoom == value)
					return;
				zoom = value;
				NotifyValueChanged ("Zoom", zoom);
				updateMaxScrolls ();
				RegisterForGraphicUpdate ();
			}
		}

		void updateMaxScrolls() {			
			MaxScrollX = Math.Max(0, _pic.Dimensions.Width * zoom / 100 - Slot.Width);
			MaxScrollY = Math.Max(0, _pic.Dimensions.Height * zoom / 100 - Slot.Height);

			if (Slot.Width + MaxScrollX > 0)
				NotifyValueChanged ("ChildWidthRatio", Slot.Width * Slot.Width / (Slot.Width + MaxScrollX));
			else
				NotifyValueChanged ("ChildWidthRatio", 0);
			
			if (Slot.Height + MaxScrollY > 0)
				NotifyValueChanged ("ChildHeightRatio", Slot.Height * Slot.Height / (Slot.Height + MaxScrollY));
			else
				NotifyValueChanged ("ChildHeightRatio", 0);
		}
		#region editor overrides
		protected override void updateEditorFromProjFile ()
		{
			Error = null;
			try {
				editorMutex.EnterWriteLock();
				_pic.LoadSvgFragment (projFile.Source);
				_pic.Scaled = true;
				_pic.KeepProportions = true;
			} catch (Exception ex) {
				Error = ex;
			}
			editorMutex.ExitWriteLock ();
			updateMaxScrolls ();
			RegisterForGraphicUpdate ();
		}
		protected override void updateProjFileFromEditor ()
		{
			throw new NotImplementedException ();
		}
		protected override bool EditorIsDirty {
			get { return false;	}
			set {
				throw new NotImplementedException ();
			}
		}
		protected override bool IsReady {			
			get { return projFile != null;	}
		}
		#endregion

		#region GraphicObject overrides
		public override int measureRawSize (LayoutingType lt)
		{
			if (_pic == null)
				return 2 * Margin;
			//_pic = "#Crow.Images.Icons.IconAlerte.svg";
			//TODO:take scalling in account
			if (lt == LayoutingType.Width)
				return _pic.Dimensions.Width + 2 * Margin;
			else
				return _pic.Dimensions.Height + 2 * Margin;
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Rectangle r = ClientRectangle;
			Foreground.SetAsSource (gr, r);
			gr.Rectangle (r, 0.1);
			gr.Stroke ();

			r.Width = _pic.Dimensions.Width * zoom / 100;
			r.Height = _pic.Dimensions.Height * zoom / 100;

			gr.Save ();

			editorMutex.EnterReadLock ();

			gr.Translate (-ScrollX, -ScrollY);
			if (_pic != null)
				_pic.Paint (gr, r);
			editorMutex.ExitReadLock ();

			gr.Restore ();
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);
			if ((layoutType | LayoutingType.Sizing) > 0)
				updateMaxScrolls ();
		}
		#endregion
	}
}


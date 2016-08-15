//
//  ImlVisualEditor.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
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
using Crow;
using System.Threading;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;

namespace CrowIDE
{
	public class ImlVisualEditor : GraphicObject
	{
		string imlPath;
		Interface imlVE;

		[XmlAttributeAttribute][DefaultValue("")]
		public virtual string ImlPath {
			get { return imlPath; }
			set {
				if (imlPath == value)
					return;
				if (!File.Exists (value))
					return;
				imlPath = value;
				NotifyValueChanged ("ImlPath", imlPath);
				imlVE.ClearInterface ();
				imlVE.LoadInterface (imlPath);
			}
		}
		bool drawGrid;
		[XmlAttributeAttribute()][DefaultValue(true)]
		public virtual bool DrawGrid {
			get { return drawGrid; }
			set {
				if (drawGrid == value)
					return;
				drawGrid = value;
				NotifyValueChanged ("DrawGrid", drawGrid);
				RegisterForRedraw ();
			}
		}
		int gridSpacing;
		[XmlAttributeAttribute()][DefaultValue(10)]
		public virtual int GridSpacing {
			get { return gridSpacing; }
			set {
				if (gridSpacing == value)
					return;
				gridSpacing = value;
				NotifyValueChanged ("GridSpacing", gridSpacing);
				RegisterForRedraw ();
			}
		}
		public ImlVisualEditor () : base()
		{
			imlVE = new Interface ();
			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();
		}

		void interfaceThread()
		{
			while (true) {
				imlVE.Update ();

				bool isDirty = false;

				lock (imlVE.RenderMutex)
					isDirty = imlVE.IsDirty;

				if (imlVE.IsDirty) {
					lock (CurrentInterface.UpdateMutex)
						RegisterForRedraw ();
				}

				Thread.Sleep (5);
			}
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);
			switch (layoutType) {
			case LayoutingType.Width:
			case LayoutingType.Height:
				imlVE.ProcessResize (this.ClientRectangle.Size);
				break;
			}
		}
		public override void RegisterForRedraw ()
		{
			base.RegisterForRedraw ();
			lock(imlVE.UpdateMutex)
				imlVE.clipping.AddRectangle (imlVE.ClientRectangle);
		}
		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);
			if (!drawGrid)
				return;


			Rectangle cb = ClientRectangle;
			const double gridLineWidth = 0.1;
			double glhw = gridLineWidth / 2.0;
			int nbLines = cb.Width / gridSpacing ;
			double d = cb.Left + gridSpacing;
			for (int i = 0; i < nbLines; i++) {
				gr.MoveTo (d-glhw, cb.Y);
				gr.LineTo (d-glhw, cb.Bottom);
				d += gridSpacing;
			}
			nbLines = cb.Height / gridSpacing;
			d = cb.Top + gridSpacing;
			for (int i = 0; i < nbLines; i++) {
				gr.MoveTo (cb.X, d - glhw);
				gr.LineTo (cb.Right, d -glhw);
				d += gridSpacing;
			}
			gr.LineWidth = gridLineWidth;
			Foreground.SetAsSource (gr, cb);
			gr.Stroke ();

			lock (imlVE.RenderMutex) {
				if (imlVE.IsDirty) {
					using (Cairo.Surface surf = new Cairo.ImageSurface (imlVE.dirtyBmp, Cairo.Format.Argb32,
						imlVE.DirtyRect.Width, imlVE.DirtyRect.Height, imlVE.DirtyRect.Width * 4)) {
						gr.SetSourceSurface (surf, imlVE.DirtyRect.Left, imlVE.DirtyRect.Top);
						gr.Paint ();
					}
					imlVE.IsDirty = false;
				}
			}
		}
	}
}

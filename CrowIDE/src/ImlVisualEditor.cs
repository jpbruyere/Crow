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
		#region CTOR
		public ImlVisualEditor () : base()
		{
			imlVE = new Interface ();
			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();
		}
		#endregion

		string imlPath;
		Interface imlVE;
		Instantiator itor;
		string imlSource;

		bool drawGrid;
		int gridSpacing;

		[XmlIgnore]public string ImlSource {
			get { return imlSource; }
			set {
				if (imlSource == value)
					return;
				imlSource = value;

				NotifyValueChanged ("ImlSource", ImlSource);

				reloadFromSource ();
			}
		}

		[XmlAttributeAttribute][DefaultValue("")]
		public string ImlPath {
			get { return imlPath; }
			set {
				if (imlPath == value)
					return;

				imlPath = value;

				NotifyValueChanged ("ImlPath", imlPath);

				reloadFromPath ();
			}
		}
		void reloadFromSource(){
			if (string.IsNullOrEmpty (imlSource)) {
				reload_iTor (null);
				return;
			}

			Instantiator iTmp;
			try {
				iTmp = Instantiator.CreateFromImlFragment (imlSource);
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex.ToString());
				return;
			}

			reload_iTor (iTmp);
		}
		void reloadFromPath(){
			if (!File.Exists (imlPath)){
				System.Diagnostics.Debug.WriteLine ("Path not found: " + imlPath);
				reload_iTor (null);
				return;
			}
			using (StreamReader sr = new StreamReader (imlPath)) {
				ImlSource = sr.ReadToEnd ();
			}
		}
		void reload_iTor(Instantiator new_iTot){
			itor = new_iTot;
			lock (imlVE.UpdateMutex) {
				try {
					imlVE.ClearInterface ();
					if (itor != null)
						imlVE.AddWidget(itor.CreateInstance(imlVE));

				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex.ToString());
				}
			}
		}
		[XmlAttributeAttribute()][DefaultValue(true)]
		public bool DrawGrid {
			get { return drawGrid; }
			set {
				if (drawGrid == value)
					return;
				drawGrid = value;
				NotifyValueChanged ("DrawGrid", drawGrid);
				RegisterForRedraw ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(10)]
		public int GridSpacing {
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
				try {
					imlVE.Update ();
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex.ToString ());
					if (Monitor.IsEntered(imlVE.UpdateMutex))
						Monitor.Exit (imlVE.UpdateMutex);
				}


				bool isDirty = false;

				lock (imlVE.RenderMutex)
					isDirty = imlVE.IsDirty;

				if (isDirty) {
					lock (CurrentInterface.UpdateMutex)
						RegisterForRedraw ();
				}

				Thread.Sleep (5);
			}
		}

		#region GraphicObject overrides
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
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			GraphicObject oldHW = imlVE.HoverWidget;
			Rectangle scr = this.ScreenCoordinates (this.getSlot ());
			imlVE.ProcessMouseMove (e.X - scr.X, e.Y - scr.Y);
			if (oldHW != imlVE.HoverWidget)
				RegisterForRedraw ();
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
				using (Cairo.Surface surf = new Cairo.ImageSurface (imlVE.bmp, Cairo.Format.Argb32,
					imlVE.ClientRectangle.Width, imlVE.ClientRectangle.Height, imlVE.ClientRectangle.Width * 4)) {
					gr.SetSourceSurface (surf, cb.Left, cb.Top);
					gr.Paint ();
				}
				imlVE.IsDirty = false;
			}

			if (imlVE.HoverWidget == null)
				return;

			Rectangle hr = imlVE.HoverWidget.ScreenCoordinates(imlVE.HoverWidget.getSlot ());

			gr.SetSourceColor (Color.LightGray);
			gr.DrawCote (new Cairo.PointD (hr.X, hr.Center.Y), new Cairo.PointD (hr.Right, hr.Center.Y));
			gr.DrawCote (new Cairo.PointD (hr.Center.X, hr.Y), new Cairo.PointD (hr.Center.X, hr.Bottom));
			hr.Inflate (2);
			gr.SetSourceColor (Color.LightGray);
			gr.SetDash (new double[]{ 3.0, 3.0 },0.0);
			gr.Rectangle (hr, 1.0);

		}
		#endregion
	}
}

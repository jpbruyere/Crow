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
				imlPath = value;
				NotifyValueChanged ("ImlPath", imlPath);
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

		}
		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);
			if (!drawGrid)
				return;


			Rectangle cb = ClientRectangle;

			int nbLines = cb.Width / gridSpacing ;
			double x = gridSpacing + cb.Center.X - nbLines * gridSpacing;
			for (int i = 0; i < nbLines; i++) {
				gr.MoveTo (x-0.5, cb.Y);
				gr.LineTo (x-0.5, cb.Y);
			}

			gr.LineWidth = 1.0;
			Foreground.SetAsSource (gr, cb);
			gr.Stroke ();
		}
	}
}

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
using System.Collections.Generic;
using Crow.IML;
using System.Text;
using System.Xml;

namespace Crow.Coding
{
	public class ImlSchematicEditor : TemplatedGroup
	{
		#region CTOR
		public ImlSchematicEditor ()
		{			
		}
		#endregion

		ProjectFileNode projNode;
		Widget selectedItem;
		ImlProjectItem imlProjFile;
		Exception imlError = null;

		bool drawGrid, snapToGrid;
		int gridSpacing;

		[DefaultValue(true)]
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
		[DefaultValue(true)]
		public bool SnapToGrid {
			get { return snapToGrid; }
			set {
				if (snapToGrid == value)
					return;
				snapToGrid = value;
				NotifyValueChanged ("SnapToGrid", snapToGrid);
			}
		}
		[DefaultValue(10)]
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
		public Widget SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem == value)
					return;
				selectedItem = value;
				NotifyValueChanged ("SelectedItem", selectedItem);
				RegisterForRedraw ();
			}
		}
//		public override ProjectFile ProjectNode {
//			get {
//				return projNode;
//			}
//			set {
//				if (projNode == value)
//					return;				
//				projNode = value;
//				NotifyValueChanged ("ProjectNode", projNode);
//
//				if (projNode is ImlProjectItem)
//					imlProjFile = projNode as ImlProjectItem;
//				else
//					imlProjFile = null;
//			}
//		}


//		protected override bool EditorIsDirty {
//			get {
//				throw new NotImplementedException ();
//			}
//			set {
//				throw new NotImplementedException ();
//			}
//		}
//		protected override void updateProjFileFromEditor ()
//		{
//
//		}
//		protected override void updateEditorFromProjFile () {
//			
//		}			
//		protected override void updateCheckPostProcess ()
//		{
//
//		}


	}
}

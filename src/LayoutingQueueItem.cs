//
//  LayoutingQueueItem.cs
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
using System.Diagnostics;

namespace Crow
{
	public enum LayoutingType
	{
		X = 0x01,
		Y = 0x02,
		Width = 0x04,
		Height = 0x08,
		Sizing = 0x0C,
		PositionChildren = 0x10,
		ChildAddition = 0x20,
		All = 0xFF
	}

	public class LayoutingQueueItem
	{

		public ILayoutable GraphicObject;
		public LayoutingType LayoutType;

		public LayoutingQueueItem (LayoutingType _layoutType, ILayoutable _graphicObject)
		{
			LayoutType = _layoutType;
			GraphicObject = _graphicObject;
		}
		public void ProcessLayouting()
		{
			#if DEBUG_LAYOUTING
			Debug.WriteLine ("Layouting => " + this.ToString ());
			#endif
			try {
				if (GraphicObject.Parent == null){
					Debug.WriteLine("ERROR: processLayouting, no parent for: " + GraphicObject.ToString());
					return;
				}
				GraphicObject.UpdateLayout (LayoutType);
			} catch (Exception ex) {
				Debug.WriteLine ("Layouting error: " + ex.ToString ());
			}
		}

		public static implicit operator GraphicObject(LayoutingQueueItem queueItem)
		{
			return queueItem.GraphicObject as GraphicObject;
		}
		public static implicit operator LayoutingType(LayoutingQueueItem lqi)
		{
			return lqi.LayoutType;
		}
		public override string ToString ()
		{
			return string.Format ("{1}->{0}", LayoutType,GraphicObject.ToString());
		}
	}
}


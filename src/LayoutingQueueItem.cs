﻿//
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
using System.Collections.Generic;

namespace Crow
{
	public enum LayoutingType : byte
	{
		None = 0x00,
		X = 0x01,
		Y = 0x02,
		Width = 0x04,
		Height = 0x08,
		Sizing = 0x0C,
		ArrangeChildren = 0x10,
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
			GraphicObject.RegisteredLayoutings |= LayoutType;
		}
	
		public void ProcessLayouting()
		{
			if (GraphicObject.Parent == null) {
				//cancel layouting for object without parent, maybe some were in queue when
				//removed from a listbox
				Debug.WriteLine ("ERROR: processLayouting, no parent for: " + this.ToString ());
				return;
			}
			#if DEBUG_LAYOUTING
			Debug.WriteLine ("Layouting => " + this.ToString ());
			#endif
			if (!GraphicObject.UpdateLayout (LayoutType)) {
				#if DEBUG_LAYOUTING
				Debug.WriteLine ("\tRequeuing => " + this.ToString ());
				#endif
				GraphicObject.LayoutingTries ++;
				if (GraphicObject.LayoutingTries < Interface.MaxLayoutingTries) {
					GraphicObject.RegisteredLayoutings |= LayoutType;
					Interface.LayoutingQueue.Enqueue (this);
				}
			} else {
				GraphicObject.LayoutingTries = 0;
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

